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
        var rect = new Rect2(ChunkColumnPosition.X * ChunkMesher.CS, ChunkColumnPosition.Y * ChunkMesher.CS, ChunkMesher.CS_P, ChunkMesher.CS_P);
        var heightMap = WorldGenerator.CalculateHeightMap(ChunkMesher.CS_P, ChunkMesher.CS_P, rect);
        return new ChunkColumn(ChunkColumnPosition, heightMap);
    }
}
