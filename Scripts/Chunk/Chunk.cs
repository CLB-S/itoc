using Godot;
using System;
using System.Collections.Generic;

public class FaceData
{
    public List<FaceRect> Rects { get; } = new List<FaceRect>();
}

public class Chunk
{
    public const int SIZE = 32;
    private int[,,] voxels = new int[SIZE, SIZE, SIZE];
    public readonly Dictionary<Direction, FaceData> Faces = new Dictionary<Direction, FaceData>();

    public Chunk()
    {
        foreach (Direction dir in Enum.GetValues(typeof(Direction)))
        {
            Faces[dir] = new FaceData();
        }
    }

    public int GetVoxel(int x, int y, int z)
    {
        if (x < 0 || x >= SIZE || y < 0 || y >= SIZE || z < 0 || z >= SIZE)
            return 0;
        return voxels[x, y, z];
    }

    public void SetVoxel(int x, int y, int z, int value)
    {
        if (x < 0 || x >= SIZE || y < 0 || y >= SIZE || z < 0 || z >= SIZE)
            throw new IndexOutOfRangeException();
        voxels[x, y, z] = value;
    }

    public void GenerateMeshes()
    {
        ChunkMeshPreGenerator.GenerateAllFaces(this);
    }
}
