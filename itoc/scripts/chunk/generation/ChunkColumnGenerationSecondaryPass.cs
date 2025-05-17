using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using ITOC.Multithreading;

namespace ITOC.ChunkGeneration;

public class ChunkColumnGenerationSecondaryPass : IPass
{
    public int Pass => 1;
    public int Extend => 1;
    public World World { get; private set; }

    public event EventHandler<PassEventArgs> PassCompleted;


    public ChunkColumnGenerationSecondaryPass(World world)
    {
        World = world ?? throw new ArgumentNullException(nameof(world));
    }


    public void ExecuteAt(Vector2I chunkColumnPos)
    {
        var task = new ActionTask(
            () =>
            {
                for (int i = -Extend; i <= Extend; i++)
                {
                    for (int j = -Extend; j <= Extend; j++)
                    {
                        var neighborColumnPos = new Vector2I(chunkColumnPos.X + i, chunkColumnPos.Y + j);
                        var column = World.ChunkColumns[neighborColumnPos];
                        var topChunk = column.Chunks.Values.MaxBy(c => c.Index.Y);
                        topChunk.SetBlock(31 + i * 2, 60, 31 + j * 2, BlockManager.Instance.GetBlock("debug"));
                    }
                }
            },
            "SecondaryPass");

        task.Completed += (sender, args) =>
            PassCompleted?.Invoke(this, new PassEventArgs(Pass, chunkColumnPos)); // TODO: Check this. Potential high perf cost. 

        Core.Instance.TaskManager.EnqueueTask(task);
    }
}