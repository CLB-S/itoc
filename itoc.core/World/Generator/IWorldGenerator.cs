namespace ITOC.Core.WorldGeneration;

public interface IWorldGenerator
{
    WorldGenerationState State { get; }
    WorldSettings WorldSettings { get; }

    // Events
    event EventHandler<GenerationProgressEventArgs> ProgressUpdatedEvent;
    event EventHandler GenerationStartedEvent;
    event EventHandler GenerationCompletedEvent;
    event EventHandler<Exception> GenerationFailedEvent;

    Task GenerateWorldAsync();

    ChunkGeneratorBase ChunkGenerator { get; }
}