using Godot;
using ITOC.Core.Multithreading;

namespace ITOC.Core.WorldGeneration.Vanilla;

public class VanillaChunkColumnGenerationPass0 : IPass
{
    private readonly VanillaChunkGenerator _generator;
    private readonly ChunkManager _chunkManager;

    public int Pass => 0;

    public int Expansion => 0;

    public event EventHandler<PassEventArgs> PassCompleted;

    public VanillaChunkColumnGenerationPass0(VanillaChunkGenerator generator, ChunkManager chunkManager)
    {
        _generator = generator ?? throw new ArgumentNullException(nameof(generator));
        _chunkManager = chunkManager ?? throw new ArgumentNullException(nameof(chunkManager));
    }

    public GameTask CreateTaskAt(Vector2I chunkColumnPos)
    {
        return new FunctionTask<ChunkColumn>(
            () => _generator.GenerateChunkColumnMetadata(chunkColumnPos),
            ChunkColumnGenerationCallback,
            "ChunkColumnGenerationTask-" + chunkColumnPos,
            TaskPriority.Low
        );
    }

    private void ChunkColumnGenerationCallback(ChunkColumn result)
    {
        if (result == null) return;

        if (!_chunkManager.ChunkColumns.ContainsKey(result.Index))
        {
            _chunkManager.ChunkColumns[result.Index] = result;

            var high = Mathf.FloorToInt(result.HeightMapHigh / Chunk.SIZE);
            var low = Mathf.FloorToInt(result.HeightMapLow / Chunk.SIZE);

            List<GameTask> tasks = new();
            for (var y = low; y <= high; y++)
            {
                var chunkIndex = new Vector3I(result.Index.X, y, result.Index.Y);
                if (_chunkManager.Chunks.ContainsKey(chunkIndex)) continue;

                var chunkTask = new VanillaChunkGenerationTask(chunkIndex, result,
                    ChunkGenerationCallback, "ChunkGenerationTask-" + chunkIndex);
                TaskManager.Instance.EnqueueTask(chunkTask);
            }

            var dependentTask = new DependentTask(
                () => PassCompleted?.Invoke(this, new PassEventArgs(Pass, result.Index)),
                "ChunkColumnGenerationInitialPass-" + result.Index,
                TaskPriority.High,
                dependencies: tasks.ToArray()
            );

            TaskManager.Instance.EnqueueTask(dependentTask);
        }
    }

    private void ChunkGenerationCallback(Chunk result)
    {
        if (result == null) return;

        var index = result.Index;
        var indexXZ = new Vector2I(index.X, index.Z);
        // var playerPosition = new Vector2I(World.PlayerChunk.X, World.PlayerChunk.Z);
        if (!_chunkManager.Chunks.ContainsKey(index) && _chunkManager.ChunkColumns.TryGetValue(indexXZ, out var chunkColumn))
        {
            _chunkManager.Chunks[index] = result;
            chunkColumn.Chunks[index] = result;
            // CallDeferred(Node.MethodName.AddChild, chunk);
            // World.UpdateNeighborMesherMasks(chunk);
            // chunk.LoadDeferred();
        }
    }

}