using System;
using System.Collections.Generic;
using Godot;

public static class ChunkMesher
{
    public const int CS = 62;
    public const int CS_P = CS + 2;
    public const int CS_2 = CS * CS;
    public const int CS_P2 = CS_P * CS_P;
    public const int CS_P3 = CS_P * CS_P * CS_P;

    public static int GetAxisIndex(int axis, int a, int b, int c)
    {
        return axis switch
        {
            0 => b + a * CS_P + c * CS_P2,
            1 => b + c * CS_P + a * CS_P2,
            _ => c + a * CS_P + b * CS_P2
        };
    }

    public static int GetIndex(int x, int y, int z)
    {
        return z + x * CS_P + y * CS_P2;
    }

    public static int GetIndex(Vector3I vec)
    {
        return vec.Z + vec.X * CS_P + vec.Y * CS_P2;
    }


    private static ulong GetQuad(ulong x, ulong y, ulong z, ulong w, ulong h, ulong type)
    {
        return (type << 32) | (h << 24) | (w << 18) | (z << 12) | (y << 6) | x;
    }


    public static void MeshVoxels(uint[] voxels, MeshData meshData)
    {
        meshData.Quads.Clear();
        meshData.QuadBlockIDs.Clear();

        Array.Clear(meshData.FaceMasks, 0, meshData.FaceMasks.Length);
        Array.Clear(meshData.ForwardMerged, 0, meshData.ForwardMerged.Length);
        Array.Clear(meshData.RightMerged, 0, meshData.RightMerged.Length);

        for (var a = 1; a < CS_P - 1; a++)
        {
            var aCS_P = a * CS_P;
            for (var b = 1; b < CS_P - 1; b++)
            {
                var columnBits = meshData.OpaqueMask[a * CS_P + b] & ~((1UL << 63) | 1);
                var baIndex = b - 1 + (a - 1) * CS;
                var abIndex = a - 1 + (b - 1) * CS;

                meshData.FaceMasks[baIndex + 0 * CS_2] = (columnBits & ~meshData.OpaqueMask[aCS_P + CS_P + b]) >> 1;
                meshData.FaceMasks[baIndex + 1 * CS_2] = (columnBits & ~meshData.OpaqueMask[aCS_P - CS_P + b]) >> 1;
                meshData.FaceMasks[abIndex + 2 * CS_2] = (columnBits & ~meshData.OpaqueMask[aCS_P + b + 1]) >> 1;
                meshData.FaceMasks[abIndex + 3 * CS_2] = (columnBits & ~meshData.OpaqueMask[aCS_P + (b - 1)]) >> 1;
                meshData.FaceMasks[baIndex + 4 * CS_2] = columnBits & ~(meshData.OpaqueMask[aCS_P + b] >> 1);
                meshData.FaceMasks[baIndex + 5 * CS_2] = columnBits & ~(meshData.OpaqueMask[aCS_P + b] << 1);
            }
        }

        for (var face = 0; face < 4; face++)
        {
            var axis = face / 2;
            meshData.FaceVertexBegin[face] = meshData.Quads.Count;

            for (var layer = 0; layer < CS; layer++)
            {
                var bitsLocation = layer * CS + face * CS_2;
                for (var forward = 0; forward < CS; forward++)
                {
                    var bitsHere = meshData.FaceMasks[forward + bitsLocation];
                    if (bitsHere == 0) continue;

                    var bitsNext = forward + 1 < CS ? meshData.FaceMasks[forward + 1 + bitsLocation] : 0;

                    byte rightMerged = 1;
                    while (bitsHere != 0)
                    {
                        var bitPos = System.Numerics.BitOperations.TrailingZeroCount(bitsHere);
                        var blockID = voxels[GetAxisIndex(axis, forward + 1, bitPos + 1, layer + 1)];
                        ref var forwardMergedRef = ref meshData.ForwardMerged[bitPos];

                        if ((bitsNext & (1UL << bitPos)) != 0 &&
                            blockID == voxels[GetAxisIndex(axis, forward + 2, bitPos + 1, layer + 1)])
                        {
                            forwardMergedRef++;
                            bitsHere &= ~(1UL << bitPos);
                            continue;
                        }

                        for (var right = bitPos + 1; right < CS; right++)
                        {
                            if ((bitsHere & (1UL << right)) == 0 ||
                                forwardMergedRef != meshData.ForwardMerged[right] ||
                                blockID != voxels[GetAxisIndex(axis, forward + 1, right + 1, layer + 1)])
                                break;

                            meshData.ForwardMerged[right] = 0;
                            rightMerged++;
                        }

                        bitsHere &= ~((1UL << (bitPos + rightMerged)) - 1);

                        var meshFront = (byte)(forward - forwardMergedRef);
                        var meshLeft = (byte)bitPos;
                        var meshUp = (byte)(layer + (~face & 1));

                        var meshWidth = rightMerged;
                        var meshLength = (byte)(forwardMergedRef + 1);

                        ulong quad = face switch
                        {
                            0 or 1 => GetQuad(
                                (ulong)(meshFront + (face == 1 ? meshLength : 0)),
                                meshUp,
                                meshLeft,
                                meshLength,
                                meshWidth,
                                0),
                            2 or 3 => GetQuad(
                                meshUp,
                                (ulong)(meshFront + (face == 2 ? meshLength : 0)),
                                meshLeft,
                                meshLength,
                                meshWidth,
                                0),
                            _ => 0
                        };

                        meshData.Quads.Add(quad);
                        meshData.QuadBlockIDs.Add(blockID);

                        forwardMergedRef = 0;
                        rightMerged = 1;
                    }
                }
            }

            meshData.FaceVertexLength[face] = meshData.Quads.Count - meshData.FaceVertexBegin[face];
        }

        for (var face = 4; face < 6; face++)
        {
            var axis = face / 2;
            meshData.FaceVertexBegin[face] = meshData.Quads.Count;

            for (var forward = 0; forward < CS; forward++)
            {
                var bitsLocation = forward * CS + face * CS_2;
                var bitsForwardLocation = (forward + 1) * CS + face * CS_2;

                for (var right = 0; right < CS; right++)
                {
                    var bitsHere = meshData.FaceMasks[right + bitsLocation];
                    if (bitsHere == 0) continue;

                    var bitsForward = forward < CS - 1 ? meshData.FaceMasks[right + bitsForwardLocation] : 0;
                    var bitsRight = right < CS - 1 ? meshData.FaceMasks[right + 1 + bitsLocation] : 0;
                    var rightCS = right * CS;

                    while (bitsHere != 0)
                    {
                        var bitPos = System.Numerics.BitOperations.TrailingZeroCount(bitsHere);
                        bitsHere &= ~(1UL << bitPos);

                        var blockID = voxels[GetAxisIndex(axis, right + 1, forward + 1, bitPos)];
                        ref var forwardMergedRef = ref meshData.ForwardMerged[rightCS + (bitPos - 1)];
                        ref var rightMergedRef = ref meshData.RightMerged[bitPos - 1];

                        if (rightMergedRef == 0 &&
                            (bitsForward & (1UL << bitPos)) != 0 &&
                            blockID == voxels[GetAxisIndex(axis, right + 1, forward + 2, bitPos)])
                        {
                            forwardMergedRef++;
                            continue;
                        }

                        if ((bitsRight & (1UL << bitPos)) != 0 &&
                            forwardMergedRef == meshData.ForwardMerged[rightCS + CS + (bitPos - 1)] &&
                            blockID == voxels[GetAxisIndex(axis, right + 2, forward + 1, bitPos)])
                        {
                            forwardMergedRef = 0;
                            rightMergedRef++;
                            continue;
                        }

                        var meshLeft = (byte)(right - rightMergedRef);
                        var meshFront = (byte)(forward - forwardMergedRef);
                        var meshUp = (byte)(bitPos - 1 + (~face & 1));
                        var meshWidth = (byte)(1 + rightMergedRef);
                        var meshLength = (byte)(1 + forwardMergedRef);

                        var quad = GetQuad(
                            (ulong)(face == 4 ? meshLeft + meshWidth : meshLeft),
                            meshFront,
                            meshUp,
                            meshWidth,
                            meshLength,
                            0
                        );

                        meshData.Quads.Add(quad);
                        meshData.QuadBlockIDs.Add(blockID);
                        forwardMergedRef = 0;
                        rightMergedRef = 0;
                    }
                }
            }

            meshData.FaceVertexLength[face] = meshData.Quads.Count - meshData.FaceVertexBegin[face];
        }
    }

    public static void AddOpaqueVoxel(ref ulong[] opaqueMask, int x, int y, int z)
    {
        opaqueMask[y * CS_P + x] |= 1UL << z;
    }

    public static void AddNonOpaqueVoxel(ref ulong[] opaqueMask, int x, int y, int z)
    {
        opaqueMask[y * CS_P + x] &= ~(1UL << z);
    }

    public static ArrayMesh GenerateMesh(MeshData meshData)
    {
        if (meshData.Quads.Count == 0) return null;

        var surfaceArrayDict = new Dictionary<uint, SurfaceArrayData>();

        for (var face = 0; face < 6; face++)
            for (var i = meshData.FaceVertexBegin[face];
                 i < meshData.FaceVertexBegin[face] + meshData.FaceVertexLength[face];
                 i++)
                ParseQuad((Direction)face, meshData.QuadBlockIDs[i], meshData.Quads[i], surfaceArrayDict);

        var _arrayMesh = new ArrayMesh();
        foreach (var (blockInfo, surfaceArrayData) in surfaceArrayDict)
        {
            var (blockID, dir) = ParseBlockInfo(blockInfo);
            _arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArrayData.GetSurfaceArray());
            _arrayMesh.SurfaceSetMaterial(_arrayMesh.GetSurfaceCount() - 1,
                BlockManager.Instance.GetBlock(blockID).GetMaterial(dir));
        }

        return _arrayMesh;
    }

    private static (uint, Direction) ParseBlockInfo(uint blockInfo)
    {
        return ((blockInfo << 3) >> 3, (Direction)(blockInfo >> 29));
    }

    private static uint GetBlockInfo(uint blockID, Direction dir)
    {
        if (BlockManager.Instance.GetBlock(blockID) is DirectionalBlock)
            return ((uint)dir << 29) | blockID;
        return blockID;
    }

    private static void ParseQuad(Direction dir, uint blockID, ulong quad,
        Dictionary<uint, SurfaceArrayData> surfaceArrayDict)
    {
        var blockInfo = GetBlockInfo(blockID, dir);
        if (!surfaceArrayDict.ContainsKey(blockInfo))
            surfaceArrayDict.Add(blockInfo, new SurfaceArrayData());

        var surfaceArrayData = surfaceArrayDict[blockInfo];

        var x = (uint)(quad & 0x3F); // 6 bits
        var y = (uint)((quad >> 6) & 0x3F); // 6 bits
        var z = (uint)((quad >> 12) & 0x3F); // 6 bits
        var w = (uint)((quad >> 18) & 0x3F); // 6 bits (width)
        var h = (uint)((quad >> 24) & 0x3F); // 6 bits (height)
        // uint blockType = (uint)((quad >> 32) & 0x7);

        // GD.Print($"{dir.Name()}: {x},{y},{z} ({w},{h})");
        // if (dir != Direction.PositiveY && dir != Direction.NegativeY) return;
        // Color color = GetBlockColor(blockType);

        var baseIndex = surfaceArrayData.Vertices.Count;
        var corners = GetQuadCorners(dir, x, y, z, w, h);
        surfaceArrayData.Vertices.AddRange(corners);

        var normal = dir.Norm();
        for (var i = 0; i < 4; i++) surfaceArrayData.Normals.Add(normal);

        var offset = 0.0014f;

        // 标准UV映射
        if (dir == Direction.PositiveZ ||
            dir == Direction.NegativeZ ||
            dir == Direction.NegativeY)
        {
            surfaceArrayData.UVs.Add(new Vector2(offset, h - offset));
            surfaceArrayData.UVs.Add(new Vector2(offset, offset));
            surfaceArrayData.UVs.Add(new Vector2(w - offset, offset));
            surfaceArrayData.UVs.Add(new Vector2(w - offset, h - offset));
        }
        else
        {
            surfaceArrayData.UVs.Add(new Vector2(offset, w - offset));
            surfaceArrayData.UVs.Add(new Vector2(offset, offset));
            surfaceArrayData.UVs.Add(new Vector2(h - offset, offset));
            surfaceArrayData.UVs.Add(new Vector2(h - offset, w - offset));
        }

        surfaceArrayData.Indices.Add(baseIndex + 0);
        surfaceArrayData.Indices.Add(baseIndex + 1);
        surfaceArrayData.Indices.Add(baseIndex + 2);
        surfaceArrayData.Indices.Add(baseIndex + 0);
        surfaceArrayData.Indices.Add(baseIndex + 2);
        surfaceArrayData.Indices.Add(baseIndex + 3);
    }

    private static Vector3[] GetQuadCorners(Direction dir, float x, float y, float z, float w, float h)
    {
        // 0 PositiveY wDir = 0 hDir = 2
        // 1 NegativeY wDir = 0 hDir = 2
        // 2 PositiveX wDir = 1 hDir = 2
        // 3 NegativeX wDir = 1 hDir = 2
        // 4 PositiveZ wDir = 0 hDir = 1
        // 5 NegativeZ wDir = 0 hDir = 1

        switch (dir)
        {
            case Direction.PositiveY: // Y+
                return new Vector3[]
                {
                    new(x, y, z),
                    new(x + w, y, z),
                    new(x + w, y, z + h),
                    new(x, y, z + h)
                };
            case Direction.NegativeY: // Y-
                return new Vector3[]
                {
                    new(x - w, y, z),
                    new(x - w, y, z + h),
                    new(x, y, z + h),
                    new(x, y, z)
                };
            case Direction.PositiveX: // X+
                return new Vector3[]
                {
                    new(x, y - w, z + h),
                    new(x, y, z + h),
                    new(x, y, z),
                    new(x, y - w, z)
                };
            case Direction.NegativeX: // X-
                return new Vector3[]
                {
                    new(x, y, z),
                    new(x, y + w, z),
                    new(x, y + w, z + h),
                    new(x, y, z + h)
                };
            case Direction.PositiveZ: // Z+
                return new Vector3[]
                {
                    new(x - w, y, z),
                    new(x - w, y + h, z),
                    new(x, y + h, z),
                    new(x, y, z)
                };
            case Direction.NegativeZ: // Z-
                return new Vector3[]
                {
                    new(x + w, y, z),
                    new(x + w, y + h, z),
                    new(x, y + h, z),
                    new(x, y, z)
                };
            default:
                throw new ArgumentOutOfRangeException(nameof(dir), dir, null);
        }
    }

    private class SurfaceArrayData
    {
        public readonly List<int> Indices = new();
        public readonly List<Vector3> Normals = new();
        public readonly List<Vector2> UVs = new();
        public readonly List<Vector3> Vertices = new();

        public Godot.Collections.Array GetSurfaceArray()
        {
            var surfaceArray = new Godot.Collections.Array();
            surfaceArray.Resize((int)Mesh.ArrayType.Max);

            surfaceArray[(int)Mesh.ArrayType.Vertex] = Vertices.ToArray();
            surfaceArray[(int)Mesh.ArrayType.TexUV] = UVs.ToArray();
            surfaceArray[(int)Mesh.ArrayType.Normal] = Normals.ToArray();
            surfaceArray[(int)Mesh.ArrayType.Index] = Indices.ToArray();

            return surfaceArray;
        }
    }

    public class MeshData
    {
        public ulong[] FaceMasks = new ulong[CS_2 * 6];
        public int[] FaceVertexBegin = new int[6];
        public int[] FaceVertexLength = new int[6];
        public byte[] ForwardMerged = new byte[CS_2];
        public ulong[] OpaqueMask = new ulong[CS_P2];
        public List<uint> QuadBlockIDs = new(10000);
        public List<ulong> Quads = new(10000);
        public byte[] RightMerged = new byte[CS];
    }
}