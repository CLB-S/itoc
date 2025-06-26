namespace ITOC.Core.WorldGeneration;

public enum WorldGenerationState
{
    NotStarted,
    PreGeneration,
    Ready,
    Failed
}

public class GenerationProgressEventArgs : EventArgs
{
    public string Message { get; set; }
    public WorldGenerationState CurrentState { get; set; }
}

public interface IWorldGenerator
{
    WorldGenerationState State { get; }
    WorldSettings WorldSettings { get; } // TODO: Configuration system

    ChunkGeneratorBase ChunkGenerator { get; }

    // Events
    event EventHandler<GenerationProgressEventArgs> ProgressUpdated;
    event EventHandler PreGenerationStarted;
    event EventHandler<Exception> GenerationFailed;
    event EventHandler Ready;

    void BeginWorldPreGeneration();
}