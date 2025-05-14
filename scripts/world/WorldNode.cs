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

    private ChunkInstantiator _chunkInstantiator;


    public override void _Ready()
    {
        World = new World();

        _player = GetNode<PlayerController>("../Player");

        _debugCube = ResourceLoader.Load<PackedScene>("res://scenes/debug_cube.tscn");

        _chunkInstantiator = new ChunkInstantiator();
        AddChild(_chunkInstantiator);

        World.OnPlayerMovedHalfAChunk += (s, pos) => _chunkInstantiator.UpdatePlayerPosition(pos);
        World.OnChunkGenerated += (s, chunk) => _chunkInstantiator.AddChunk(chunk);
        World.OnChunkMeshUpdated += (s, chunk) => _chunkInstantiator.QueueChunkForUpdate(chunk);
    }

    public override void _PhysicsProcess(double delta)
    {
        World.PhysicsProcess(delta);
    }

    public override void _Process(double delta)
    {
        _chunkInstantiator.UpdateInstances();
    }

    public void SpawnDebugCube(Vector3I pos)
    {
        var cube = _debugCube.Instantiate() as Node3D;
        cube.Position = pos + Vector3.One * 0.5;
        CallDeferred(Node.MethodName.AddChild, cube);
    }
}