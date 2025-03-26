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
        chunk.ChunkMaterial = ChunkMaterial;
        AddChild(chunk);

        var chunk2 = ChunkGenerator.GenerateDebugChunk();
        chunk2.ChunkMaterial = ChunkMaterial;
        chunk2.ChunkID = new Vector3I(1, 0, 0);
        AddChild(chunk2);

        var chunk3 = ChunkGenerator.GenerateChunkRandom(0.2f);
        chunk3.ChunkMaterial = ChunkMaterial;
        chunk3.ChunkID = new Vector3I(2, 0, 0);
        AddChild(chunk3);

        var chunk4 = ChunkGenerator.GenerateChunkRandom(0.05f);
        chunk4.ChunkMaterial = ChunkMaterial;
        chunk4.ChunkID = new Vector3I(0, -1, 0);
        AddChild(chunk4);

        var chunk5 = ChunkGenerator.GenerateChunkRandom(0.8f);
        chunk5.ChunkMaterial = ChunkMaterial;
        chunk5.ChunkID = new Vector3I(0, 0, 1);
        AddChild(chunk5);
    }
}