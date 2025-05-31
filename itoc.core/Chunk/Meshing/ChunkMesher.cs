using System.Numerics;
using Godot;
using ITOC.Core.Utils;
using Array = Godot.Collections.Array;
using Vector2 = Godot.Vector2;
using Vector3 = Godot.Vector3;

namespace ITOC.Core.ChunkMeshing;

public static class ChunkMesher
{
    #region Constants

    // Should be same as Chunk.SIZE

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

    private static ulong GetQuadV1(ulong x, ulong y, ulong z, ulong type,
        ulong halvedX, ulong halvedY, ulong halvedZ,
        ulong offsetX, ulong offsetY, ulong offsetZ,
        ulong textureId)
    {
        return (textureId << 28) |
            (offsetZ << 27) | (offsetY << 26) | (offsetX << 25) |
            (halvedZ << 24) | (halvedY << 23) | (halvedX << 22) |
            (type << 18) | (z << 12) | (y << 6) | x;
    }

    private static void ParseQuad(Direction dir, Block block, ulong quad,
        Dictionary<(Block, Direction), SurfaceArrayData> surfaceArrayDict)
    {
        if (block == null) return; // TODO: Shouldn't be null 

        var blockDirPair = block is DirectionalBlock ? (block, dir) : (block, Direction.PositiveY);
        if (!surfaceArrayDict.ContainsKey(blockDirPair))
            surfaceArrayDict.Add(blockDirPair, new SurfaceArrayData());
        var surfaceArrayData = surfaceArrayDict[blockDirPair];

        var x = quad & 0x3F;         // 6 bits
        var y = (quad >> 6) & 0x3F;  // 6 bits
        var z = (quad >> 12) & 0x3F; // 6 bits
        var w = (quad >> 18) & 0x3F; // 6 bits (width)
        var h = (quad >> 24) & 0x3F; // 6 bits (height)
        // ushort blockType = (ushort)((quad >> 32) & 0x7);

        // GD.Print($"{dir.Name()}: {x},{y},{z} ({w},{h})");
        // if (dir != Direction.PositiveY && dir != Direction.NegativeY) return;
        // Color color = GetBlockColor(blockType);

        var baseIndex = surfaceArrayData.Vertices.Count;
        Vector3[] corners;
        // if (block.Id == "itoc:water" && lod == 0)
        // {
        //     if (dir == Direction.PositiveY)
        //         corners = GetQuadCorners(dir, x, y - 0.1f, z, w, h);
        //     else if (dir == Direction.PositiveZ || dir == Direction.NegativeZ)
        //         corners = GetQuadCorners(dir, x, y, z, w, h - 0.1f);
        //     else if (dir == Direction.NegativeX)
        //         corners = GetQuadCorners(dir, x, y, z, w - 0.1f, h);
        //     else if (dir == Direction.PositiveX)
        //         corners = GetQuadCorners(dir, x, y - 0.1f, z, w - 0.1f, h);
        //     else
        //         corners = GetQuadCorners(dir, x, y, z, w, h);
        // }
        // else
        // {
        corners = GetQuadCorners(dir, x, y, z, w, h);
        // }

        surfaceArrayData.Vertices.AddRange(corners);

        var normal = dir.Norm();
        for (var i = 0; i < 4; i++) surfaceArrayData.Normals.Add(normal);

        var offset = 0.0014f;

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

    private static bool CanMergeBlock(Block blockA, Block blockB)
    {
        return ReferenceEquals(blockA, blockB);
    }

    /// <summary>
    /// Perform meshing according to the chunk data.
    /// </summary>
    public static MeshResult Mesh(Chunk chunk, MesherSettings settings = null)
    {
        settings ??= new MesherSettings();

        var meshBuffer = chunk.GetMeshBuffer();

        CullHiddenFaces(meshBuffer);

        if (settings.EnableGreedyMeshing)
            return MeshMerged(chunk, meshBuffer, settings);

        return MeshWithoutMerging(chunk, meshBuffer);
    }

    public static MeshBuffer CullHiddenFaces(MeshBuffer meshBuffer)
    {
        // System.Array.Clear(meshBuffer.FaceMasks, 0, meshBuffer.FaceMasks.Length);
        // System.Array.Clear(meshBuffer.ForwardMerged, 0, meshBuffer.ForwardMerged.Length);
        // System.Array.Clear(meshBuffer.RightMerged, 0, meshBuffer.RightMerged.Length);

        // Hidden face culling
        for (var a = 1; a < CS_P - 1; a++)
        {
            var aCS_P = a * CS_P;
            for (var b = 1; b < CS_P - 1; b++)
            {
                var baIndex = b - 1 + (a - 1) * CS;
                var abIndex = a - 1 + (b - 1) * CS;

                // 1 -> Render the face.
                var columnBits = meshBuffer.OpaqueMask[a * CS_P + b] & ~((1UL << 63) | 1);
                meshBuffer.FaceMasks[baIndex + 0 * CS_2] = (columnBits & ~meshBuffer.OpaqueMask[aCS_P + CS_P + b]) >> 1; // +Y
                meshBuffer.FaceMasks[baIndex + 1 * CS_2] = (columnBits & ~meshBuffer.OpaqueMask[aCS_P - CS_P + b]) >> 1; // -Y
                meshBuffer.FaceMasks[abIndex + 2 * CS_2] = (columnBits & ~meshBuffer.OpaqueMask[aCS_P + b + 1]) >> 1;    // +X
                meshBuffer.FaceMasks[abIndex + 3 * CS_2] = (columnBits & ~meshBuffer.OpaqueMask[aCS_P + (b - 1)]) >> 1;  // -X
                meshBuffer.FaceMasks[baIndex + 4 * CS_2] = columnBits & ~(meshBuffer.OpaqueMask[aCS_P + b] >> 1); // +Z
                meshBuffer.FaceMasks[baIndex + 5 * CS_2] = columnBits & ~(meshBuffer.OpaqueMask[aCS_P + b] << 1); // -Z

                if (meshBuffer.TransparentMask != null)
                {
                    // Water top face.
                    // meshData.FaceMasks[baIndex + 0 * CS_2] |= (meshData.TransparentMask[a * CS_P + b] & ~((1UL << 63) | 1) & ~meshData.TransparentMask[aCS_P + CS_P + b]) >> 1; // +Y

                    columnBits = (meshBuffer.TransparentMask[a * CS_P + b] | meshBuffer.OpaqueMask[a * CS_P + b]) &
                                 ~((1UL << 63) | 1);

                    meshBuffer.FaceMasks[baIndex + 0 * CS_2] |= (columnBits &
                                                               ~(meshBuffer.TransparentMask[aCS_P + CS_P + b] |
                                                                 meshBuffer.OpaqueMask[aCS_P + CS_P + b])) >> 1;
                    meshBuffer.FaceMasks[baIndex + 1 * CS_2] |= (columnBits &
                                                               ~(meshBuffer.TransparentMask[aCS_P - CS_P + b] |
                                                                 meshBuffer.OpaqueMask[aCS_P - CS_P + b])) >> 1;
                    meshBuffer.FaceMasks[abIndex + 2 * CS_2] |= (columnBits &
                                                               ~(meshBuffer.TransparentMask[aCS_P + b + 1] |
                                                                 meshBuffer.OpaqueMask[aCS_P + b + 1])) >> 1;
                    meshBuffer.FaceMasks[abIndex + 3 * CS_2] |= (columnBits &
                                                               ~(meshBuffer.TransparentMask[aCS_P + (b - 1)] |
                                                                 meshBuffer.OpaqueMask[aCS_P + (b - 1)])) >> 1;
                    meshBuffer.FaceMasks[baIndex + 4 * CS_2] |= columnBits &
                                                              ~((meshBuffer.TransparentMask[aCS_P + b] >> 1) |
                                                                (meshBuffer.OpaqueMask[aCS_P + b] >> 1));
                    meshBuffer.FaceMasks[baIndex + 5 * CS_2] |= columnBits &
                                                              ~((meshBuffer.TransparentMask[aCS_P + b] << 1) |
                                                                (meshBuffer.OpaqueMask[aCS_P + b] << 1));
                }
            }
        }

        return meshBuffer;
    }

    private static MeshResult MeshWithoutMerging(Chunk chunk, MeshBuffer meshBuffer)
    {
        var meshResult = new MeshResult();

        for (var face = 0; face < 4; face++)
        {
            var axis = face / 2;
            for (var layer = 0; layer < CS; layer++)
            {
                var bitsLocation = layer * CS + face * CS_2;
                for (var forward = 0; forward < CS; forward++)
                {
                    var bitsHere = meshBuffer.FaceMasks[forward + bitsLocation];
                    if (bitsHere == 0) continue;

                    while (bitsHere != 0)
                    {
                        var bitPos = BitOperations.TrailingZeroCount(bitsHere);
                        var block = chunk.GetBlock(axis, forward, bitPos, layer);
                        bitsHere &= ~(1UL << bitPos);

                        var meshFront = forward;
                        var meshLeft = bitPos;
                        var meshUp = layer + (~face & 1);

                        ulong quad = face switch
                        {
                            0 or 1 => GetQuadV1(
                                (ulong)meshFront,
                                (ulong)meshUp,
                                (ulong)meshLeft,
                                (ulong)face, 0, 0, 0, 0, 0, 0,
                                (ulong)block.BlockModel.GetTextureId((Direction)face)),
                            2 or 3 => GetQuadV1(
                                (ulong)meshUp,
                                (ulong)meshFront,
                                (ulong)meshLeft,
                                (ulong)face, 0, 0, 0, 0, 0, 0,
                                (ulong)block.BlockModel.GetTextureId((Direction)face)),
                            _ => 0
                        };

                        meshResult.Quads.Add(quad);
                    }
                }
            }
        }

        for (var face = 4; face < 6; face++)
        {
            var axis = face / 2;
            for (var forward = 0; forward < CS; forward++)
            {
                var bitsLocation = forward * CS + face * CS_2;
                for (var right = 0; right < CS; right++)
                {
                    var bitsHere = meshBuffer.FaceMasks[right + bitsLocation];
                    if (bitsHere == 0) continue;

                    while (bitsHere != 0)
                    {
                        var bitPos = BitOperations.TrailingZeroCount(bitsHere);
                        bitsHere &= ~(1UL << bitPos);

                        var block = chunk.GetBlock(axis, right, forward, bitPos - 1);

                        var meshLeft = right;
                        var meshFront = forward;
                        var meshUp = bitPos - 1 + (~face & 1);

                        var quad = GetQuadV1(
                            (ulong)meshLeft,
                            (ulong)meshFront,
                            (ulong)meshUp,
                            (ulong)face, 0, 0, 0, 0, 0, 0,
                            (ulong)block.BlockModel.GetTextureId((Direction)face));

                        meshResult.Quads.Add(quad);
                    }
                }
            }
        }

        return meshResult;
    }

    private static MeshResult MeshMerged(Chunk chunk, MeshBuffer meshBuffer, MesherSettings settings)
    {
        var meshResult = new MeshResult { QuadBlocks = new List<Block>(1000) };

        for (var face = 0; face < 4; face++)
        {
            var axis = face / 2;
            meshResult.FaceVertexBegin[face] = meshResult.Quads.Count;

            for (var layer = 0; layer < CS; layer++)
            {
                var bitsLocation = layer * CS + face * CS_2;
                for (var forward = 0; forward < CS; forward++)
                {
                    var bitsHere = meshBuffer.FaceMasks[forward + bitsLocation];
                    if (bitsHere == 0) continue;

                    var bitsNext = forward + 1 < CS ? meshBuffer.FaceMasks[forward + 1 + bitsLocation] : 0;

                    byte rightMerged = 1;
                    while (bitsHere != 0)
                    {
                        var bitPos = BitOperations.TrailingZeroCount(bitsHere);
                        var block = chunk.GetBlock(axis, forward, bitPos, layer);
                        var blockB = chunk.GetBlock(axis, forward + 1, bitPos, layer);
                        ref var forwardMergedRef = ref meshBuffer.ForwardMerged[bitPos];

                        if ((bitsNext & (1UL << bitPos)) != 0 &&
                            (settings.IgnoreBlockType || CanMergeBlock(block, blockB)))
                        {
                            forwardMergedRef++;
                            bitsHere &= ~(1UL << bitPos);
                            continue;
                        }

                        for (var right = bitPos + 1; right < CS; right++)
                        {
                            blockB = chunk.GetBlock(axis, forward, right, layer);
                            if ((bitsHere & (1UL << right)) == 0 ||
                                forwardMergedRef != meshBuffer.ForwardMerged[right] ||
                                (!settings.IgnoreBlockType && !CanMergeBlock(block, blockB)))
                                break;

                            meshBuffer.ForwardMerged[right] = 0;
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

                        meshResult.Quads.Add(quad);
                        meshResult.QuadBlocks.Add(block);

                        forwardMergedRef = 0;
                        rightMerged = 1;
                    }
                }
            }

            meshResult.FaceVertexLength[face] = meshResult.Quads.Count - meshResult.FaceVertexBegin[face];
        }

        for (var face = 4; face < 6; face++)
        {
            var axis = face / 2;
            meshResult.FaceVertexBegin[face] = meshResult.Quads.Count;

            for (var forward = 0; forward < CS; forward++)
            {
                var bitsLocation = forward * CS + face * CS_2;
                var bitsForwardLocation = (forward + 1) * CS + face * CS_2;

                for (var right = 0; right < CS; right++)
                {
                    var bitsHere = meshBuffer.FaceMasks[right + bitsLocation];
                    if (bitsHere == 0) continue;

                    var bitsForward = forward < CS - 1 ? meshBuffer.FaceMasks[right + bitsForwardLocation] : 0;
                    var bitsRight = right < CS - 1 ? meshBuffer.FaceMasks[right + 1 + bitsLocation] : 0;
                    var rightCS = right * CS;

                    while (bitsHere != 0)
                    {
                        var bitPos = BitOperations.TrailingZeroCount(bitsHere);
                        bitsHere &= ~(1UL << bitPos);

                        var block = chunk.GetBlock(axis, right, forward, bitPos - 1);
                        var blockB = chunk.GetBlock(axis, right, forward + 1, bitPos - 1);
                        ref var forwardMergedRef = ref meshBuffer.ForwardMerged[rightCS + (bitPos - 1)];
                        ref var rightMergedRef = ref meshBuffer.RightMerged[bitPos - 1];

                        if (rightMergedRef == 0 &&
                            (bitsForward & (1UL << bitPos)) != 0 &&
                            (settings.IgnoreBlockType || CanMergeBlock(block, blockB)))
                        {
                            forwardMergedRef++;
                            continue;
                        }

                        blockB = chunk.GetBlock(axis, right + 1, forward, bitPos - 1);
                        if ((bitsRight & (1UL << bitPos)) != 0 &&
                            forwardMergedRef == meshBuffer.ForwardMerged[rightCS + CS + (bitPos - 1)] &&
                            (settings.IgnoreBlockType || CanMergeBlock(block, blockB)))
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

                        meshResult.Quads.Add(quad);
                        meshResult.QuadBlocks.Add(block);
                        forwardMergedRef = 0;
                        rightMergedRef = 0;
                    }
                }
            }

            meshResult.FaceVertexLength[face] = meshResult.Quads.Count - meshResult.FaceVertexBegin[face];
        }

        return meshResult;
    }

    #endregion

    #region Mesh Generation

    public static Mesh GenerateCollisionMesh(Chunk chunk, MeshBuffer meshBuffer)
    {
        List<Vector3> vertices = new();
        List<int> indices = new();

        void AddQuad(Vector3[] quad)
        {
            var baseIndex = vertices.Count;
            vertices.AddRange(quad);

            indices.Add(baseIndex + 0);
            indices.Add(baseIndex + 1);
            indices.Add(baseIndex + 2);
            indices.Add(baseIndex + 0);
            indices.Add(baseIndex + 2);
            indices.Add(baseIndex + 3);
        }

        for (var face = 0; face < 4; face++)
        {
            for (var layer = 0; layer < CS; layer++)
            {
                var bitsLocation = layer * CS + face * CS_2;
                for (var forward = 0; forward < CS; forward++)
                {
                    var bitsHere = meshBuffer.FaceMasks[forward + bitsLocation];
                    if (bitsHere == 0) continue;

                    var bitsNext = forward + 1 < CS ? meshBuffer.FaceMasks[forward + 1 + bitsLocation] : 0;

                    byte rightMerged = 1;
                    while (bitsHere != 0)
                    {
                        var bitPos = BitOperations.TrailingZeroCount(bitsHere);
                        ref var forwardMergedRef = ref meshBuffer.ForwardMerged[bitPos];

                        if ((bitsNext & (1UL << bitPos)) != 0)
                        {
                            forwardMergedRef++;
                            bitsHere &= ~(1UL << bitPos);
                            continue;
                        }

                        for (var right = bitPos + 1; right < CS; right++)
                        {
                            if ((bitsHere & (1UL << right)) == 0 ||
                                forwardMergedRef != meshBuffer.ForwardMerged[right])
                                break;

                            meshBuffer.ForwardMerged[right] = 0;
                            rightMerged++;
                        }

                        bitsHere &= ~((1UL << (bitPos + rightMerged)) - 1);

                        var meshFront = forward - forwardMergedRef;
                        var meshLeft = bitPos;
                        var meshUp = layer + (~face & 1);

                        var meshWidth = rightMerged;
                        var meshLength = forwardMergedRef + 1;

                        Vector3[] quad = face switch
                        {
                            0 or 1 => GetQuadCorners((Direction)face,
                                meshFront + (face == 1 ? meshLength : 0),
                                meshUp,
                                meshLeft,
                                meshLength,
                                meshWidth),
                            2 or 3 => GetQuadCorners((Direction)face,
                                meshUp,
                                meshFront + (face == 2 ? meshLength : 0),
                                meshLeft,
                                meshLength,
                                meshWidth),
                            _ => []
                        };

                        AddQuad(quad);

                        forwardMergedRef = 0;
                        rightMerged = 1;
                    }
                }
            }
        }

        for (var face = 4; face < 6; face++)
        {
            var axis = face / 2;

            for (var forward = 0; forward < CS; forward++)
            {
                var bitsLocation = forward * CS + face * CS_2;
                var bitsForwardLocation = (forward + 1) * CS + face * CS_2;

                for (var right = 0; right < CS; right++)
                {
                    var bitsHere = meshBuffer.FaceMasks[right + bitsLocation];
                    if (bitsHere == 0) continue;

                    var bitsForward = forward < CS - 1 ? meshBuffer.FaceMasks[right + bitsForwardLocation] : 0;
                    var bitsRight = right < CS - 1 ? meshBuffer.FaceMasks[right + 1 + bitsLocation] : 0;
                    var rightCS = right * CS;

                    while (bitsHere != 0)
                    {
                        var bitPos = BitOperations.TrailingZeroCount(bitsHere);
                        bitsHere &= ~(1UL << bitPos);

                        var block = chunk.GetBlock(axis, right, forward, bitPos - 1);
                        ref var forwardMergedRef = ref meshBuffer.ForwardMerged[rightCS + (bitPos - 1)];
                        ref var rightMergedRef = ref meshBuffer.RightMerged[bitPos - 1];

                        if (rightMergedRef == 0 &&
                            (bitsForward & (1UL << bitPos)) != 0)
                        {
                            forwardMergedRef++;
                            continue;
                        }

                        if ((bitsRight & (1UL << bitPos)) != 0 &&
                            forwardMergedRef == meshBuffer.ForwardMerged[rightCS + CS + (bitPos - 1)])
                        {
                            forwardMergedRef = 0;
                            rightMergedRef++;
                            continue;
                        }

                        var meshLeft = right - rightMergedRef;
                        var meshFront = forward - forwardMergedRef;
                        var meshUp = bitPos - 1 + (~face & 1);
                        var meshWidth = 1 + rightMergedRef;
                        var meshLength = 1 + forwardMergedRef;

                        var quad = GetQuadCorners((Direction)face,
                            face == 4 ? meshLeft + meshWidth : meshLeft,
                            meshFront,
                            meshUp,
                            meshWidth,
                            meshLength
                        );

                        AddQuad(quad);

                        forwardMergedRef = 0;
                        rightMergedRef = 0;
                    }
                }
            }
        }

        var arrMesh = new ArrayMesh();
        Array arrays = [];
        arrays.Resize((int)Godot.Mesh.ArrayType.Max);
        arrays[(int)Godot.Mesh.ArrayType.Vertex] = vertices.ToArray();
        arrays[(int)Godot.Mesh.ArrayType.Index] = indices.ToArray();
        arrMesh.AddSurfaceFromArrays(Godot.Mesh.PrimitiveType.Triangles, arrays);
        return arrMesh;
    }

    public static Mesh GenerateMesh(MeshResult meshResult, Material materialOverride = null)
    {
        if (meshResult.Quads.Count == 0) return null;

        var surfaceArrayDict = new Dictionary<(Block, Direction), SurfaceArrayData>();

        for (var face = 0; face < 6; face++)
            for (var i = meshResult.FaceVertexBegin[face];
                 i < meshResult.FaceVertexBegin[face] + meshResult.FaceVertexLength[face];
                 i++)
                ParseQuad((Direction)face, meshResult.QuadBlocks[i], meshResult.Quads[i], surfaceArrayDict);

        var _arrayMesh = new ArrayMesh();
        foreach (var ((block, dir), surfaceArrayData) in surfaceArrayDict)
        {
            _arrayMesh.AddSurfaceFromArrays(Godot.Mesh.PrimitiveType.Triangles, surfaceArrayData.GetSurfaceArray());
            _arrayMesh.SurfaceSetMaterial(_arrayMesh.GetSurfaceCount() - 1,
                materialOverride ?? block.BlockModel.GetMaterial(dir));
        }

        return _arrayMesh;
    }

    public static Mesh GenerateMeshV1(MeshResult meshResult)
    {
        // Create ArrayMesh using only indices. The shader will recover the mesh using vertex pulling.
        // This is not the best approach, but it works though.
        // May use RenderingDevice later to improve it.

        if (meshResult.Quads.Count == 0) return null;

        // Vertices ArrayMesh

        var arrMesh = new ArrayMesh();
        Array arrays = [];
        arrays.Resize((int)Godot.Mesh.ArrayType.Max);

        var numVertices = meshResult.Quads.Count * 6;
        var arr = new int[numVertices];
        for (int i = 0; i < numVertices; i++)
            arr[i] = i;

        arrays[(int)Godot.Mesh.ArrayType.Index] = arr;

        arrMesh.AddSurfaceFromArrays(Godot.Mesh.PrimitiveType.Triangles, arrays,
            flags: Godot.Mesh.ArrayFormat.FlagUsesEmptyVertexArray);

        arrMesh.CustomAabb = new Aabb(Vector3.Zero, Vector3.One * Chunk.SIZE);

        // Texture Buffer

        var bufferBytes = BitPacker.PackUInt64Array(meshResult.Quads.ToArray()); // numPixels * 4 bytes
        var (bufferTexture, width) = TextureBuffer.Create(bufferBytes);

        // Meterial

        var shader = ResourceLoader.Load<Shader>("res://assets/shaders/quads.gdshader");
        var material = new ShaderMaterial { Shader = shader };

        material.SetShaderParameter("texture_buff", bufferTexture);
        material.SetShaderParameter("texture_buff_width", width);

        arrMesh.SurfaceSetMaterial(0, material);

        return arrMesh;
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
            surfaceArray.Resize((int)Godot.Mesh.ArrayType.Max);

            surfaceArray[(int)Godot.Mesh.ArrayType.Vertex] = Vertices.ToArray();
            surfaceArray[(int)Godot.Mesh.ArrayType.TexUV] = UVs.ToArray();
            surfaceArray[(int)Godot.Mesh.ArrayType.Normal] = Normals.ToArray();
            surfaceArray[(int)Godot.Mesh.ArrayType.Index] = Indices.ToArray();

            return surfaceArray;
        }
    }

    #endregion
}