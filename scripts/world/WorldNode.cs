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


    public override void _Ready()
    {
        World = new World();

        _player = GetNode<PlayerController>("../Player");

        _debugCube = ResourceLoader.Load<PackedScene>("res://scenes/debug_cube.tscn");

        _chunkRenderer = new ChunkRenderer();
        AddChild(_chunkRenderer);

        World.OnPlayerMovedHalfAChunk += (s, pos) => _chunkRenderer.UpdatePlayerPosition(pos);
        World.OnChunkGenerated += (s, chunk) => _chunkRenderer.AddChunk(chunk);
        World.OnChunkMeshUpdated += (s, chunk) => _chunkRenderer.QueueChunkForUpdate(chunk);
    }

    public override void _PhysicsProcess(double delta)
    {
        World.PhysicsProcess(delta);
    }

    public override void _Process(double delta)
    {
        _chunkRenderer.UpdateRendering();
    }

    public void SpawnDebugCube(Vector3I pos)
    {
        var cube = _debugCube.Instantiate() as Node3D;
        cube.Position = pos + Vector3.One * 0.5;
        CallDeferred(Node.MethodName.AddChild, cube);
    }
}