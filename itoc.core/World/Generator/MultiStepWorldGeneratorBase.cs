using System.Diagnostics;
using Godot;
using ITOC.Core.PatternSystem;
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
                $"Invalid block ID format: {id}. Must be in format 'foo_bar' using lowercase letters, numbers and underscores"
            );

        Id = id;
        Action = action;
        Optional = optional;
    }
}

public abstract class MultiStepWorldGeneratorBase : IWorldGenerator
{
    private readonly Stopwatch _stopwatch = new();
    private readonly object _stateLock = new();

    protected readonly LinkedList<WorldGenerationStep> _generationPipeline = new();

    public WorldGenerationState State { get; private set; } = WorldGenerationState.NotStarted;

    public WorldSettings WorldSettings { get; protected set; }

    public ChunkGeneratorBase ChunkGenerator { get; protected set; }

    // Events

    public event EventHandler<GenerationProgressEventArgs> ProgressUpdated;
    public event EventHandler PreGenerationStarted;
    public event EventHandler Ready;
    public event EventHandler<Exception> GenerationFailed;

    public MultiStepWorldGeneratorBase(WorldSettings settings = null)
    {
        WorldSettings = settings ?? new WorldSettings();

        InitializePipeline();
        ChunkGenerator = InitializeChunkGenerator();
    }

    protected abstract void InitializePipeline();
    protected abstract ChunkGeneratorBase InitializeChunkGenerator();

    public void AddGenerationStepAfter(WorldGenerationStep step, string afterStepId)
    {
        if (State != WorldGenerationState.NotStarted && State != WorldGenerationState.Ready)
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
        if (State != WorldGenerationState.NotStarted && State != WorldGenerationState.Ready)
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

        if (State != WorldGenerationState.NotStarted && State != WorldGenerationState.Ready)
            throw new InvalidOperationException(
                "Cannot remove steps after generation has started."
            );

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
        if (State != WorldGenerationState.NotStarted && State != WorldGenerationState.Ready)
            throw new InvalidOperationException("Cannot add steps after generation has started.");

        _generationPipeline.AddLast(step);
    }

    public void PrependGenerationStep(WorldGenerationStep step)
    {
        if (State != WorldGenerationState.NotStarted && State != WorldGenerationState.Ready)
            throw new InvalidOperationException("Cannot add steps after generation has started.");

        _generationPipeline.AddFirst(step);
    }

    public void BeginWorldPreGeneration()
    {
        try
        {
            lock (_stateLock)
            {
                if (State == WorldGenerationState.Ready)
                    ReportProgress(
                        "Warning: World generation has already been completed. Regenerating..."
                    );

                State = WorldGenerationState.PreGeneration;
            }

            PreGenerationStarted?.Invoke(this, EventArgs.Empty);
            _stopwatch.Restart();

            var currentNode = _generationPipeline.First;
            while (currentNode != null)
            {
                var step = currentNode.Value;
                ReportProgress($"Executing step {step.Id}.");
                step.Action?.Invoke();
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

    protected void ReportProgress(string message) =>
        ProgressUpdated?.Invoke(
            this,
            new GenerationProgressEventArgs
            {
                Message = $"[{_stopwatch.Elapsed.TotalSeconds:F2}s] {message}",
                CurrentState = State,
            }
        );

    private void UpdateState(WorldGenerationState newState)
    {
        lock (_stateLock)
            State = newState;
    }

    private void CompleteGeneration()
    {
        _stopwatch.Stop();
        UpdateState(WorldGenerationState.Ready);
        ReportProgress("Generation completed");
        Ready?.Invoke(this, EventArgs.Empty);
    }

    private void HandleError(Exception ex)
    {
        UpdateState(WorldGenerationState.Failed);
        GenerationFailed?.Invoke(this, ex);
    }

    public virtual double[,] CalculateChunkHeightMap(
        Vector2I chunkColumnIndex,
        Func<double, double, double> getHeight
    )
    {
        if (State != WorldGenerationState.Ready)
            throw new InvalidOperationException("World generation is not completed yet.");

        var rect = new Rect2I(chunkColumnIndex * Chunk.SIZE, Chunk.SIZE, Chunk.SIZE);
        return HeightMapUtils.ConstructChunkHeightMap(rect, getHeight, 2);
    }

    #region Utils

    public static Vector2 Warp(Vector2 point, PatternTreeNode pattern)
    {
        var warpedPoint = new Vector2(point.X, point.Y);
        warpedPoint.X += pattern.Evaluate(warpedPoint.X, warpedPoint.Y);
        warpedPoint.Y += pattern.Evaluate(warpedPoint.Y, warpedPoint.X);
        return warpedPoint;
    }

    #endregion
}
