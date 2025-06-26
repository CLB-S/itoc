using Godot;

namespace ITOC.Core;

public class ChunkLoadingSource
{
    public bool IsActive { get; private set; } = true;

    public ChunkGeneratorBase ChunkGenerator { get; private set; }
    public ChunkRange ChunkRange { get; private set; }

    private Queue<(Vector2I, int)> _surfaceChunksQueue;

    public ChunkLoadingSource(ChunkRange chunkRange, ChunkGeneratorBase chunkGenerator)
    {
        ChunkRange = chunkRange;
        ChunkGenerator = chunkGenerator;
    }

    private void EnqueueSurfaceChunksGeneration()
    {
        if (!IsActive)
            return;

        if (_surfaceChunksQueue == null || _surfaceChunksQueue.Count == 0)
            return;

        var (chunkColumnIndex, distance) = _surfaceChunksQueue.Dequeue();
        ChunkGenerator?.EnqueueSurfaceChunksGeneration(chunkColumnIndex,
            _ => EnqueueSurfaceChunksGeneration());
    }

    public void UpdateFrom(Vector3 sourcePosition)
    {
        if ((!IsActive) || (ChunkGenerator == null))
            return;

        ChunkRange.Center = sourcePosition;

        var chunkColumnRange = ChunkRange.ChunkColumnsSorted();
        _surfaceChunksQueue = new Queue<(Vector2I, int)>(chunkColumnRange);

        for (int i = 0; i < ChunkGenerator.MaxConcurrentChunkGenerationTasks + 2; i++)
            EnqueueSurfaceChunksGeneration();
    }
}