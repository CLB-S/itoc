using Godot;
using ITOC.Models;

namespace ITOC;

public class DirectionalBlock : Block
{
    private Direction _direction;

    public DirectionalBlock(Identifier id, string name, CubeModelBase blockModel, BlockProperties properties = null, Direction? freezeDirection = null)
        : base(id, name, blockModel, properties)
    {
        FreezeDirection = freezeDirection;
        if (FreezeDirection != null)
        {
            if (BlockModel is CubeDirectionalModel cubeModel)
            {
                cubeModel.DirectionPY = freezeDirection.Value;
                cubeModel.DirectionPX = freezeDirection.Value.Forward();
                cubeModel.DirectionPZ = freezeDirection.Value.Right();
            }
        }
    }

    public Direction? FreezeDirection { get; }

    public Direction Direction
    {
        get => FreezeDirection ?? _direction;
        set
        {
            if (FreezeDirection != null) return;

            _direction = value;
            if (BlockModel is CubeDirectionalModel cubeModel)
            {
                cubeModel.DirectionPY = value;
                cubeModel.DirectionPX = value.Forward();
                cubeModel.DirectionPZ = value.Right();
            }
        }
    }
}