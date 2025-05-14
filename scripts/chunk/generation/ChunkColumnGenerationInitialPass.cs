using System;
using System.Collections.Generic;
using Godot;
using ITOC.Multithreading;

namespace ITOC.ChunkGeneration;

public class ChunkColumnGenerationInitialPass : IPass
{
    public int Pass => 0;

    public int Extend => 0;

    public event EventHandler<PassEventArgs> PassCompleted;

    public World World { get; private set; }

    public ChunkColumnGenerationInitialPass(World world)
    {
        World = world ?? throw new ArgumentNullException(nameof(world));
    }

    public void ExecuteAt(Vector2I chunkColumnPos)
    {
        var columnTask = new FunctionTask<ChunkColumn>(
            () => World.Generator.GenerateChunkColumn(chunkColumnPos),
            ChunkColumnGenerationCallback,
            "ChunkColumnGenerationTask-" + chunkColumnPos
        );

        // var columnTask = new ChunkColumnGenerationTask(Generator, pos, ChunkColumnGenerationCallback);
        Core.Instance.TaskManager.EnqueueTask(columnTask);
    }

    private void ChunkColumnGenerationCallback(ChunkColumn result)
    {
        if (result == null) return;

        if (!World.ChunkColumns.ContainsKey(result.Position))
        {
            World.ChunkColumns[result.Position] = result;

            var high = Mathf.FloorToInt(result.HeightMapHigh / ChunkMesher.CS);
            var low = Mathf.FloorToInt((result.HeightMapLow - 2) / ChunkMesher.CS) - 1;

            List<GameTask> tasks = new();
            for (var y = low; y <= high; y++)
            {
                var chunkPos = new Vector3I(result.Position.X, y, result.Position.Y);
                if (World.Chunks.ContainsKey(chunkPos)) continue;

                var chunkTask = new ChunkGenerationTask(World.Generator, chunkPos, result,
                    ChunkGenerationCallback, "ChunkGenerationTask-" + chunkPos);
                tasks.Add(chunkTask);
                Core.Instance.TaskManager.EnqueueTask(chunkTask);
            }

            var dependentTask = new DependentTask(
                () => PassCompleted?.Invoke(this, new PassEventArgs(Pass, result.Position))
                , "ChunkColumnGenerationInitialPass-" + result.Position
                , dependencies: tasks.ToArray()
            );

            Core.Instance.TaskManager.EnqueueTask(dependentTask);
        }
    }

    private void ChunkGenerationCallback(Chunk result)
    {
        if (result == null) return;

        var position = result.Position;
        var positionXZ = new Vector2I(position.X, position.Z);
        // var playerPosition = new Vector2I(World.PlayerChunk.X, World.PlayerChunk.Z);
        if (!World.Chunks.ContainsKey(position) && World.ChunkColumns.TryGetValue(positionXZ, out var chunkColumn))
        {
            World.Chunks[position] = result;
            chunkColumn.Chunks[position] = result;
            // CallDeferred(Node.MethodName.AddChild, chunk);
            // World.UpdateNeighborMesherMasks(chunk);
            // chunk.LoadDeferred();
        }
    }

}