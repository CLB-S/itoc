using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using DelaunatorSharp;

namespace WorldGenerator;

public class CellData
{
    public int TriangleIndex;
    public VoronoiCell Cell;
    public Vector2 TectonicMovement;
    public PlateType PlateType;
    public uint PlateSeed;
    public float Altitude = 0;
    public bool RoundPlateJunction = false;
    public float Precipitation;
    public float Flux;
    public RiverSegment River;
}

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

public enum GenerationState
{
    NotStarted,
    Initializing,
    GeneratingPoints,
    CreatingVoronoi,
    InitializingTectonics,
    CalculatingAltitudes,
    PropagatingAltitudes,
    CalculatingWaterFlux,
    InitInterpolator,
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

    private readonly LinkedList<GenerationStep> _generationPipeline = new();
    private readonly Stopwatch _stopwatch = new();
    private readonly object _stateLock = new();
    private IdwInterpolator _interpolator;

    // World data properties
    private Noise _plateNoise;
    private Noise _heightNoise;

    // Configuration
    public WorldSettings Settings { get; private set; }

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

    private void InitializePipeline()
    {
        _generationPipeline.AddLast(new GenerationStep(GenerationState.Initializing, InitializeResources));
        _generationPipeline.AddLast(new GenerationStep(GenerationState.GeneratingPoints, GeneratePoints));
        _generationPipeline.AddLast(new GenerationStep(GenerationState.CreatingVoronoi, CreateVoronoiDiagram));
        _generationPipeline.AddLast(new GenerationStep(GenerationState.InitializingTectonics, InitializeTectonicProperties));
        _generationPipeline.AddLast(new GenerationStep(GenerationState.CalculatingAltitudes, CalculateAltitudes));
        _generationPipeline.AddLast(new GenerationStep(GenerationState.PropagatingAltitudes, PropagateAltitudes));
        _generationPipeline.AddLast(new GenerationStep(GenerationState.CalculatingWaterFlux, CalculateWaterFlux));
        _generationPipeline.AddLast(new GenerationStep(GenerationState.InitInterpolator, InitInterpolator));
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
                    ResetGenerator();

                State = GenerationState.Initializing;
            }

            GenerationStartedEvent?.Invoke(this, EventArgs.Empty);
            _stopwatch.Restart();

            var currentNode = _generationPipeline.First;
            while (currentNode != null)
            {
                var step = currentNode.Value;
                currentNode = currentNode.Next;

                UpdateState(step.State);
                await Task.Run(step.Action);
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

    // private void ResolveDepressions()
    // {
    //     var landCells = _cellDatas.Values.Where(c => c.PlateType != PlateType.Oceans).ToList();
    //     bool hasDepressions;

    //     do
    //     {
    //         hasDepressions = false;
    //         foreach (var cell in landCells)
    //         {
    //             var lowestNeighbor = GetNeighborCells(cell)
    //                 .OrderBy(n => _cellDatas[n].Altitude).First();

    //             if (cell.Altitude <= _cellDatas[lowestNeighbor].Altitude)
    //             {
    //                 cell.Altitude = _cellDatas[lowestNeighbor].Altitude * 1.05;
    //                 hasDepressions = true;
    //             }
    //         }
    //     } while (hasDepressions);
    // }

    private void InitInterpolator()
    {
        var posList = new List<Vector2>(_cellDatas.Count);
        var dataList = new List<float>(_cellDatas.Count);
        for (var i = 0; i < _cellDatas.Count; i++)
        {
            posList.Add(_points[i]);
            dataList.Add(_cellDatas[i].Altitude);
        }

        _interpolator = new IdwInterpolator(posList, dataList); // TODO: Settings
    }

    public float[,] CalculateFullHeightMap(int resolutionX, int resolutionY)
    {
        return CalculateHeightMap(resolutionX, resolutionY, Settings.Bounds, true);
    }

    public float[,] CalculateHeightMap(int resolutionX, int resolutionY, Rect2 bounds, bool parallel = false, int upscaleLevel = 2)
    {
        if (State != GenerationState.Completed)
            throw new InvalidOperationException("World generation is not completed yet.");

        return _interpolator.ConstructHeightMap(resolutionX, resolutionY, bounds, parallel, upscaleLevel);
    }

    public ImageTexture GetFullHeightMapImageTexture(int resolutionX, int resolutionY)
    {
        var heightMap = CalculateFullHeightMap(resolutionX, resolutionY);
        var image = Image.CreateEmpty(resolutionX, resolutionY, false, Image.Format.Rgb8);
        for (var x = 0; x < resolutionX; x++)
            for (var y = 0; y < resolutionY; y++)
            {
                var h = (float)(0.5 * (1 + heightMap[x, y] / Settings.MaxAltitude));
                image.SetPixel(x, y, new Color(h, h, h));
            }

        return ImageTexture.CreateFromImage(image);
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

    private void ResetGenerator()
    {

    }
}


