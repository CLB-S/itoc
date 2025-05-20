using System;
using System.Threading.Tasks;
using Godot;

namespace ITOC.WorldGeneration;

public interface IWorldGenerator
{
    WorldGenerationState State { get; }

    // Events
    event EventHandler<GenerationProgressEventArgs> ProgressUpdatedEvent;
    event EventHandler GenerationStartedEvent;
    event EventHandler GenerationCompletedEvent;
    event EventHandler<Exception> GenerationFailedEvent;

    Task GenerateWorldAsync();
    ChunkColumn GenerateChunkColumn(Vector2I chunkColumnIndex);
}