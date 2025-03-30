using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

public partial class World : Node
{
    // 区块加载范围（以区块为单位）
    public const int LoadDistance = 24;
    public const int ChunkSize = ChunkMesher.CS;

    // 单例访问
    public static World Instance { get; private set; }


    public bool UseDebugMaterial = false;
    public ShaderMaterial DebugMaterial;

    public bool DebugDrawChunkBounds = false;

    // 区块存储（线程安全字典）
    public readonly ConcurrentDictionary<Vector3I, Chunk> Chunks =
        new ConcurrentDictionary<Vector3I, Chunk>();

    // 多线程生成系统
    private ChunkGenerator _generator;
    private Queue<Vector3I> _generationQueue = new Queue<Vector3I>();
    private HashSet<Vector3I> _queuedPositions = new HashSet<Vector3I>();
    private const int MaxGenerationsPerFrame = 5; // 每帧最多处理5个区块


    private Vector3 _lastPlayerPosition = Vector3.Inf;

    private PackedScene _debugCube;

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
        Vector3 playerPos = GetPlayerPosition();
        if ((playerPos - _lastPlayerPosition).Length() > ChunkSize / 2)
        {
            UpdateChunkLoading(playerPos);
            _lastPlayerPosition = playerPos;
        }

        // 分帧提交生成请求
        int processed = 0;
        while (_generationQueue.Count > 0 && processed < MaxGenerationsPerFrame)
        {
            var pos = _generationQueue.Dequeue();
            _queuedPositions.Remove(pos);

            var request = new ChunkGenerationRequest(pos, MainThreadCallback);
            _generator.Enqueue(request);
            processed++;
        }
    }

    public Chunk GetChunk(Vector3 worldPos)
    {
        Vector3I chunkPos = WorldToChunkPosition(worldPos);
        Chunks.TryGetValue(chunkPos, out Chunk chunk);
        return chunk;
    }

    public int GetBlock(Vector3 worldPos)
    {
        Chunk chunk = GetChunk(worldPos);
        if (chunk == null) return 0;

        Vector3 localPos = WorldToLocalPosition(worldPos);
        return chunk.GetBlock((int)localPos.X, (int)localPos.Y, (int)localPos.Z);
    }

    private void UpdateChunkLoading(Vector3 center)
    {
        Vector3I centerChunk = WorldToChunkPosition(center);
        var loadArea = new HashSet<Vector3I>();

        // 计算需要加载的区块范围（原有逻辑）
        for (int x = -LoadDistance; x <= LoadDistance; x++)
            for (int y = -1; y <= 1; y++)
                for (int z = -LoadDistance; z <= LoadDistance; z++)
                {
                    Vector3I pos = centerChunk + new Vector3I(x, y, z);
                    if (pos.DistanceTo(centerChunk) <= LoadDistance)
                    {
                        loadArea.Add(pos);
                    }
                }

        // 卸载超出范围的区块（原有逻辑）
        foreach (var existingPos in Chunks.Keys)
        {
            if (!loadArea.Contains(existingPos))
            {
                if (Chunks.TryRemove(existingPos, out Chunk chunk))
                {
                    chunk.Unload();
                }
            }
        }

        // 收集需要生成的区块并按距离排序
        var toGenerate = new List<Vector3I>();
        foreach (var pos in loadArea)
        {
            if (!Chunks.ContainsKey(pos) && !_queuedPositions.Contains(pos))
            {
                toGenerate.Add(pos);
            }
        }

        // 按距离排序（近的优先）
        toGenerate.Sort((a, b) => a.DistanceTo(centerChunk).CompareTo(b.DistanceTo(centerChunk)));

        // 加入生成队列
        foreach (var pos in toGenerate)
        {
            if (_queuedPositions.Add(pos))
            {
                _generationQueue.Enqueue(pos);
            }
        }
    }

    private void MainThreadCallback(ChunkGenerationResult result)
    {
        // 检查区块是否仍在加载范围内
        Vector3 currentPlayerPos = GetPlayerPosition();
        Vector3I currentCenter = WorldToChunkPosition(currentPlayerPos);

        if (result.ChunkPosition.DistanceTo(currentCenter) > LoadDistance)
        {
            return; // 超出范围则丢弃
        }

        if (!Chunks.ContainsKey(result.ChunkPosition))
        {
            var chunk = new Chunk(result);
            Chunks[result.ChunkPosition] = chunk;
            chunk.Load();
            CallDeferred(Node.MethodName.AddChild, chunk);
        }
    }

    private Vector3I WorldToChunkPosition(Vector3 worldPos)
    {
        return new Vector3I(
            Mathf.FloorToInt(worldPos.X / ChunkSize),
            Mathf.FloorToInt(worldPos.Y / ChunkSize),
            Mathf.FloorToInt(worldPos.Z / ChunkSize)
        );
    }

    private Vector3 WorldToLocalPosition(Vector3 worldPos)
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