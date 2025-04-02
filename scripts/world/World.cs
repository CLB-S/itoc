using System.Collections.Concurrent;
using System.Collections.Generic;
using Godot;

public partial class World : Node
{
    // 区块加载范围（以区块为单位）
    public const int LoadDistance = 8;
    public const int ChunkSize = ChunkMesher.CS;

    private const int MaxGenerationsPerFrame = 3;

    // 区块存储（线程安全字典）
    public readonly ConcurrentDictionary<Vector3I, Chunk> Chunks = new();

    private PackedScene _debugCube;
    private readonly Queue<Vector3I> _generationQueue = new();

    // 多线程生成系统
    private ChunkGenerator _generator;


    private Vector3 _lastPlayerPosition = Vector3.Inf;
    private readonly HashSet<Vector3I> _queuedPositions = new();

    public bool DebugDrawChunkBounds = false;
    public bool UseDebugMaterial = false;
    public ShaderMaterial DebugMaterial;



    // 单例访问
    public static World Instance { get; private set; }

    public override void _Ready()
    {
        Instance = this;
        _generator = new ChunkGenerator();
        _generator.Start();

        DebugMaterial = ResourceLoader.Load<ShaderMaterial>("res://scripts/chunk/chunk_debug_shader_material.tres");
        _debugCube = ResourceLoader.Load<PackedScene>("res://scripts/world/debug_cube.tscn");

        // var request = new ChunkGenerationRequest(
        //     new Vector3I(0, -1, 0),
        //     MainThreadCallback
        // );

        // _generator.Enqueue(request);
    }

    public override void _Process(double delta)
    {
        // 玩家位置检查（原有逻辑）
        var playerPos = GetPlayerPosition();
        if ((playerPos - _lastPlayerPosition).Length() > ChunkSize / 2)
        {
            UpdateChunkLoading(playerPos);
            _lastPlayerPosition = playerPos;
        }

        // 分帧提交生成请求
        var processed = 0;
        while (_generationQueue.Count > 0 && processed < MaxGenerationsPerFrame)
        {
            var pos = _generationQueue.Dequeue();
            _queuedPositions.Remove(pos);

            var request = new ChunkGenerationRequest(pos, MainThreadCallback);
            _generator.Enqueue(request);
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

    public uint GetBlock(Vector3 worldPos)
    {
        var chunk = GetChunkWorldPos(worldPos);
        if (chunk == null) return 0;

        var localPos = WorldToLocalPosition(worldPos);
        return chunk.GetBlock(Mathf.FloorToInt(localPos.X), Mathf.FloorToInt(localPos.Y), Mathf.FloorToInt(localPos.Z));
    }

    public void SetBlock(Vector3 worldPos, uint block)
    {
        var chunk = GetChunkWorldPos(worldPos);
        if (chunk == null) return;

        var localPos = WorldToLocalPosition(worldPos);
        chunk.SetBlock(Mathf.FloorToInt(localPos.X), Mathf.FloorToInt(localPos.Y), Mathf.FloorToInt(localPos.Z), block);
    }

    private void UpdateChunkLoading(Vector3 center)
    {
        var centerChunk = WorldToChunkPosition(center);
        var loadArea = new HashSet<Vector3I>();

        // 计算需要加载的区块范围（原有逻辑）
        for (var x = -LoadDistance; x <= LoadDistance; x++)
            for (var y = -3; y <= 3; y++)
                for (var z = -LoadDistance; z <= LoadDistance; z++)
                {
                    var pos = centerChunk + new Vector3I(x, y, z);
                    if (pos.DistanceTo(centerChunk) <= LoadDistance) loadArea.Add(pos);
                }

        // 卸载超出范围的区块（原有逻辑）
        foreach (var existingPos in Chunks.Keys)
            if (!loadArea.Contains(existingPos))
                if (Chunks.TryRemove(existingPos, out var chunk))
                    chunk.Unload();

        // 收集需要生成的区块并按距离排序
        var toGenerate = new List<Vector3I>();
        foreach (var pos in loadArea)
            if (!Chunks.ContainsKey(pos) && !_queuedPositions.Contains(pos))
                toGenerate.Add(pos);

        // 按距离排序（近的优先）
        toGenerate.Sort((a, b) => a.DistanceTo(centerChunk).CompareTo(b.DistanceTo(centerChunk)));

        // 加入生成队列
        foreach (var pos in toGenerate)
            if (_queuedPositions.Add(pos))
                _generationQueue.Enqueue(pos);
    }

    private void MainThreadCallback(ChunkGenerationResult result)
    {
        // 检查区块是否仍在加载范围内
        var currentPlayerPos = GetPlayerPosition();
        var currentCenter = WorldToChunkPosition(currentPlayerPos);

        if (result.ChunkPosition.DistanceTo(currentCenter) > LoadDistance) return; // 超出范围则丢弃

        if (!Chunks.ContainsKey(result.ChunkPosition))
        {
            var chunk = new Chunk(result);
            Chunks[result.ChunkPosition] = chunk;
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
        cube.Position = pos + Vector3.One * 0.5f;
        CallDeferred(Node.MethodName.AddChild, cube);
    }

    public override void _ExitTree()
    {
        _generator.Stop();
    }
}