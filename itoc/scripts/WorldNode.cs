using Godot;
using ITOC.Core;

namespace ITOC;

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
        World = new World(GameControllerNode.Instance.GameController);

        _player = GetNode<PlayerController>("../Player");

        _debugCube = ResourceLoader.Load<PackedScene>("res://assets/meshes/debug_cube.tscn");

        _chunkInstantiator = new ChunkInstantiator(World);
        AddChild(_chunkInstantiator);
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