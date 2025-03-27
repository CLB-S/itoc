using Godot;
using System;
using System.Collections.Generic;

public partial class ChunkFaceMesh : MeshInstance3D
{
    public ChunkFace ChunkFace { get; private set; }

    private int _blockID;
    private List<FaceRect> _faceRects;


    public void Initialize(ChunkFace chunkFace, int BlockID, List<FaceRect> faceRects)
    {
        ChunkFace = chunkFace;
        _blockID = BlockID;
        _faceRects = faceRects;

        GenerateMesh();

        var block = BlockManager.Instance.GetBlock(_blockID);
        var material = block.GetMaterial(ChunkFace.Direction);
        // var material = ResourceLoader.Load($"res://scripts/chunk/chunk_debug_shader_material.tres") as ShaderMaterial;
        SetSurfaceOverrideMaterial(0, material);
    }

    private void GenerateMesh()
    {
        var surfaceArray = new Godot.Collections.Array();
        surfaceArray.Resize((int)Mesh.ArrayType.Max);

        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var normals = new List<Vector3>();
        var indices = new List<int>();


        foreach (FaceRect rect in _faceRects)
        {
            var baseIndex = vertices.Count;
            var corners = GetQuadCorners(rect);

            // 添加四个顶点
            vertices.AddRange(corners);

            // 统一法线方向
            normals.Add(ChunkFace.Normal);
            normals.Add(ChunkFace.Normal);
            normals.Add(ChunkFace.Normal);
            normals.Add(ChunkFace.Normal);

            // 标准UV映射
            if ((ChunkFace.Direction == Direction.PositiveZ) ||
                (ChunkFace.Direction == Direction.NegativeZ) ||
                (ChunkFace.Direction == Direction.NegativeY))
            {
                uvs.Add(new Vector2(0, rect.Width));
                uvs.Add(new Vector2(0, 0));
                uvs.Add(new Vector2(rect.Height, 0));
                uvs.Add(new Vector2(rect.Height, rect.Width));
            }
            else
            {
                uvs.Add(new Vector2(0, rect.Height));
                uvs.Add(new Vector2(0, 0));
                uvs.Add(new Vector2(rect.Width, 0));
                uvs.Add(new Vector2(rect.Width, rect.Height));
            }

            // 三角形索引（顺时针顺序）
            indices.Add(baseIndex + 0);
            indices.Add(baseIndex + 1);
            indices.Add(baseIndex + 2);
            indices.Add(baseIndex + 0);
            indices.Add(baseIndex + 2);
            indices.Add(baseIndex + 3);
        }

        surfaceArray[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();
        surfaceArray[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Normal] = normals.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Index] = indices.ToArray();

        var arrayMesh = new ArrayMesh();
        arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);
        Mesh = arrayMesh;
    }

    private Vector3[] GetQuadCorners(FaceRect rect)
    {
        switch (ChunkFace.Direction)
        {
            case Direction.PositiveX:
                return new Vector3[]
                {
                    new Vector3(rect.Start.X, rect.Start.Y, rect.Start.Z + rect.Width),
                    new Vector3(rect.Start.X, rect.Start.Y + rect.Height, rect.Start.Z + rect.Width),
                    new Vector3(rect.Start.X, rect.Start.Y + rect.Height, rect.Start.Z),
                    rect.Start,
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
                    new Vector3(rect.Start.X + rect.Height, rect.Start.Y, rect.Start.Z),
                    new Vector3(rect.Start.X + rect.Height, rect.Start.Y + rect.Width, rect.Start.Z),
                    new Vector3(rect.Start.X, rect.Start.Y + rect.Width, rect.Start.Z),
                    rect.Start,
                };

            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
