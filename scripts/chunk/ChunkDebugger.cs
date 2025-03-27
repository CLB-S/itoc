using Godot;
using System;
using System.Collections.Generic;

public partial class ChunkDebugger : Node
{
    [Export]
    public ShaderMaterial ChunkMaterial;

    public override void _Ready()
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
}