using System;
using System.Collections.Concurrent;
using Godot;
using ITOC;

public partial class WorldNode : Node
{
    public World World { get; private set; }

    public bool DebugDrawChunkBounds = false;

    private PackedScene _debugCube;
    // private ChunkFactory _chunkFactory;

    private PlayerController _player;
    private bool _playerSpawned; // TODO: Spawn player. Implement `GetHeight(Vector2 pos)`

    private ChunkRenderer _chunkRenderer;

    private ConcurrentDictionary<Vector3I, Chunk> _chunksToUpdateMesh = new();

    public override void _Ready()
    {
        World = new World();

        _player = GetNode<PlayerController>("../Player");

        _debugCube = ResourceLoader.Load<PackedScene>("res://scenes/debug_cube.tscn");

        _chunkRenderer = new ChunkRenderer();
        AddChild(_chunkRenderer);

        World.OnPlayerMovedHalfAChunk += (s, pos) => _chunkRenderer.UpdatePlayerPosition(pos);
        World.OnChunkGenerated += (s, chunk) => _chunkRenderer.AddChunk(chunk);
        World.OnChunkMeshUpdated += (s, chunk) => _chunksToUpdateMesh.TryAdd(chunk.Position, chunk);
    }

    public override void _PhysicsProcess(double delta)
    {
        World.PhysicsProcess(delta);
    }

    public override void _Process(double delta)
    {
        // Update chunk meshes and remove them from the dictionary
        foreach (var chunk in _chunksToUpdateMesh)
            _chunkRenderer.UpdateChunk(chunk.Value);
        _chunksToUpdateMesh.Clear();

        _chunkRenderer.RenderAll();
    }

    public void SpawnDebugCube(Vector3I pos)
    {
        var cube = _debugCube.Instantiate() as Node3D;
        cube.Position = pos + Vector3.One * 0.5;
        CallDeferred(Node.MethodName.AddChild, cube);
    }
}