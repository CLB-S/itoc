using System.Diagnostics;
using Godot;
using ITOC.Core.Utils;

namespace ITOC.Core.WorldGeneration;

public class WorldGenerationStep
{
    public string Id { get; }
    public Action Action { get; }
    public bool Optional { get; }

    public WorldGenerationStep(string id, Action action, bool optional = false)
    {
        if (!StringUtils.IsValidId(id))
            throw new ArgumentException(
                $"Invalid block ID format: {id}. Must be in format 'foo_bar' using lowercase letters, numbers and underscores");

        Id = id;
        Action = action;
        Optional = optional;
    }
}

public enum WorldGenerationState
{
    NotStarted,
    Generating,
    Completed,
    Failed
}

public class GenerationProgressEventArgs : EventArgs
{
    public string Message { get; set; }
    public WorldGenerationState CurrentState { get; set; }
}

public abstract class WorldGeneratorBase : IWorldGenerator
{
    private readonly Stopwatch _stopwatch = new();
    private readonly object _stateLock = new();

    protected readonly LinkedList<WorldGenerationStep> _generationPipeline = new();

    public WorldGenerationState State { get; private set; } = WorldGenerationState.NotStarted;

    public WorldSettings WorldSettings { get; protected set; }

    // Events
    public event EventHandler<GenerationProgressEventArgs> ProgressUpdatedEvent;
    public event EventHandler GenerationStartedEvent;
    public event EventHandler GenerationCompletedEvent;
    public event EventHandler<Exception> GenerationFailedEvent;

    public WorldGeneratorBase(WorldSettings settings = null)
    {
        WorldSettings = settings ?? new WorldSettings();

        InitializePipeline();
    }

    protected abstract void InitializePipeline();

    public void AddGenerationStepAfter(WorldGenerationStep step, string afterStepId)
    {
        if (State != WorldGenerationState.NotStarted && State != WorldGenerationState.Completed)
            throw new InvalidOperationException("Cannot add steps after generation has started.");

        var node = _generationPipeline.First;
        while (node != null)
        {
            if (node.Value.Id == afterStepId)
            {
                _generationPipeline.AddAfter(node, step);
                return;
            }

            node = node.Next;
        }

        throw new ArgumentException($"No step found with state {afterStepId}");
    }

    public void AddGenerationStepBefore(WorldGenerationStep step, string beforeStepId)
    {
        if (State != WorldGenerationState.NotStarted && State != WorldGenerationState.Completed)
            throw new InvalidOperationException("Cannot add steps after generation has started.");

        var node = _generationPipeline.First;
        while (node != null)
        {
            if (node.Value.Id == beforeStepId)
            {
                _generationPipeline.AddBefore(node, step);
                return;
            }

            node = node.Next;
        }

        throw new ArgumentException($"No step found with state {beforeStepId}");
    }

    public void RemoveGenerationStep(string stepId)
    {
        // TODO: Dependency check

        if (State != WorldGenerationState.NotStarted && State != WorldGenerationState.Completed)
            throw new InvalidOperationException("Cannot remove steps after generation has started.");

        var node = _generationPipeline.First;
        while (node != null)
        {
            if (node.Value.Id == stepId)
            {
                _generationPipeline.Remove(node);
                return;
            }

            node = node.Next;
        }
    }

    public void AppendGenerationStep(WorldGenerationStep step)
    {
        if (State != WorldGenerationState.NotStarted && State != WorldGenerationState.Completed)
            throw new InvalidOperationException("Cannot add steps after generation has started.");

        _generationPipeline.AddLast(step);
    }

    public void PrependGenerationStep(WorldGenerationStep step)
    {
        if (State != WorldGenerationState.NotStarted && State != WorldGenerationState.Completed)
            throw new InvalidOperationException("Cannot add steps after generation has started.");

        _generationPipeline.AddFirst(step);
    }

    public async Task GenerateWorldAsync()
    {
        try
        {
            lock (_stateLock)
            {
                if (State == WorldGenerationState.Completed)
                    ReportProgress("Warning: World generation has already been completed. Regenerating...");

                State = WorldGenerationState.Generating;
            }

            GenerationStartedEvent?.Invoke(this, EventArgs.Empty);
            _stopwatch.Restart();

            var currentNode = _generationPipeline.First;
            while (currentNode != null)
            {
                var step = currentNode.Value;
                ReportProgress($"Executing step {step.Id}.");
                await Task.Run(step.Action);
                ReportProgress($"Step {step.Id} completed.");
                currentNode = currentNode.Next;
            }

            CompleteGeneration();
        }
        catch (Exception ex)
        {
            HandleError(ex);
        }
    }

    protected void ReportProgress(string message)
    {
        ProgressUpdatedEvent?.Invoke(this, new GenerationProgressEventArgs
        {
            Message = $"[{_stopwatch.Elapsed.TotalSeconds:F2}s] {message}",
            CurrentState = State
        });
    }

    private void UpdateState(WorldGenerationState newState)
    {
        lock (_stateLock)
            State = newState;
    }

    private void CompleteGeneration()
    {
        _stopwatch.Stop();
        UpdateState(WorldGenerationState.Completed);
        ReportProgress("Generation completed");
        GenerationCompletedEvent?.Invoke(this, EventArgs.Empty);
    }

    private void HandleError(Exception ex)
    {
        UpdateState(WorldGenerationState.Failed);
        GenerationFailedEvent?.Invoke(this, ex);
    }

    public virtual double[,] CalculateChunkHeightMap(Vector2I chunkColumnIndex, Func<double, double, double> getHeight)
    {
        if (State != WorldGenerationState.Completed)
            throw new InvalidOperationException("World generation is not completed yet.");

        var rect = new Rect2I(chunkColumnIndex * Chunk.SIZE, Chunk.SIZE, Chunk.SIZE);
        return HeightMapUtils.ConstructChunkHeightMap(rect, getHeight, 2);
    }

    public abstract ChunkColumn GenerateChunkColumn(Vector2I chunkColumnIndex);
}