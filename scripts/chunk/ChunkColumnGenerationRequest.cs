using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Godot;

namespace ChunkGenerator;


public class ChunkColumnGenerationRequest
{
    public Vector2I ChunkColumnPosition { get; }
    public Action<ChunkColumn> Callback { get; }
    public readonly WorldGenerator.WorldGenerator WorldGenerator;

    public ChunkColumnGenerationRequest(
        WorldGenerator.WorldGenerator worldGenerator,
        Vector2I position,
        Action<ChunkColumn> callback)
    {
        WorldGenerator = worldGenerator;
        ChunkColumnPosition = position;
        Callback = callback;
    }

    public ChunkColumn Excute()
    {
        var heightMap = WorldGenerator.CalculateChunkHeightMap(ChunkColumnPosition);
        return new ChunkColumn(ChunkColumnPosition, heightMap);
    }
}
