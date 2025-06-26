using ITOC.Core.BlockModels;

namespace ITOC.Core;

public class DirectionalCubeBlock : CubeBlock
{
    private Direction _direction;

    public DirectionalCubeBlock(
        Identifier id,
        string name,
        CubeDirectionalModel blockModel,
        BlockProperties properties = null,
        Direction? freezeDirection = null
    )
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
            if (FreezeDirection != null)
                return;

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
