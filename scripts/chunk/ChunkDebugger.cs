using Godot;
using System;
using System.Collections.Generic;

public partial class ChunkDebugger : Node
{
    [Export]
    public ShaderMaterial ChunkMaterial;

    private void DebugChunks()
    {
        var chunk = ChunkGenerator.GenerateBallChunk();
        AddChild(chunk);

        var chunk2 = ChunkGenerator.GenerateDebugChunk();
        chunk2.ChunkID = new Vector3I(1, 0, 0);
        AddChild(chunk2);

        var chunk3 = ChunkGenerator.GenerateChunkRandom(0.2f);
        chunk3.ChunkID = new Vector3I(2, 0, 0);
        AddChild(chunk3);

        var chunk4 = ChunkGenerator.GenerateChunkRandom(0.05f);
        chunk4.ChunkID = new Vector3I(0, -1, 0);
        AddChild(chunk4);

        var chunk5 = ChunkGenerator.GenerateChunkRandom(0.8f);
        chunk5.ChunkID = new Vector3I(0, 0, 1);
        AddChild(chunk5);

        var chunk6 = new Chunk();
        for (int x = 0; x < Chunk.SIZE; x++)
        {
            for (int y = 0; y < Chunk.SIZE; y++)
            {
                for (int z = 0; z < Chunk.SIZE; z++)
                {
                    if (y == Chunk.SIZE - 1)
                        chunk6.SetVoxel(x, y, z, 4);
                    else if (y > Chunk.SIZE - 4)
                        chunk6.SetVoxel(x, y, z, 3);
                    else
                        chunk6.SetVoxel(x, y, z, 2);
                }
            }
        }

        chunk6.ChunkID = new Vector3I(-2, -1, 0);
        AddChild(chunk6);
    }

    private void DebugTerrain()
    {
        var generator = new TerrainGenerator();
        for (int x = -10; x < 10; x++)
        {
            for (int z = -10; z < 10; z++)
            {
                var chunk = new Chunk();
                chunk.ChunkID = new Vector3I(x, -1, z);
                float[,] heightmap = generator.GenerateChunkHeightmap(x, z);
                TerrainGenerator.GenerateVoxelsFromHeightmap(heightmap, chunk);
                AddChild(chunk);

                var chunk1 = new Chunk();
                chunk1.ChunkID = new Vector3I(x, -2, z);
                chunk1.Fill(2);
                AddChild(chunk1);
            }
        }
    }

    public override void _Ready()
    {
        DebugTerrain();
    }
}