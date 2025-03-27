using Godot;
using System;

public enum Direction
{
    PositiveX, NegativeX,
    PositiveY, NegativeY,
    PositiveZ, NegativeZ
}

public static class DirectionHelper
{
    public static Vector3I Norm(this Direction dir)
    {
        return dir switch
        {
            Direction.PositiveX => Vector3I.Right,
            Direction.NegativeX => Vector3I.Left,
            Direction.PositiveY => Vector3I.Up,
            Direction.NegativeY => Vector3I.Down,
            Direction.PositiveZ => Vector3I.Back,
            _ => Vector3I.Forward
        };
    }

    public static Vector3I Offset(this Direction dir)
    {
        return dir switch
        {
            Direction.PositiveX => Vector3I.Right,
            Direction.PositiveY => Vector3I.Up,
            Direction.PositiveZ => Vector3I.Back,
            _ => Vector3I.Zero
        };
    }

    public static Vector3I AntiOffset(this Direction dir)
    {
        return dir switch
        {
            Direction.NegativeX => Vector3I.Right,
            Direction.NegativeY => Vector3I.Up,
            Direction.NegativeZ => Vector3I.Back,
            _ => Vector3I.Zero
        };
    }

    public static Direction GetDirection(Vector3 dir)
    {
        if (Mathf.Abs(dir.X) > Mathf.Abs(dir.Y) && Mathf.Abs(dir.X) > Mathf.Abs(dir.Z))
        {
            return dir.X > 0 ? Direction.PositiveX : Direction.NegativeX;
        }
        else if (Mathf.Abs(dir.Y) > Mathf.Abs(dir.X) && Mathf.Abs(dir.Y) > Mathf.Abs(dir.Z))
        {
            return dir.Y > 0 ? Direction.PositiveY : Direction.NegativeY;
        }
        else
        {
            return dir.Z > 0 ? Direction.PositiveZ : Direction.NegativeZ;
        }
    }

    public static string Name(this Direction dir)
    {
        return dir switch
        {
            Direction.PositiveX => "+X",
            Direction.NegativeX => "-X",
            Direction.PositiveY => "+Y",
            Direction.NegativeY => "-Y",
            Direction.PositiveZ => "+Z",
            Direction.NegativeZ => "-Z",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static bool IsPositive(this Direction dir)
    {
        return dir switch
        {
            Direction.PositiveX => true,
            Direction.PositiveY => true,
            Direction.PositiveZ => true,
            _ => false
        };
    }

    public static Direction Opposite(this Direction dir)
    {
        return dir switch
        {
            Direction.PositiveX => Direction.NegativeX,
            Direction.NegativeX => Direction.PositiveX,
            Direction.PositiveY => Direction.NegativeY,
            Direction.NegativeY => Direction.PositiveY,
            Direction.PositiveZ => Direction.NegativeZ,
            _ => Direction.PositiveZ
        };
    }

}