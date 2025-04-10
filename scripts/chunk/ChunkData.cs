using Godot;
using System;

public class ChunkData
{
    public readonly int x;
    public readonly int y;
    public readonly int z;
    public ushort[] Voxels;

    private ChunkData() { }
    public ChunkData(int x, int y, int z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public ChunkData(Vector3I pos)
    {
        x = pos.X;
        y = pos.Y;
        z = pos.Z;
    }

    public Vector3I GetPosition()
    {
        return new Vector3I(x, y, z);
    }
}