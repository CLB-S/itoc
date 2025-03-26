using Godot;
using System;
using System.Collections.Generic;

public static class ChunkExtensions
{
    public static ChunkMesh CreateMeshInstance(this Chunk chunk, Node parent)
    {
        var meshInstance = new ChunkMesh();
        parent.AddChild(meshInstance);
        meshInstance.Initialize(chunk);
        return meshInstance;
    }
}