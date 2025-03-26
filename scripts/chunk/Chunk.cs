using Godot;
using System;
using System.Collections.Generic;

public class ChunkFaceData
{
    public List<FaceRect> Rects { get; } = new List<FaceRect>();
}

public partial class Chunk : Node3D
{
    public const int SIZE = 32;
    public readonly Dictionary<Direction, ChunkFaceData> Faces = new Dictionary<Direction, ChunkFaceData>();
    public ShaderMaterial ChunkMaterial;


    private readonly int[,,] _voxels = new int[SIZE, SIZE, SIZE];

    private Vector3I _chunkID;
    public Vector3I ChunkID
    {
        get { return _chunkID; }
        set
        {
            Position = value * SIZE;
            _chunkID = value;
        }
    }


    public override void _Ready()
    {
        foreach (Direction dir in Enum.GetValues(typeof(Direction)))
        {
            Faces[dir] = new ChunkFaceData();
        }

        GenerateMeshes();

        foreach (Direction dir in Enum.GetValues(typeof(Direction)))
        {
            var faceMesh = new ChunkFaceMesh();
            faceMesh.Initialize(this, dir);
            AddChild(faceMesh);
        }
    }

    public int GetVoxel(int x, int y, int z)
    {
        if (x < 0 || x >= SIZE || y < 0 || y >= SIZE || z < 0 || z >= SIZE)
            return 0;
        return _voxels[x, y, z];
    }

    public void SetVoxel(int x, int y, int z, int value)
    {
        if (x < 0 || x >= SIZE || y < 0 || y >= SIZE || z < 0 || z >= SIZE)
            throw new IndexOutOfRangeException();
        _voxels[x, y, z] = value;
    }

    public void GenerateMeshes()
    {
        ChunkMeshPreGenerator.GenerateAllFaces(this);
    }
}
