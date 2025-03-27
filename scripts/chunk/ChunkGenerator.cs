using Godot;
using System;
using System.Collections.Generic;

public static class ChunkGenerator
{

    public static Chunk GenerateBallChunk()
    {
        var chunk = new Chunk();
        var center = Chunk.SIZE / 2;
        var radius = Chunk.SIZE / 4;

        for (int x = 0; x < Chunk.SIZE; x++)
        {
            for (int y = 0; y < Chunk.SIZE; y++)
            {
                for (int z = 0; z < Chunk.SIZE; z++)
                {
                    var distance = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center) + (z - center) * (z - center));
                    if (distance < radius)
                    {
                        chunk.SetVoxel(x, y, z, 3);
                    }
                }
            }
        }

        return chunk;
    }

    public static Chunk GenerateDebugChunk()
    {
        var chunk = new Chunk();
        chunk.SetVoxel(0, 0, 0, 1);
        chunk.SetVoxel(0, 1, 0, 1);
        chunk.SetVoxel(0, 2, 0, 1);

        chunk.SetVoxel(2, 0, 0, 1);
        chunk.SetVoxel(2, 0, 1, 1);
        chunk.SetVoxel(2, 0, 2, 1);

        chunk.SetVoxel(0, 0, 4, 1);
        chunk.SetVoxel(1, 0, 4, 1);
        chunk.SetVoxel(2, 0, 4, 1);

        // chunk.SetVoxel(0, 1, 1, 1);
        // chunk.SetVoxel(3, 0, 0, 1);

        // chunk.SetVoxel(0, 2, 0, 1);
        // chunk.SetVoxel(0, 2, 2, 1);

        return chunk;
    }

    public static Chunk GenerateChunkRandom(float ratio = 0.5f)
    {
        var chunk = new Chunk();
        var random = new Random();
        int count = 0;

        for (int x = 0; x < Chunk.SIZE; x++)
        {
            for (int y = 0; y < Chunk.SIZE; y++)
            {
                for (int z = 0; z < Chunk.SIZE; z++)
                {
                    if (random.NextDouble() < ratio)
                    {
                        chunk.SetVoxel(x, y, z, (int)(GD.Randi() % 4 + 1));
                        count++;
                    }
                }
            }
        }

        GD.Print($"Chunk generated with {count} voxels");
        return chunk;
    }
}