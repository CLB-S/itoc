using Godot;

namespace ITOC.Models;

public abstract class CubeDirectionalModel : CubeModelBase
{
    public Direction DirectionPY { get; set; } = Direction.PositiveY;
    public Direction DirectionPX { get; set; } = Direction.PositiveX;
    public Direction DirectionPZ { get; set; } = Direction.PositiveZ;

    public override Material GetMaterial(Direction face = Direction.PositiveY)
    {
        if (face == DirectionPY)
            return base.GetMaterial(Direction.PositiveY);
        else if (face == DirectionPY.Opposite())
            return base.GetMaterial(Direction.NegativeY);
        else if (face == DirectionPX)
            return base.GetMaterial(Direction.PositiveX);
        else if (face == DirectionPX.Opposite())
            return base.GetMaterial(Direction.NegativeX);
        else if (face == DirectionPZ)
            return base.GetMaterial(Direction.PositiveZ);
        else if (face == DirectionPZ.Opposite())
            return base.GetMaterial(Direction.NegativeZ);
        else
            return base.GetMaterial(face);
    }
}