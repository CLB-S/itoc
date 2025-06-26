using Godot;
using ITOC.Core;

namespace ITOC;

public partial class WorldNode : Node
{
    private World _world;

    public override void _Ready()
    {
        _world = new World(this, GameController.Instance.WorldGenerator);

        GameController.Instance.CurrentWorld = _world;

        var playerNode = GetNode<PlayerNode>("../Player");
        var chunkInstantiator = new ChunkInstantiator(playerNode.Player, _world.ChunkManager);
        AddChild(chunkInstantiator);
    }

    public override void _PhysicsProcess(double delta)
    {
        _world.OnPhysicsProcess(delta);
    }
}