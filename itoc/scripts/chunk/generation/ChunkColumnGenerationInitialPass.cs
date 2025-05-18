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
            "ChunkColumnGenerationTask-" + chunkColumnPos,
            TaskPriority.Low
        );

        // var columnTask = new ChunkColumnGenerationTask(Generator, pos, ChunkColumnGenerationCallback);
        Core.Instance.TaskManager.EnqueueTask(columnTask);
    }

    private void ChunkColumnGenerationCallback(ChunkColumn result)
    {
        if (result == null) return;

        if (!World.ChunkColumns.ContainsKey(result.Index))
        {
            World.ChunkColumns[result.Index] = result;

            var high = Mathf.FloorToInt(result.HeightMapHigh / ChunkMesher.CS);
            var low = Mathf.FloorToInt(result.HeightMapLow / ChunkMesher.CS);

            List<GameTask> tasks = new();
            for (var y = low; y <= high; y++)
            {
                var chunkIndex = new Vector3I(result.Index.X, y, result.Index.Y);
                if (World.Chunks.ContainsKey(chunkIndex)) continue;

                var chunkTask = new ChunkGenerationTask(World.Generator, chunkIndex, result,
                    ChunkGenerationCallback, "ChunkGenerationTask-" + chunkIndex);
                tasks.Add(chunkTask);
                Core.Instance.TaskManager.EnqueueTask(chunkTask);
            }

            var dependentTask = new DependentTask(
                () => PassCompleted?.Invoke(this, new PassEventArgs(Pass, result.Index)),
                "ChunkColumnGenerationInitialPass-" + result.Index,
                TaskPriority.High,
                dependencies: tasks.ToArray()
            );

            Core.Instance.TaskManager.EnqueueTask(dependentTask);
        }
    }

    private void ChunkGenerationCallback(Chunk result)
    {
        if (result == null) return;

        var index = result.Index;
        var indexXZ = new Vector2I(index.X, index.Z);
        // var playerPosition = new Vector2I(World.PlayerChunk.X, World.PlayerChunk.Z);
        if (!World.Chunks.ContainsKey(index) && World.ChunkColumns.TryGetValue(indexXZ, out var chunkColumn))
        {
            World.Chunks[index] = result;
            chunkColumn.Chunks[index] = result;
            // CallDeferred(Node.MethodName.AddChild, chunk);
            // World.UpdateNeighborMesherMasks(chunk);
            // chunk.LoadDeferred();
        }
    }

}