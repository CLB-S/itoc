using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;

namespace ChunkGenerator;

public enum ChunkGenerationState
{
    NotStarted,
    Initializing,
    HeightMap,
    Meshing,
    Custom,
    Completed,
    Failed
}

public class GenerationStep
{
    public ChunkGenerationState State { get; }
    public Action<ChunkGenerationRequest> Action { get; }
    public bool Optional { get; }

    public GenerationStep(ChunkGenerationState state, Action<ChunkGenerationRequest> action, bool optional = false)
    {
        State = state;
        Action = action;
        Optional = optional;
    }
}

public class ChunkGenerationPipeline
{
    public ChunkGenerationState State { get; private set; } = ChunkGenerationState.NotStarted;

    private ChunkData _chunkData;
    private Mesh _mesh;
    private Shape3D _shape;
    private readonly Stopwatch _stopwatch = new();
    private readonly LinkedList<GenerationStep> _generationPipeline = new();

    public ChunkGenerationPipeline()
    {
        InitializePipeline();
    }

    private void InitializePipeline()
    {
        _generationPipeline.AddLast(new GenerationStep(ChunkGenerationState.Initializing, Initialize));
        // _generationPipeline.AddLast(new GenerationStep(GenerationState.Custom, TerrainTest));
        _generationPipeline.AddLast(new GenerationStep(ChunkGenerationState.HeightMap, SetBlocksHeightMap));
        _generationPipeline.AddLast(new GenerationStep(ChunkGenerationState.Meshing, Meshing));
    }

    private void Initialize(ChunkGenerationRequest request)
    {
        _chunkData = new ChunkData(request.ChunkPosition);
    }

    // TODO: Optimize this
    private void SetBlocksHeightMap(ChunkGenerationRequest request)
    {
        for (var x = 0; x < ChunkMesher.CS_P; x++)
            for (var z = 0; z < ChunkMesher.CS_P; z++)
            {
                var height = Mathf.FloorToInt(request.ChunkColumn.HeightMap[x, z]);
                for (var y = 0; y < ChunkMesher.CS_P; y++)
                {
                    var actualY = request.ChunkPosition.Y * ChunkMesher.CS + y;
                    if (actualY < height - ChunkMesher.CS)
                    {
                        if (actualY == height - ChunkMesher.CS - 1)
                            _chunkData.SetBlock(x, y, z, "grass_block"); // GD.Randi() % 4 + 1;
                        else if (actualY > height - ChunkMesher.CS - 4)
                            _chunkData.SetBlock(x, y, z, "dirt");
                        else
                            _chunkData.SetBlock(x, y, z, "stone");
                        ChunkMesher.AddOpaqueVoxel(_chunkData.OpaqueMask, x, y, z);
                    }
                }
            }
    }

    private void Meshing(ChunkGenerationRequest request)
    {
        using var meshData = new ChunkMesher.MeshData(_chunkData.OpaqueMask);
        ChunkMesher.MeshChunk(_chunkData, meshData);
        _mesh = ChunkMesher.GenerateMesh(meshData);
        _shape = _mesh?.CreateTrimeshShape();
    }

    public ChunkGenerationResult Excute(ChunkGenerationRequest request)
    {
        try
        {
            State = ChunkGenerationState.Initializing;

            // GenerationStartedEvent?.Invoke(this, EventArgs.Empty);
            _stopwatch.Restart();

            var currentNode = _generationPipeline.First;
            while (currentNode != null)
            {
                var step = currentNode.Value;
                currentNode = currentNode.Next;

                State = step.State;
                step.Action(request);
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
        State = ChunkGenerationState.Completed;
        ReportProgress("Generation completed");
        // GenerationCompletedEvent?.Invoke(this, EventArgs.Empty);
        return new ChunkGenerationResult(_chunkData, _mesh, _shape);
    }

    private ChunkGenerationResult HandleError(Exception ex)
    {
        State = ChunkGenerationState.Failed;
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