using System;
using Godot;
using ITOC;

public partial class WorldNode : Node
{
    public static WorldNode Instance { get; private set; }
    public World World { get; private set; }
    public bool DebugDrawChunkBounds = false;

    private PackedScene _debugCube;
    // private ChunkFactory _chunkFactory;

    private PlayerController _player;
    private bool _playerSpawned; // TODO: Spawn player. Implement `GetHeight(Vector2 pos)`

    public override void _Ready()
    {
        if (Instance != null)
            throw new Exception("World singleton already exists!");

        Instance = this;

        World = new World();

        _player = GetNode<PlayerController>("../Player");

        _debugCube = ResourceLoader.Load<PackedScene>("res://scenes/debug_cube.tscn");


        World.ChunkGenerator.ChunkColumnCompleted += (sender, args) =>
        {
            GD.Print($"Chunk column completed: {args}");
            if (World.ChunkColumns.TryGetValue(args, out var chunkColumn))
                foreach (var chunk in chunkColumn.Chunks.Values)
                {
                    CallDeferred(Node.MethodName.AddChild, chunk);
                    chunk.LoadDeferred();
                }
        };

    }

    public override void _PhysicsProcess(double delta)
    {
        World.PhysicsProcess(delta);
    }

    public void SpawnDebugCube(Vector3I pos)
    {
        var cube = _debugCube.Instantiate() as Node3D;
        cube.Position = pos + Vector3.One * 0.5;
        CallDeferred(Node.MethodName.AddChild, cube);
    }
}