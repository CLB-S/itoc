using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ChunkGenerator;
using Godot;

public partial class World : Node
{
    public const int ChunkSize = ChunkMesher.CS;
    public WorldGenerator.WorldGenerator Generator { get; private set; }
    public WorldSettings Settings => Generator.Settings;

    public Vector3I PlayerChunk { get; private set; } = Vector3I.Zero;
    public double Time { get; private set; }

    public static World Instance { get; private set; }

    public readonly ConcurrentDictionary<Vector3I, Chunk> Chunks = new();
    public readonly ConcurrentDictionary<Vector2I, ChunkColumn> ChunkColumns = new();
    public bool DebugDrawChunkBounds = false;
    public bool UseDebugMaterial = false;
    public ShaderMaterial DebugMaterial;

    private PackedScene _debugCube;
    private ChunkFactory _chunkFactory;
    private bool _ready; //TODO: State
    private Vector3 _lastPlayerPosition = Vector3.Inf;
    private readonly Queue<Vector2I> _chunkColumnsGenerationQueue = new();

    private PlayerController _player;
    public Vector3 PlayerPos { get; private set; } = Vector3.Zero;
    private bool _playerSpawned; // TODO: Spawn player. Implement `GetHeight(Vector2 pos)`

    public override void _Ready()
    {
        if (Instance != null)
            throw new Exception("World singleton already exists!");

        Instance = this;
        Generator = Core.Instance.WorldGenerator;

        Time += Settings.MinutesPerDay * 60.0f / 3; // 8:00 AM

        _player = GetNode<PlayerController>("../Player");

        DebugMaterial = ResourceLoader.Load<ShaderMaterial>("res://assets/graphics/chunk_debug_shader_material.tres");
        _debugCube = ResourceLoader.Load<PackedScene>("res://scenes/debug_cube.tscn");

        _chunkFactory = new ChunkFactory();
        _chunkFactory.Start();

        _ready = true;
    }

    // TODO: Chunk generation logic should be revised. 
    public override void _Process(double delta)
    {
        if (!_ready) return;

        Time += delta;
        PlayerPos = GetPlayerPosition();

        if ((PlayerPos - _lastPlayerPosition).Length() > ChunkSize / 2)
        {
            PlayerChunk = WorldToChunkPosition(PlayerPos);
            UpdateChunkLoading();
            _lastPlayerPosition = PlayerPos;
        }

        var processed = 0;
        while (_chunkColumnsGenerationQueue.Count > 0 && processed < Core.Instance.Settings.MaxChunkGenerationsPerFrame)
        {
            var pos = _chunkColumnsGenerationQueue.Dequeue();

            var columnRequest = new ChunkColumnGenerationRequest(Generator, pos, ChunkColumnGenerationCallback);
            _chunkFactory.Enqueue(columnRequest);
            processed++;
        }
    }

    public Chunk GetChunkWorldPos(Vector3 worldPos)
    {
        var chunkPos = WorldToChunkPosition(worldPos);
        Chunks.TryGetValue(chunkPos, out var chunk);
        return chunk;
    }


    public Chunk GetChunk(Vector3I chunkPos)
    {
        Chunks.TryGetValue(chunkPos, out var chunk);
        return chunk;
    }

    public Block GetBlock(Vector3 worldPos)
    {
        var chunk = GetChunkWorldPos(worldPos);
        if (chunk == null) return null;

        var localPos = WorldToLocalPosition(worldPos);
        return chunk.GetBlock(Mathf.FloorToInt(localPos.X), Mathf.FloorToInt(localPos.Y), Mathf.FloorToInt(localPos.Z));
    }

    public void SetBlock(Vector3 worldPos, string blockId)
    {
        var block = BlockManager.Instance.GetBlock(blockId);
        SetBlock(worldPos, block);
    }

    public void SetBlock(Vector3 worldPos, Block block)
    {
        var chunk = GetChunkWorldPos(worldPos);
        if (chunk == null) return;

        var localPos = WorldToLocalPosition(worldPos);
        chunk.SetBlock(Mathf.FloorToInt(localPos.X), Mathf.FloorToInt(localPos.Y), Mathf.FloorToInt(localPos.Z), block);
    }

    // TODO: Generate 3x3x3 chunks around the player if these chunks are not generated and coressponding ChunkColumns are generated. 
    private void UpdateChunkLoading()
    {
        var playerChunkXZ = new Vector2I(PlayerChunk.X, PlayerChunk.Z);
        var renderArea = new HashSet<Vector2I>();

        // Load Area
        for (var x = -Core.Instance.Settings.RenderDistance; x <= Core.Instance.Settings.RenderDistance; x++)
            for (var z = -Core.Instance.Settings.RenderDistance; z <= Core.Instance.Settings.RenderDistance; z++)
            {
                var pos = playerChunkXZ + new Vector2I(x, z);
                if (pos.DistanceTo(playerChunkXZ) <= Core.Instance.Settings.RenderDistance) renderArea.Add(pos);
            }

        // Unload
        foreach (var existingPos in Chunks.Keys)
            if (!renderArea.Contains(new Vector2I(existingPos.X, existingPos.Z)))
                if (Chunks.TryRemove(existingPos, out var chunk))
                    chunk.Unload();

        foreach (var existingPos in ChunkColumns.Keys)
            if (!renderArea.Contains(existingPos))
                if (ChunkColumns.TryRemove(existingPos, out var chunkColumn))
                    chunkColumn = null;

        // To generate ChunkColumns
        var toGenerate = new List<Vector2I>();
        foreach (var pos in renderArea)
            if (!ChunkColumns.ContainsKey(pos))
                toGenerate.Add(pos);

        // Sort by distance.
        toGenerate.Sort((a, b) => a.DistanceTo(playerChunkXZ).CompareTo(b.DistanceTo(playerChunkXZ)));

        // Reset the generation queue and queued set to ensure proper sorting by new player position
        _chunkColumnsGenerationQueue.Clear();
        foreach (var pos in toGenerate)
            _chunkColumnsGenerationQueue.Enqueue(pos);

        // Generate 3x3x3 chunks around the player for existing ChunkColumns
        GeneratePlayerSurroundingChunks();
    }

    private void GeneratePlayerSurroundingChunks()
    {
        // Generate 3x3x3 area around player
        for (var x = -1; x <= 1; x++)
            for (var y = -1; y <= 1; y++)
                for (var z = -1; z <= 1; z++)
                {
                    var chunkPos = PlayerChunk + new Vector3I(x, y, z);
                    var columnPos = new Vector2I(chunkPos.X, chunkPos.Z);

                    // Skip if chunk already exists
                    if (Chunks.ContainsKey(chunkPos))
                        continue;

                    // Only generate chunks for columns that already exist
                    if (ChunkColumns.TryGetValue(columnPos, out var chunkColumn))
                    {
                        var createCollisionShape = chunkPos.DistanceTo(PlayerChunk) <= Core.Instance.Settings.PhysicsDistance;
                        var request = new ChunkGenerationRequest(
                            Generator,
                            chunkPos,
                            chunkColumn,
                            ChunkGenerationCallback,
                            createCollisionShape
                        );
                        _chunkFactory.Enqueue(request);
                    }
                }
    }

    private void ChunkColumnGenerationCallback(ChunkColumn result)
    {
        if (result == null) return;

        if (!ChunkColumns.ContainsKey(result.Position))
        {
            ChunkColumns[result.Position] = result;

            var high = Mathf.FloorToInt(result.HeightMapHigh / ChunkSize);
            var low = Mathf.FloorToInt((result.HeightMapLow - 2) / ChunkSize) - 1;

            for (var y = low; y <= high; y++)
            {
                var chunkPos = new Vector3I(result.Position.X, y, result.Position.Y);
                if (Chunks.ContainsKey(chunkPos)) continue;

                var createCollisionShape = chunkPos.DistanceTo(PlayerChunk) <= Core.Instance.Settings.PhysicsDistance;
                var request = new ChunkGenerationRequest(Generator, chunkPos, result, ChunkGenerationCallback,
                    createCollisionShape);
                _chunkFactory.Enqueue(request);
            }
        }
    }

    private void ChunkGenerationCallback(ChunkGenerationResult result)
    {
        if (result == null) return;

        // var currentPlayerPos = GetPlayerPosition();
        // var currentCenter = WorldToChunkPosition(currentPlayerPos);
        // if (result.ChunkData.GetPosition().DistanceTo(currentCenter) > Core.Instance.Settings.LoadDistance) return;

        var position = result.ChunkData.GetPosition();
        var positionXZ = new Vector2I(position.X, position.Z);
        var playerPosition = new Vector2I(PlayerChunk.X, PlayerChunk.Z);
        if (!Chunks.ContainsKey(position) && ChunkColumns.TryGetValue(positionXZ, out var value)
                                          && playerPosition.DistanceTo(positionXZ) <=
                                          Core.Instance.Settings.RenderDistance)
        {
            var chunk = new Chunk(result);
            Chunks[position] = chunk;
            value.Chunks[position] = chunk;
            chunk.Load();
            CallDeferred(Node.MethodName.AddChild, chunk);
        }
    }

    public static Vector3I WorldToChunkPosition(Vector3 worldPos)
    {
        return new Vector3I(
            Mathf.FloorToInt(worldPos.X / ChunkSize),
            Mathf.FloorToInt(worldPos.Y / ChunkSize),
            Mathf.FloorToInt(worldPos.Z / ChunkSize)
        );
    }

    public static Vector3 WorldToLocalPosition(Vector3 worldPos)
    {
        return new Vector3(
            Mathf.PosMod(worldPos.X, ChunkSize),
            Mathf.PosMod(worldPos.Y, ChunkSize),
            Mathf.PosMod(worldPos.Z, ChunkSize)
        );
    }

    private Vector3 GetPlayerPosition()
    {
        return CameraHelper.Instance.GetCameraPosition();
    }

    public void SpawnDebugCube(Vector3I pos)
    {
        var cube = _debugCube.Instantiate() as Node3D;
        cube.Position = pos + Vector3.One * 0.5;
        CallDeferred(Node.MethodName.AddChild, cube);
    }

    public override void _ExitTree()
    {
        _chunkFactory?.Stop();
        _chunkFactory?.Dispose();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            _chunkFactory?.Stop();
            _chunkFactory?.Dispose();
        }
    }
}