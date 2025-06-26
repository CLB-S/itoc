using Godot;
using ITOC.Core.Multithreading;

namespace ITOC.Core.WorldGeneration.Vanilla;

public class VanillaChunkColumnGenerationPass1(ChunkManager chunkManager) : IPass
{
    public int Pass => 1;
    public int Expansion => 0;

    public event EventHandler<PassEventArgs> PassCompleted;

    public GameTask CreateTaskAt(Vector2I chunkColumnPos)
    {
        var task = new ActionTask(
            () =>
            {
                for (var i = -Expansion; i <= Expansion; i++)
                {
                    for (var j = -Expansion; j <= Expansion; j++)
                    {
                        var neighborColumnPos = new Vector2I(
                            chunkColumnPos.X + i,
                            chunkColumnPos.Y + j
                        );
                        var column = chunkManager.ChunkColumns[neighborColumnPos];
                        var topChunk = column.Chunks.Values.MaxBy(c => c.Index.Y);
                        topChunk.SetBlock(
                            31 + i * 2,
                            60,
                            31 + j * 2,
                            BlockManager.Instance.GetBlock("itoc:debug")
                        );
                    }
                }
            },
            "SecondaryPass"
        );

        task.Completed += (sender, args) =>
            PassCompleted?.Invoke(this, new PassEventArgs(Pass, chunkColumnPos)); // TODO: Check this. Potential high perf cost.

        return task;
    }
}
