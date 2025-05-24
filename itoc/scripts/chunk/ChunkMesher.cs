using System;
using System.Collections.Generic;
using System.Numerics;
using Godot;
using Array = Godot.Collections.Array;
using Vector2 = Godot.Vector2;
using Vector3 = Godot.Vector3;

namespace ITOC;

public static class ChunkMesher
{
    #region Constants

    public const int CS = 62;
    public const int CS_P = CS + 2;
    public const int CS_2 = CS * CS;
    public const int CS_3 = CS * CS * CS;
    public const int CS_P2 = CS_P * CS_P;
    public const int CS_P3 = CS_P * CS_P * CS_P;

    #endregion

    #region Index Utils

    public static int GetAxisIndex(int axis, int a, int b, int c, int size = CS_P)
    {
        return axis switch
        {
            0 => b + a * size + c * size * size,
            1 => b + c * size + a * size * size,
            _ => c + a * size + b * size * size
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

    public static int GetBlockAxisIndex(int axis, int a, int b, int c)
    {
        return GetAxisIndex(axis, a, b, c, CS);
    }

    public static int GetBlockIndex(int x, int y, int z)
    {
        return z + x * CS + y * CS_2;
    }

    public static (int x, int y, int z) GetBlockIndex(int index)
    {
        var z = index % CS;
        index -= z;
        var x = (index / CS) % CS;
        index -= x * CS;
        var y = index / CS_2;
        return (x, y, z);
    }

    public static int GetBlockIndex(Vector3I vec)
    {
        return vec.Z + vec.X * CS + vec.Y * CS_2;
    }

    #endregion


    #region Mask Utils

    public static void AddOpaqueVoxel(ulong[] opaqueMask, int x, int y, int z)
    {
        opaqueMask[y * CS_P + x] |= 1UL << z;
    }

    public static void AddNonOpaqueVoxel(ulong[] opaqueMask, int x, int y, int z)
    {
        opaqueMask[y * CS_P + x] &= ~(1UL << z);
    }

    #endregion

    #region Quads

    private static ulong GetQuad(ulong x, ulong y, ulong z, ulong w, ulong h, ulong type)
    {
        return (type << 32) | (h << 24) | (w << 18) | (z << 12) | (y << 6) | x;
    }

    private static void ParseQuad(Direction dir, Block block, ulong quad, int lod,
        Dictionary<(Block, Direction), SurfaceArrayData> surfaceArrayDict)
    {
        if (block == null) return; // TODO: Shouldn't be null 

        var blockDirPair = block is DirectionalBlock ? (block, dir) : (block, Direction.PositiveY);
        if (!surfaceArrayDict.ContainsKey(blockDirPair))
            surfaceArrayDict.Add(blockDirPair, new SurfaceArrayData());
        var surfaceArrayData = surfaceArrayDict[blockDirPair];

        var x = (quad & 0x3F) << lod;         // 6 bits
        var y = ((quad >> 6) & 0x3F) << lod;  // 6 bits
        var z = ((quad >> 12) & 0x3F) << lod; // 6 bits
        var w = ((quad >> 18) & 0x3F) << lod; // 6 bits (width)
        var h = ((quad >> 24) & 0x3F) << lod; // 6 bits (height)
        // ushort blockType = (ushort)((quad >> 32) & 0x7);

        // GD.Print($"{dir.Name()}: {x},{y},{z} ({w},{h})");
        // if (dir != Direction.PositiveY && dir != Direction.NegativeY) return;
        // Color color = GetBlockColor(blockType);

        var baseIndex = surfaceArrayData.Vertices.Count;
        Vector3[] corners;
        if (block.Id == "itoc:water" && lod == 0)
        {
            if (dir == Direction.PositiveY)
                corners = GetQuadCorners(dir, x, y - 0.1f, z, w, h);
            else if (dir == Direction.PositiveZ || dir == Direction.NegativeZ)
                corners = GetQuadCorners(dir, x, y, z, w, h - 0.1f);
            else if (dir == Direction.NegativeX)
                corners = GetQuadCorners(dir, x, y, z, w - 0.1f, h);
            else if (dir == Direction.PositiveX)
                corners = GetQuadCorners(dir, x, y - 0.1f, z, w - 0.1f, h);
            else
                corners = GetQuadCorners(dir, x, y, z, w, h);
        }
        else
        {
            corners = GetQuadCorners(dir, x, y, z, w, h);
        }

        surfaceArrayData.Vertices.AddRange(corners);

        var normal = dir.Norm();
        for (var i = 0; i < 4; i++) surfaceArrayData.Normals.Add(normal);

        var offset = 0.0014f;

        h >>= lod;
        w >>= lod;

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
        return dir switch
        {
            Direction.PositiveY => [
                new(x, y, z),
                new(x + w, y, z),
                new(x + w, y, z + h),
                new(x, y, z + h)],
            Direction.NegativeY => [
                new(x - w, y, z),
                new(x - w, y, z + h),
                new(x, y, z + h),
                new(x, y, z)],
            Direction.PositiveX => [
                new(x, y - w, z + h),
                new(x, y, z + h),
                new(x, y, z),
                new(x, y - w, z)],
            Direction.NegativeX => [
                new(x, y, z),
                new(x, y + w, z),
                new(x, y + w, z + h),
                new(x, y, z + h)],
            Direction.PositiveZ => [
                new(x - w, y, z),
                new(x - w, y + h, z),
                new(x, y + h, z),
                new(x, y, z)],
            Direction.NegativeZ => [
                new(x + w, y, z),
                new(x + w, y + h, z),
                new(x, y + h, z),
                new(x, y, z)],
            _ => throw new ArgumentOutOfRangeException(nameof(dir), dir, null),
        };
    }

    #endregion

    #region Meshing

    /// <summary>
    /// Calculate MeshData according to the chunk data.
    /// </summary>
    public static void MeshChunk(Chunk chunk, MeshData meshData)
    {
        meshData.Quads.Clear();
        meshData.QuadBlocks.Clear();

        System.Array.Clear(meshData.FaceMasks, 0, meshData.FaceMasks.Length);
        System.Array.Clear(meshData.ForwardMerged, 0, meshData.ForwardMerged.Length);
        System.Array.Clear(meshData.RightMerged, 0, meshData.RightMerged.Length);

        // Hidden face culling
        for (var a = 1; a < CS_P - 1; a++)
        {
            var aCS_P = a * CS_P;
            for (var b = 1; b < CS_P - 1; b++)
            {
                var baIndex = b - 1 + (a - 1) * CS;
                var abIndex = a - 1 + (b - 1) * CS;

                // 1 -> Render the face.
                var columnBits = meshData.OpaqueMask[a * CS_P + b] & ~((1UL << 63) | 1);
                meshData.FaceMasks[baIndex + 0 * CS_2] = (columnBits & ~meshData.OpaqueMask[aCS_P + CS_P + b]) >> 1; // +Y
                meshData.FaceMasks[baIndex + 1 * CS_2] = (columnBits & ~meshData.OpaqueMask[aCS_P - CS_P + b]) >> 1; // -Y
                meshData.FaceMasks[abIndex + 2 * CS_2] = (columnBits & ~meshData.OpaqueMask[aCS_P + b + 1]) >> 1;    // +X
                meshData.FaceMasks[abIndex + 3 * CS_2] = (columnBits & ~meshData.OpaqueMask[aCS_P + (b - 1)]) >> 1;  // -X
                meshData.FaceMasks[baIndex + 4 * CS_2] = columnBits & ~(meshData.OpaqueMask[aCS_P + b] >> 1); // +Z
                meshData.FaceMasks[baIndex + 5 * CS_2] = columnBits & ~(meshData.OpaqueMask[aCS_P + b] << 1); // -Z

                if (meshData.WaterMasks != null)
                {
                    // Water top face.
                    meshData.FaceMasks[baIndex + 0 * CS_2] |= (meshData.WaterMasks[a * CS_P + b] & ~((1UL << 63) | 1) & ~meshData.WaterMasks[aCS_P + CS_P + b]) >> 1; // +Y

                    columnBits = (meshData.WaterMasks[a * CS_P + b] | meshData.OpaqueMask[a * CS_P + b]) &
                                 ~((1UL << 63) | 1);
                    meshData.FaceMasks[baIndex + 1 * CS_2] |= (columnBits &
                                                               ~(meshData.WaterMasks[aCS_P - CS_P + b] |
                                                                 meshData.OpaqueMask[aCS_P - CS_P + b])) >> 1;
                    meshData.FaceMasks[abIndex + 2 * CS_2] |= (columnBits &
                                                               ~(meshData.WaterMasks[aCS_P + b + 1] |
                                                                 meshData.OpaqueMask[aCS_P + b + 1])) >> 1;
                    meshData.FaceMasks[abIndex + 3 * CS_2] |= (columnBits &
                                                               ~(meshData.WaterMasks[aCS_P + (b - 1)] |
                                                                 meshData.OpaqueMask[aCS_P + (b - 1)])) >> 1;
                    meshData.FaceMasks[baIndex + 4 * CS_2] |= columnBits &
                                                              ~((meshData.WaterMasks[aCS_P + b] >> 1) |
                                                                (meshData.OpaqueMask[aCS_P + b] >> 1));
                    meshData.FaceMasks[baIndex + 5 * CS_2] |= columnBits &
                                                              ~((meshData.WaterMasks[aCS_P + b] << 1) |
                                                                (meshData.OpaqueMask[aCS_P + b] << 1));
                }
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
                        var bitPos = BitOperations.TrailingZeroCount(bitsHere);
                        var block = chunk.GetBlock(axis, forward, bitPos, layer);
                        ref var forwardMergedRef = ref meshData.ForwardMerged[bitPos];

                        if ((bitsNext & (1UL << bitPos)) != 0 &&
                            block == chunk.GetBlock(axis, forward + 1, bitPos, layer))
                        {
                            forwardMergedRef++;
                            bitsHere &= ~(1UL << bitPos);
                            continue;
                        }

                        for (var right = bitPos + 1; right < CS; right++)
                        {
                            if ((bitsHere & (1UL << right)) == 0 ||
                                forwardMergedRef != meshData.ForwardMerged[right] ||
                                block != chunk.GetBlock(axis, forward, right, layer))
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
                        meshData.QuadBlocks.Add(block);

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
                        var bitPos = BitOperations.TrailingZeroCount(bitsHere);
                        bitsHere &= ~(1UL << bitPos);

                        var block = chunk.GetBlock(axis, right, forward, bitPos - 1);
                        ref var forwardMergedRef = ref meshData.ForwardMerged[rightCS + (bitPos - 1)];
                        ref var rightMergedRef = ref meshData.RightMerged[bitPos - 1];

                        if (rightMergedRef == 0 &&
                            (bitsForward & (1UL << bitPos)) != 0 &&
                            block == chunk.GetBlock(axis, right, forward + 1, bitPos - 1))
                        {
                            forwardMergedRef++;
                            continue;
                        }

                        if ((bitsRight & (1UL << bitPos)) != 0 &&
                            forwardMergedRef == meshData.ForwardMerged[rightCS + CS + (bitPos - 1)] &&
                            block == chunk.GetBlock(axis, right + 1, forward, bitPos - 1))
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
                        meshData.QuadBlocks.Add(block);
                        forwardMergedRef = 0;
                        rightMergedRef = 0;
                    }
                }
            }

            meshData.FaceVertexLength[face] = meshData.Quads.Count - meshData.FaceVertexBegin[face];
        }
    }

    #endregion

    #region Mesh Generation

    public static ArrayMesh GenerateMesh(MeshData meshData)
    {
        if (meshData.Quads.Count == 0) return null;

        var surfaceArrayDict = new Dictionary<(Block, Direction), SurfaceArrayData>();

        for (var face = 0; face < 6; face++)
            for (var i = meshData.FaceVertexBegin[face];
                 i < meshData.FaceVertexBegin[face] + meshData.FaceVertexLength[face];
                 i++)
                ParseQuad((Direction)face, meshData.QuadBlocks[i], meshData.Quads[i], meshData.Lod, surfaceArrayDict);

        var _arrayMesh = new ArrayMesh();
        foreach (var ((block, dir), surfaceArrayData) in surfaceArrayDict)
        {
            _arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArrayData.GetSurfaceArray());
            _arrayMesh.SurfaceSetMaterial(_arrayMesh.GetSurfaceCount() - 1,
                block.BlockModel.GetMaterial(dir));
        }

        return _arrayMesh;
    }

    #endregion

    #region Data Classes

    private class SurfaceArrayData
    {
        public readonly List<int> Indices = new();
        public readonly List<Vector3> Normals = new();
        public readonly List<Vector2> UVs = new();
        public readonly List<Vector3> Vertices = new();

        public Array GetSurfaceArray()
        {
            var surfaceArray = new Array();
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
        public int Lod = 0;
        public ulong[] FaceMasks = new ulong[CS_2 * 6];
        public int[] FaceVertexBegin = new int[6];
        public int[] FaceVertexLength = new int[6];
        public byte[] ForwardMerged = new byte[CS_2];
        public ulong[] OpaqueMask; // Each mask is 32 KB.
        public ulong[] WaterMasks;
        public List<Block> QuadBlocks = new(1000);
        public List<ulong> Quads = new(1000);
        public byte[] RightMerged = new byte[CS];

        public MeshData(ulong[] opaqueMask, ulong[] waterMasks = null)
        {
            OpaqueMask = opaqueMask;
            WaterMasks = waterMasks;
        }

        public MeshData()
        {
            OpaqueMask = new ulong[CS_P2];
        }
    }

    #endregion
}