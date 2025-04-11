using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class World : Node
{
    public const int ChunkSize = ChunkMesher.CS;
    public const int MaxGenerationsPerFrame = 1;

    public readonly ConcurrentDictionary<Vector3I, Chunk> Chunks = new();
    public readonly ConcurrentDictionary<Vector2I, ChunkColumn> ChunkColumns = new();
    public WorldSettings Settings = new();
    public bool DebugDrawChunkBounds = false;
    public bool UseDebugMaterial = false;
    public ShaderMaterial DebugMaterial;
    private PackedScene _debugCube;

    private WorldGenerator.WorldGenerator _worldGenerator;
    private ChunkGenerator.ChunkFactory _chunkFactory;
    private bool _ready = false; //TODO: State
    private Vector3 _lastPlayerPosition = Vector3.Inf;
    private readonly HashSet<Vector2I> _queuedChunkColumns = new();
    private readonly Queue<Vector2I> _chunkColumnsGenerationQueue = new();

    public static World Instance { get; private set; } //TODO: Remove after gui.

    public override async void _Ready()
    {
        Instance = this;

        DebugMaterial = ResourceLoader.Load<ShaderMaterial>("res://scripts/chunk/chunk_debug_shader_material.tres");
        _debugCube = ResourceLoader.Load<PackedScene>("res://scripts/world/debug_cube.tscn");

        _worldGenerator = new WorldGenerator.WorldGenerator(Settings);
        await _worldGenerator.GenerateWorldAsync(); //TODO: GUI
        GD.Print("World pre-generation finished.");

        _chunkFactory = new ChunkGenerator.ChunkFactory();
        _chunkFactory.Start();

        _ready = true;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_ready) return;

        var playerPos = GetPlayerPosition();
        if ((playerPos - _lastPlayerPosition).Length() > ChunkSize / 2)
        {
            UpdateChunkLoading(playerPos);
            _lastPlayerPosition = playerPos;
        }

        var processed = 0;
        while (_chunkColumnsGenerationQueue.Count > 0 && processed < MaxGenerationsPerFrame)
        {
            var pos = _chunkColumnsGenerationQueue.Dequeue();
            _queuedChunkColumns.Remove(pos);

            var columnRequest = new ChunkGenerator.ChunkColumnGenerationRequest(_worldGenerator, pos, ChunkColumnGenerationCallback);
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

    public string GetBlock(Vector3 worldPos)
    {
        var chunk = GetChunkWorldPos(worldPos);
        if (chunk == null) return "itoc:air";

        var localPos = WorldToLocalPosition(worldPos);
        return chunk.GetBlock(Mathf.FloorToInt(localPos.X), Mathf.FloorToInt(localPos.Y), Mathf.FloorToInt(localPos.Z));
    }

    public void SetBlock(Vector3 worldPos, string block)
    {
        var chunk = GetChunkWorldPos(worldPos);
        if (chunk == null) return;

        var localPos = WorldToLocalPosition(worldPos);
        chunk.SetBlock(Mathf.FloorToInt(localPos.X), Mathf.FloorToInt(localPos.Y), Mathf.FloorToInt(localPos.Z), block);
    }

    private void UpdateChunkLoading(Vector3 center)
    {
        var centerChunk = WorldToChunkPosition(center);
        var centerChunkXZ = new Vector2I(centerChunk.X, centerChunk.Z);
        var loadArea = new HashSet<Vector2I>();

        // Load Area
        for (var x = -Settings.LoadDistance; x <= Settings.LoadDistance; x++)
            for (var z = -Settings.LoadDistance; z <= Settings.LoadDistance; z++)
            {
                var pos = centerChunkXZ + new Vector2I(x, z);
                if (pos.DistanceTo(centerChunkXZ) <= Settings.LoadDistance) loadArea.Add(pos);
            }

        // Unload
        foreach (var existingPos in Chunks.Keys)
            if (!loadArea.Contains(new Vector2I(existingPos.X, existingPos.Z)))
                if (Chunks.TryRemove(existingPos, out var chunk))
                    chunk.Unload();

        foreach (var existingPos in ChunkColumns.Keys)
            if (!loadArea.Contains(existingPos))
                if (ChunkColumns.TryRemove(existingPos, out var chunkColumn))
                    chunkColumn = null;

        // To generate
        var toGenerate = new List<Vector2I>();
        foreach (var pos in loadArea)
            if (!ChunkColumns.ContainsKey(pos) && !_queuedChunkColumns.Contains(pos))
                toGenerate.Add(pos);

        // Sort by distance.
        toGenerate.Sort((a, b) => a.DistanceTo(centerChunkXZ).CompareTo(b.DistanceTo(centerChunkXZ)));

        foreach (var pos in toGenerate)
            if (_queuedChunkColumns.Add(pos))
                _chunkColumnsGenerationQueue.Enqueue(pos);
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

                var request = new ChunkGenerator.ChunkGenerationRequest(_worldGenerator, chunkPos, result, ChunkGenerationCallback);
                _chunkFactory.Enqueue(request);
            }
        }
    }

    private void ChunkGenerationCallback(ChunkGenerator.ChunkGenerationResult result)
    {
        if (result == null) return;

        // var currentPlayerPos = GetPlayerPosition();
        // var currentCenter = WorldToChunkPosition(currentPlayerPos);
        // if (result.ChunkData.GetPosition().DistanceTo(currentCenter) > Settings.LoadDistance) return;

        if (!Chunks.ContainsKey(result.ChunkData.GetPosition()))
        {
            var chunk = new Chunk(result);
            Chunks[result.ChunkData.GetPosition()] = chunk;
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
        _chunkFactory.Stop();
        _chunkFactory.Dispose();
    }
}