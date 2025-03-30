using System;
using System.Collections.Generic;
using System.Numerics;
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


    public static void MeshVoxels(int[] voxels, MeshData meshData)
    {
        meshData.Quads.Clear();
        meshData.QuadBlockIDs.Clear();

        Array.Clear(meshData.FaceMasks, 0, meshData.FaceMasks.Length);
        Array.Clear(meshData.ForwardMerged, 0, meshData.ForwardMerged.Length);
        Array.Clear(meshData.RightMerged, 0, meshData.RightMerged.Length);

        // Step 1: 生成面掩码
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

        // Step 2: 贪婪合并（前四个面）
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

        // Step 3: 处理最后两个面（Godot坐标系调整）
        // 完整的贪婪合并逻辑（包含face 4-5的处理）
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

                        var blockID = voxels[GetAxisIndex(axis, right + 1, forward + 1, bitPos)];
                        ref var forwardMergedRef = ref meshData.ForwardMerged[rightCS + (bitPos - 1)];
                        ref var rightMergedRef = ref meshData.RightMerged[bitPos - 1];

                        // 前向合并检查
                        if (rightMergedRef == 0 &&
                            (bitsForward & (1UL << bitPos)) != 0 &&
                            blockID == voxels[GetAxisIndex(axis, right + 1, forward + 2, bitPos)])
                        {
                            forwardMergedRef++;
                            continue;
                        }

                        // 右向合并检查
                        if ((bitsRight & (1UL << bitPos)) != 0 &&
                            forwardMergedRef == meshData.ForwardMerged[rightCS + CS + (bitPos - 1)] &&
                            blockID == voxels[GetAxisIndex(axis, right + 2, forward + 1, bitPos)])
                        {
                            forwardMergedRef = 0;
                            rightMergedRef++;
                            continue;
                        }

                        // 计算合并后的四边形参数
                        var meshLeft = (byte)(right - rightMergedRef);
                        var meshFront = (byte)(forward - forwardMergedRef);
                        var meshUp = (byte)(bitPos - 1 + (~face & 1));
                        var meshWidth = (byte)(1 + rightMergedRef);
                        var meshLength = (byte)(1 + forwardMergedRef);

                        // 生成四边形数据（Godot坐标系调整）
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

    public static void AddNonOpaqueVoxel(ref ulong[] opaqueMask, int x, int y, int z)
    {
        opaqueMask[y * CS_P + x] |= 1UL << z;
    }

    public class MeshData
    {
        public ulong[] FaceMasks = new ulong[CS_2 * 6];
        public int[] FaceVertexBegin = new int[6];
        public int[] FaceVertexLength = new int[6];
        public byte[] ForwardMerged = new byte[CS_2];
        public ulong[] OpaqueMask = new ulong[CS_P2];
        public List<int> QuadBlockIDs = new(10000);
        public List<ulong> Quads = new(10000);
        public byte[] RightMerged = new byte[CS];
    }
}