using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Godot;

namespace ChunkGenerator;



public class GenerationStep
{
    public GenerationState State { get; }
    public Action Action { get; }
    public bool Optional { get; }

    public GenerationStep(GenerationState state, Action action, bool optional = false)
    {
        State = state;
        Action = action;
        Optional = optional;
    }
}

public partial class ChunkGenerationRequest
{
    public GenerationState State { get; private set; } = GenerationState.NotStarted;
    public Vector3I ChunkPosition { get; }
    public Action<ChunkGenerationResult> Callback { get; }

    private readonly LinkedList<GenerationStep> _generationPipeline = new();
    private readonly Stopwatch _stopwatch = new();
    private readonly WorldGenerator.WorldGenerator _worldGenerator;


    public ChunkGenerationRequest(WorldGenerator.WorldGenerator worldGenerator, Vector3I position, Action<ChunkGenerationResult> callback)
    {
        _worldGenerator = worldGenerator;
        ChunkPosition = position;
        Callback = callback;
        InitializePipeline();
    }

    public void AddGenerationStepAfter(GenerationStep step, GenerationState afterState)
    {
        if (State != GenerationState.NotStarted && State != GenerationState.Completed)
            throw new InvalidOperationException("Cannot add steps after generation has started.");

        var node = _generationPipeline.First;
        while (node != null)
        {
            if (node.Value.State == afterState)
            {
                _generationPipeline.AddAfter(node, step);
                return;
            }
            node = node.Next;
        }

        throw new ArgumentException($"No step found with state {afterState}");
    }

    public void AddGenerationStepBefore(GenerationStep step, GenerationState beforeState)
    {
        if (State != GenerationState.NotStarted && State != GenerationState.Completed)
            throw new InvalidOperationException("Cannot add steps after generation has started.");

        if (beforeState == GenerationState.Completed)
        {
            _generationPipeline.AddLast(step);
            return;
        }

        var node = _generationPipeline.First;
        while (node != null)
        {
            if (node.Value.State == beforeState)
            {
                _generationPipeline.AddBefore(node, step);
                return;
            }
            node = node.Next;
        }

        throw new ArgumentException($"No step found with state {beforeState}");
    }

    public void RemoveGenerationStep(GenerationState state)
    {
        if (State != GenerationState.NotStarted && State != GenerationState.Completed)
            throw new InvalidOperationException("Cannot remove steps after generation has started.");

        var node = _generationPipeline.First;
        while (node != null)
        {
            if (node.Value.State == state)
            {
                _generationPipeline.Remove(node);
                return;
            }
            node = node.Next;
        }
    }

    public ChunkGenerationResult Generate()
    {
        try
        {
            State = GenerationState.Initializing;

            // GenerationStartedEvent?.Invoke(this, EventArgs.Empty);
            _stopwatch.Restart();

            var currentNode = _generationPipeline.First;
            while (currentNode != null)
            {
                var step = currentNode.Value;
                currentNode = currentNode.Next;

                State = step.State;
                step.Action();
            }

            return CompleteGeneration();
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    private ChunkGenerationResult CompleteGeneration()
    {
        _stopwatch.Stop();
        State = GenerationState.Completed;
        ReportProgress("Generation completed");
        // GenerationCompletedEvent?.Invoke(this, EventArgs.Empty);
        return new ChunkGenerationResult(_chunkData, _meshData, _mesh, _shape);
    }

    private ChunkGenerationResult HandleError(Exception ex)
    {
        State = GenerationState.Failed;
        // GenerationFailedEvent?.Invoke(this, ex);
        GD.PrintErr($"Chunk generation failed: {ex}");
        return null;
    }


    private void ReportProgress(string message)
    {
        // ProgressUpdatedEvent?.Invoke(this, new GenerationProgressEventArgs
        // {
        //     Message = $"[{_stopwatch.Elapsed.TotalSeconds:F2}s] {message}",
        //     CurrentState = State
        // });
    }
}
