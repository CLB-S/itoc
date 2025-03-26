using Godot;
using System;
using System.Diagnostics;

public static class ChunkMeshPreGenerator
{
    public const int SIZE = 32;

    public static void GenerateAllFaces(Chunk chunk)
    {
        ProcessDirection(chunk, Axis.X, Direction.PositiveX, Direction.NegativeX);
        ProcessDirection(chunk, Axis.Y, Direction.PositiveY, Direction.NegativeY);
        ProcessDirection(chunk, Axis.Z, Direction.PositiveZ, Direction.NegativeZ);
    }

    private static void ProcessDirection(Chunk chunk, Axis axis, Direction dirPos, Direction dirNeg)
    {

        // 遍历主轴的每个层
        for (int layer = 0; layer <= SIZE; layer++)
        {
            // 构建当前层的材质矩阵
            int[,] materialMatrixPos = new int[SIZE, SIZE];
            int[,] materialMatrixNeg = new int[SIZE, SIZE];
            for (int a = 0; a < SIZE; a++)
            {
                for (int b = 0; b < SIZE; b++)
                {
                    Vector3I voxelPos = ChunkHelper.GetVoxelPosition(axis, layer, a, b);
                    if (IsFaceVisible(chunk, voxelPos, dirPos))
                    {
                        materialMatrixPos[a, b] = chunk.GetVoxel(voxelPos.X, voxelPos.Y, voxelPos.Z);
                    }

                    if (IsFaceVisible(chunk, voxelPos, dirNeg))
                    {
                        materialMatrixNeg[a, b] = chunk.GetVoxel(voxelPos.X, voxelPos.Y, voxelPos.Z);
                    }
                }
            }

            // 贪心算法合并
            GreedyMerge(materialMatrixPos, dirPos, layer, chunk.Faces[dirPos]);
            GreedyMerge(materialMatrixNeg, dirNeg, layer, chunk.Faces[dirNeg]);
        }
    }

    private static bool IsFaceVisible(Chunk chunk, Vector3I voxelPos, Direction dir)
    {
        Vector3I facePos = ChunkHelper.GetFacePosition(voxelPos, dir);

        // GetVoxel returns 0 if the voxel is out of bounds
        return chunk.GetVoxel(facePos.X, facePos.Y, facePos.Z) == 0;
    }

    private static void GreedyMerge(int[,] matrix, Direction dir, int layer, FaceData faceData)
    {
        bool[,] merged = new bool[SIZE, SIZE];

        for (int y = 0; y < SIZE; y++)
        {
            for (int x = 0; x < SIZE; x++)
            {
                int currentMat = matrix[y, x];
                if (merged[y, x] || currentMat == 0) continue;

                int width = 1;
                while (x + width < SIZE && matrix[y, x + width] == currentMat && !merged[y, x + width])
                    width++;

                int height = 1;
                bool canExpand = y + height < SIZE;
                while (canExpand)
                {
                    for (int i = 0; i < width; i++)
                    {
                        if (merged[y + height, x + i] || matrix[y + height, x + i] != currentMat)
                        {
                            canExpand = false;
                            break;
                        }
                    }
                    if (canExpand) height++;
                }

                // 记录合并后的面
                Vector3I startPos = ChunkHelper.GetFaceStartPosition(dir, layer, x, y);
                faceData.Rects.Add(new FaceRect(startPos, width, height, currentMat, dir));

                // 标记已合并区域
                for (int h = 0; h < height; h++)
                    for (int w = 0; w < width; w++)
                        merged[y + h, x + w] = true;
            }
        }
    }


}