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
    CollisionShape,
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
    private RandomNumberGenerator _rng;
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
        _generationPipeline.AddLast(new GenerationStep(ChunkGenerationState.HeightMap, SetBlocksByHeightMap));
        _generationPipeline.AddLast(new GenerationStep(ChunkGenerationState.Meshing, Meshing));
        _generationPipeline.AddLast(new GenerationStep(ChunkGenerationState.CollisionShape, CreateCollisionShape));
    }

    private void Initialize(ChunkGenerationRequest request)
    {
        _chunkData = new ChunkData(request.ChunkPosition);
        _rng = new RandomNumberGenerator();
        _rng.Seed = request.WorldGenerator.Settings.Seed + (uint)(request.ChunkPosition.X + request.ChunkPosition.Y + request.ChunkPosition.Z);
    }

    private void SetBlocksByHeightMap(ChunkGenerationRequest request)
    {
        for (var x = 0; x < ChunkMesher.CS_P; x++)
            for (var z = 0; z < ChunkMesher.CS_P; z++)
            {
                var height = Mathf.FloorToInt(request.ChunkColumn.HeightMap[x, z]);

                // Calculate slope steepness
                float maxSlope = CalculateSlope(request, x, z);

                int baseDirtDepth = Mathf.Clamp(4 - Mathf.FloorToInt(maxSlope), 1, 4);
                for (var y = 0; y < ChunkMesher.CS_P; y++)
                {
                    var actualY = request.ChunkPosition.Y * ChunkMesher.CS + y;
                    if (actualY <= height)
                    {
                        string blockType = DetermineBlockType(actualY, height, maxSlope, baseDirtDepth);
                        _chunkData.SetBlock(x, y, z, blockType);
                    }
                    else if (actualY <= 0)
                    {
                        _chunkData.SetBlock(x, y, z, "water");
                    }
                }
            }
    }

    private static float CalculateSlope(ChunkGenerationRequest request, int x, int z)
    {
        float maxSlope = 0;

        int[][] neighborOffsets =
        [
            [-1,1],
            [0,1],
            [1,0],
            [1,1]
        ];

        foreach (var offset in neighborOffsets)
        {
            int dx = offset[0];
            int dz = offset[1];

            int neighborAX = x + dx;
            int neighborAZ = z + dz;
            if (neighborAX < 0 || neighborAX >= ChunkMesher.CS_P || neighborAZ < 0 || neighborAZ >= ChunkMesher.CS_P)
                continue;

            int neighborBX = x - dx;
            int neighborBZ = z - dz;
            if (neighborBX < 0 || neighborBX >= ChunkMesher.CS_P || neighborBZ < 0 || neighborBZ >= ChunkMesher.CS_P)
                continue;

            var neighborHeightA = request.ChunkColumn.HeightMap[neighborAX, neighborAZ];
            var neighborHeightB = request.ChunkColumn.HeightMap[neighborBX, neighborBZ];

            var slope = Mathf.Abs(neighborHeightA - neighborHeightB) / 2.0f;
            if (slope > maxSlope)
                maxSlope = slope;
        }


        return maxSlope;
    }

    private string DetermineBlockType(int actualY, int height, float maxSlope, int dirtDepth)
    {
        // Elevation-based blocks
        if (actualY <= 3)
            return "sand"; // maxSlope <= 1 ? "sand" : "gravel";

        // Depth-based layers
        if (actualY > height - dirtDepth)
        {
            // Surface layers
            if (actualY == height)
            {
                if (maxSlope > 1.5) return "stone";

                if (_rng.Randf() > 1 - (actualY - 250) / 50.0f)
                    return maxSlope <= 2 ? "snow" : "stone";

                if (_rng.Randf() < (actualY - 170) / 50.0f)
                    return maxSlope <= 1 ? "grass_block" : "stone";

                return "grass_block";
            }
            return maxSlope > 2.5 ? "stone" : "dirt";
        }

        return "stone";
    }

    private void Meshing(ChunkGenerationRequest request)
    {
        var meshData = new ChunkMesher.MeshData(_chunkData.OpaqueMask, _chunkData.TransparentMasks);
        ChunkMesher.MeshChunk(_chunkData, meshData);
        _mesh = ChunkMesher.GenerateMesh(meshData);
    }

    private void CreateCollisionShape(ChunkGenerationRequest request)
    {
        if (request.CreateCollisionShape)
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