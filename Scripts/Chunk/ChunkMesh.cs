using Godot;
using System;
using System.Collections.Generic;

public partial class ChunkMesh : MeshInstance3D
{
    [Export]
    public ShaderMaterial Material;

    private Chunk _targetChunk;

    public void Initialize(Chunk chunk)
    {
        _targetChunk = chunk;
        GenerateMesh();
    }

    public override void _Ready()
    {
        Initialize(ChunkGenerator.GenerateBallChunk());
        // Initialize(ChunkGenerator.GenerateDebugChunk());
        // Initialize(ChunkGenerator.GenerateChunkRandom(0.01f));
    }

    private void GenerateMesh()
    {
        var surfaceArray = new Godot.Collections.Array();
        surfaceArray.Resize((int)Mesh.ArrayType.Max);

        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var normals = new List<Vector3>();
        var indices = new List<int>();

        foreach (Direction direction in Enum.GetValues(typeof(Direction)))
        {
            ProcessDirection(direction, vertices, uvs, normals, indices);
        }

        surfaceArray[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();
        surfaceArray[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Normal] = normals.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Index] = indices.ToArray();

        var arrayMesh = new ArrayMesh();
        arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);
        arrayMesh.SurfaceSetMaterial(0, Material);
        Mesh = arrayMesh;

        GD.Print("Mesh generated");
    }

    private void ProcessDirection(Direction dir, List<Vector3> vertices,
        List<Vector2> uvs, List<Vector3> normals, List<int> indices)
    {
        var normal = ChunkHelper.GetDirectionNormal(dir);
        var faceData = _targetChunk.Faces[dir];

        foreach (FaceRect rect in faceData.Rects)
        {
            var baseIndex = vertices.Count;
            var corners = GetQuadCorners(rect);

            // 添加四个顶点
            vertices.AddRange(corners);

            // 统一法线方向
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);

            // 标准UV映射
            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(1, 1));
            uvs.Add(new Vector2(0, 1));

            // 三角形索引（顺时针顺序）
            indices.Add(baseIndex + 0);
            indices.Add(baseIndex + 1);
            indices.Add(baseIndex + 2);
            indices.Add(baseIndex + 0);
            indices.Add(baseIndex + 2);
            indices.Add(baseIndex + 3);
        }
    }

    private Vector3[] GetQuadCorners(FaceRect rect)
    {
        switch (rect.FaceDirection)
        {
            case Direction.PositiveX:
                return new Vector3[]
                {
                    rect.Start,
                    new Vector3(rect.Start.X, rect.Start.Y, rect.Start.Z + rect.Width),
                    new Vector3(rect.Start.X, rect.Start.Y + rect.Height, rect.Start.Z + rect.Width),
                    new Vector3(rect.Start.X, rect.Start.Y + rect.Height, rect.Start.Z),
                };
            case Direction.NegativeX:
                return new Vector3[]
                {
                    rect.Start,
                    new Vector3(rect.Start.X, rect.Start.Y + rect.Height, rect.Start.Z),
                    new Vector3(rect.Start.X, rect.Start.Y + rect.Height, rect.Start.Z + rect.Width),
                    new Vector3(rect.Start.X, rect.Start.Y, rect.Start.Z + rect.Width),
                };
            case Direction.PositiveY:
                return new Vector3[]
                {
                    rect.Start,
                    new Vector3(rect.Start.X + rect.Height, rect.Start.Y, rect.Start.Z),
                    new Vector3(rect.Start.X + rect.Height, rect.Start.Y, rect.Start.Z + rect.Width),
                    new Vector3(rect.Start.X, rect.Start.Y, rect.Start.Z + rect.Width),
                };
            case Direction.NegativeY:
                return new Vector3[]
                {
                    rect.Start,
                    new Vector3(rect.Start.X, rect.Start.Y, rect.Start.Z + rect.Width),
                    new Vector3(rect.Start.X + rect.Height, rect.Start.Y, rect.Start.Z + rect.Width),
                    new Vector3(rect.Start.X + rect.Height, rect.Start.Y, rect.Start.Z),
                };
            case Direction.PositiveZ:
                return new Vector3[]
                {
                    rect.Start,
                    new Vector3(rect.Start.X, rect.Start.Y + rect.Width, rect.Start.Z),
                    new Vector3(rect.Start.X + rect.Height, rect.Start.Y + rect.Width, rect.Start.Z),
                    new Vector3(rect.Start.X + rect.Height, rect.Start.Y, rect.Start.Z),
                };
            case Direction.NegativeZ:
                return new Vector3[]
                {
                    rect.Start,
                    new Vector3(rect.Start.X + rect.Height, rect.Start.Y, rect.Start.Z),
                    new Vector3(rect.Start.X + rect.Height, rect.Start.Y + rect.Width, rect.Start.Z),
                    new Vector3(rect.Start.X, rect.Start.Y + rect.Width, rect.Start.Z),
                };

            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
