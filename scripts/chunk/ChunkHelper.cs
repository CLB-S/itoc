using Godot;

public enum Axis { X, Y, Z }

public struct FaceRect
{
    public Vector3I Start; // 面起始坐标（根据方向确定主轴位置）
    public int Width;        // 面在第一个副轴方向的扩展
    public int Height;       // 面在第二个副轴方向的扩展
    // public int Material;    // 材质ID

    public FaceRect(Vector3I start, int width, int height)
    {
        Start = start;
        Width = width;
        Height = height;
        // Material = material;
    }
}



public static class ChunkHelper
{
    public static bool IsPositiveDirection(Direction dir) => dir.ToString().StartsWith("Positive");

    public static Axis GetMainAxis(Direction dir)
    {
        return dir switch
        {
            Direction.PositiveX or Direction.NegativeX => Axis.X,
            Direction.PositiveY or Direction.NegativeY => Axis.Y,
            _ => Axis.Z
        };
    }


    public static Vector3I GetVoxelPosition(Axis mainAxis, int layer, int a, int b)
    {
        return mainAxis switch
        {
            Axis.X => new Vector3I(layer, a, b),
            Axis.Y => new Vector3I(a, layer, b),
            _ => new Vector3I(a, b, layer)
        };
    }

    public static Vector3I GetFacePosition(Vector3I voxelPos, Direction dir)
    {
        return voxelPos + dir.Norm();
    }

    public static Vector3I GetFaceStartPosition(Direction dir, int layer, int x, int y)
    {
        return dir switch
        {
            Direction.PositiveX => new Vector3I(layer + 1, y, x),
            Direction.NegativeX => new Vector3I(layer, y, x),
            Direction.PositiveY => new Vector3I(y, layer + 1, x),
            Direction.NegativeY => new Vector3I(y, layer, x),
            Direction.PositiveZ => new Vector3I(y, x, layer + 1),
            _ => new Vector3I(y, x, layer)
        };
    }
}