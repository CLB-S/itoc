using System;
using System.Collections.Generic;
using Godot;
using ITOC.Multithreading;

namespace ITOC.Chunks;

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
            ChunkColumnGenerationCallback
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

                // var createCollisionShape = chunkPos.DistanceTo(World.PlayerChunk) <= Core.Instance.Settings.PhysicsDistance;
                // var request = new ChunkGenerationRequest(Generator, chunkPos, result, ChunkGenerationCallback,
                //     createCollisionShape);
                // _chunkFactory.Enqueue(request);

                var chunkTask = new ChunkGenerationTask(World.Generator, chunkPos, result, ChunkGenerationCallback);
                tasks.Add(chunkTask);
                Core.Instance.TaskManager.EnqueueTask(chunkTask);
            }

            var dependentTask = new DependentTask(
                () => PassCompleted?.Invoke(this, new PassEventArgs(Pass, result.Position))
                , dependencies: tasks.ToArray()
            );

            Core.Instance.TaskManager.EnqueueTask(dependentTask);
        }
    }

    public void ChunkGenerationCallback(ChunkData result)
    {
        if (result == null) return;

        // var currentPlayerPos = GetPlayerPosition();
        // var currentCenter = WorldToChunkPosition(currentPlayerPos);
        // if (result.ChunkData.GetPosition().DistanceTo(currentCenter) > Core.Instance.Settings.LoadDistance) return;

        var position = result.GetPosition();
        var positionXZ = new Vector2I(position.X, position.Z);
        var playerPosition = new Vector2I(World.PlayerChunk.X, World.PlayerChunk.Z);
        if (!World.Chunks.ContainsKey(position) && World.ChunkColumns.TryGetValue(positionXZ, out var chunkColumn)
                                          && playerPosition.DistanceTo(positionXZ) <=
                                          Core.Instance.Settings.RenderDistance)
        {
            var chunk = new Chunk(result);
            World.Chunks[position] = chunk;
            chunkColumn.Chunks[position] = chunk;
            // CallDeferred(Node.MethodName.AddChild, chunk);
            World.UpdateNeighborMesherMasks(chunk);
            // chunk.LoadDeferred();
        }
    }

}