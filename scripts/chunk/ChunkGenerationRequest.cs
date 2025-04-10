using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Godot;

namespace ChunkGenerator;


public class ChunkGenerationRequest
{
    public GenerationState State { get; private set; } = GenerationState.NotStarted;
    public Vector3I ChunkPosition { get; }
    public Action<ChunkGenerationResult> Callback { get; }
    public readonly WorldGenerator.WorldGenerator WorldGenerator;


    public ChunkGenerationRequest(
        WorldGenerator.WorldGenerator worldGenerator,
        Vector3I position,
        Action<ChunkGenerationResult> callback)
    {
        WorldGenerator = worldGenerator;
        ChunkPosition = position;
        Callback = callback;
    }


}
