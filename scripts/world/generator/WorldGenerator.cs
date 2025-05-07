using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using PatternSystem;

namespace WorldGenerator;

public class GenerationStep
{
    public GenerationState State { get; }
    public Action Action { get; }
    public bool Optional { get; }
    public Func<bool> ShouldRepeat { get; }
    public GenerationState? RepeatToState { get; }

    public GenerationStep(GenerationState state, Action action, bool optional = false)
    {
        State = state;
        Action = action;
        Optional = optional;
        ShouldRepeat = () => false;
    }

    public GenerationStep(GenerationState state, Action action, Func<bool> shouldRepeat, GenerationState repeatToState,
        bool optional = false)
    {
        State = state;
        Action = action;
        Optional = optional;
        ShouldRepeat = shouldRepeat;
        RepeatToState = repeatToState;
    }
}

public enum GenerationState
{
    NotStarted,
    Initializing,
    GeneratingSamplePoints,
    InitializingCellDatas,
    InitializingTectonics,
    CalculatingInitialUplifts,
    PropagatingUplifts,
    FindingRiverMouths,
    PreparingStreamGraph,
    SolvingPowerEquation, // If not converged, goto `PreparingStreamGraph`.
    // CalculatingNormals,
    AdjustingTemperature,
    SettingBiome,
    // InitInterpolator,
    Custom,
    Completed,
    Failed
}

public partial class WorldGenerator
{
    public class GenerationProgressEventArgs : EventArgs
    {
        public string Message { get; set; }
        public GenerationState CurrentState { get; set; }
    }

    public event EventHandler<GenerationProgressEventArgs> ProgressUpdatedEvent;
    public event EventHandler GenerationStartedEvent;
    public event EventHandler GenerationCompletedEvent;
    public event EventHandler<Exception> GenerationFailedEvent;

    public GenerationState State { get; private set; } = GenerationState.NotStarted;

    protected readonly LinkedList<GenerationStep> _generationPipeline = new();
    private readonly Stopwatch _stopwatch = new();
    private readonly object _stateLock = new();
    private IdwInterpolator _heightMapInterpolator;

    // World data properties
    private PatternTree _platePattern;
    private PatternTree _upliftPattern;
    private PatternTree _temperaturePattern;
    private PatternTree _precipitationPattern;
    private PatternTree _domainWarpPattern;
    private PatternTree _heightPattern;

    // Configuration
    public WorldSettings Settings { get; }

    public WorldGenerator()
    {
        InitializePipeline();
        Settings = new WorldSettings();
    }

    public WorldGenerator(WorldSettings settings)
    {
        InitializePipeline();
        Settings = settings;
    }

    protected virtual void InitializePipeline()
    {
        _generationPipeline.AddLast(new GenerationStep(GenerationState.Initializing, InitializeResources));
        _generationPipeline.AddLast(new GenerationStep(GenerationState.GeneratingSamplePoints, GenerateSamplePoints));
        _generationPipeline.AddLast(new GenerationStep(GenerationState.InitializingCellDatas, InitializeCellDatas));
        _generationPipeline.AddLast(new GenerationStep(GenerationState.InitializingTectonics,
            InitializeTectonicProperties));
        _generationPipeline.AddLast(new GenerationStep(GenerationState.CalculatingInitialUplifts,
            CalculateInitialUplifts));
        _generationPipeline.AddLast(new GenerationStep(GenerationState.PropagatingUplifts, PropagateUplifts));

        // Add the stream generation cycle
        _generationPipeline.AddLast(new GenerationStep(GenerationState.FindingRiverMouths, FindRiverMouths));
        _generationPipeline.AddLast(new GenerationStep(GenerationState.PreparingStreamGraph, PrepareStreamGraph));
        _generationPipeline.AddLast(
            new GenerationStep(GenerationState.SolvingPowerEquation, SolvePowerEquation,
                () => !_powerEquationConverged, GenerationState.PreparingStreamGraph));

        // _generationPipeline.AddLast(new GenerationStep(GenerationState.CalculatingNormals, CalculateNormals));
        _generationPipeline.AddLast(new GenerationStep(GenerationState.AdjustingTemperature, AdjustTemperatureAccordingToHeight));
        _generationPipeline.AddLast(new GenerationStep(GenerationState.SettingBiome, SetBiomes));
        // _generationPipeline.AddLast(new GenerationStep(GenerationState.InitInterpolator, InitInterpolator));
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

    public async Task GenerateWorldAsync()
    {
        try
        {
            lock (_stateLock)
            {
                if (State == GenerationState.Completed)
                    ReportProgress("Warning: World generation has already been completed. Regenerating...");

                State = GenerationState.Initializing;
                _powerEquationConverged = false;
                _iterationCount = 0;
            }

            GenerationStartedEvent?.Invoke(this, EventArgs.Empty);
            _stopwatch.Restart();

            var currentNode = _generationPipeline.First;
            while (currentNode != null)
            {
                var step = currentNode.Value;

                UpdateState(step.State);
                await Task.Run(step.Action);

                // Check if we need to repeat certain steps
                if (step.ShouldRepeat() && step.RepeatToState.HasValue)
                {
                    // Find the node we should go back to
                    var repeatNode = _generationPipeline.First;
                    while (repeatNode != null && repeatNode.Value.State != step.RepeatToState.Value)
                        repeatNode = repeatNode.Next;

                    if (repeatNode != null)
                    {
                        // ReportProgress($"Repeating from {step.RepeatToState.Value} state");
                        currentNode = repeatNode;
                    }
                }
                else
                {
                    currentNode = currentNode.Next;
                }
            }

            CompleteGeneration();
        }
        catch (Exception ex)
        {
            HandleError(ex);
        }
    }

    private void UpdateState(GenerationState newState)
    {
        lock (_stateLock)
        {
            State = newState;
        }
    }

    private void ReportProgress(string message)
    {
        ProgressUpdatedEvent?.Invoke(this, new GenerationProgressEventArgs
        {
            Message = $"[{_stopwatch.Elapsed.TotalSeconds:F2}s] {message}",
            CurrentState = State
        });
    }

    private void CompleteGeneration()
    {
        _stopwatch.Stop();
        UpdateState(GenerationState.Completed);
        ReportProgress("Generation completed");
        GenerationCompletedEvent?.Invoke(this, EventArgs.Empty);
    }

    private void HandleError(Exception ex)
    {
        UpdateState(GenerationState.Failed);
        GenerationFailedEvent?.Invoke(this, ex);
    }
}