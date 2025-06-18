using Godot;

namespace ITOC.Core;

public class ChunkManager
{
    private ChunkGeneratorBase _chunkGenerator;

    public readonly Dictionary<Vector3I, Chunk> Chunks = new();
    public readonly Dictionary<Vector2I, ChunkColumn> ChunkColumns = new();

    public void LinkChunkGenerator(ChunkGeneratorBase chunkGenerator)
    {
        if (_chunkGenerator != null)
            throw new InvalidOperationException("Chunk generator is already linked.");

        _chunkGenerator = chunkGenerator ?? throw new ArgumentNullException(nameof(chunkGenerator));
        chunkGenerator.OnSurfaceChunksGenerated += OnSurfaceChunksReady;
    }

    private void OnSurfaceChunksReady(object source, Vector2I chunkColumnIndex)
    {
        var chunkColumn = ChunkColumns[chunkColumnIndex];
        chunkColumn.IsSurfaceChunksGenerated = true;

        foreach (var chunk in chunkColumn.Chunks.Values)
            UpdateNeighborMesherMasks(chunk);

        foreach (var chunk in chunkColumn.Chunks.Values)
        {
            chunk.State = ChunkState.Ready;
            chunk.OnBlockUpdated += OnChunkBlockUpdated;
        }
    }

    public bool IsSurfaceChunksGeneratedAt(Vector2I chunkColumnIndex)
    {
        if (ChunkColumns.TryGetValue(chunkColumnIndex, out var chunkColumn))
            return chunkColumn.IsSurfaceChunksGenerated;
        return false;
    }

    #region Update Neighbors' Mesher Masks

    private void OnChunkBlockUpdated(object sender, OnBlockUpdatedEventArgs e)
    {
        var x = e.UpdatePosition.X;
        var y = e.UpdatePosition.Y;
        var z = e.UpdatePosition.Z;
        var block = e.UpdateTargetBlock;
        var sourceChunkIndex = (sender as Chunk).Index;

        if (x == 0)
        {
            var neighbourChunkIndex = sourceChunkIndex;
            neighbourChunkIndex.X -= 1;
            if (Chunks.TryGetValue(neighbourChunkIndex, out var neighbourChunk))
                neighbourChunk.SetMesherMask(Chunk.SIZE_P - 1, y + 1, z + 1, block);
        }

        if (x == Chunk.SIZE - 1)
        {
            var neighbourChunkIndex = sourceChunkIndex;
            neighbourChunkIndex.X += 1;
            if (Chunks.TryGetValue(neighbourChunkIndex, out var neighbourChunk))
                neighbourChunk.SetMesherMask(0, y + 1, z + 1, block);
        }

        if (y == 0)
        {
            var neighbourChunkIndex = sourceChunkIndex;
            neighbourChunkIndex.Y -= 1;
            if (Chunks.TryGetValue(neighbourChunkIndex, out var neighbourChunk))
                neighbourChunk.SetMesherMask(x + 1, Chunk.SIZE_P - 1, z + 1, block);
        }

        if (y == Chunk.SIZE - 1)
        {
            var neighbourChunkIndex = sourceChunkIndex;
            neighbourChunkIndex.Y += 1;
            if (Chunks.TryGetValue(neighbourChunkIndex, out var neighbourChunk))
                neighbourChunk.SetMesherMask(x + 1, 0, z + 1, block);
        }

        if (z == 0)
        {
            var neighbourChunkIndex = sourceChunkIndex;
            neighbourChunkIndex.Z -= 1;
            if (Chunks.TryGetValue(neighbourChunkIndex, out var neighbourChunk))
                neighbourChunk.SetMesherMask(x + 1, y + 1, Chunk.SIZE_P - 1, block);
        }

        if (z == Chunk.SIZE - 1)
        {
            var neighbourChunkIndex = sourceChunkIndex;
            neighbourChunkIndex.Z += 1;
            if (Chunks.TryGetValue(neighbourChunkIndex, out var neighbourChunk))
                neighbourChunk.SetMesherMask(x + 1, y + 1, 0, block);
        }
    }

    private void UpdateNeighborMesherMasks(Chunk chunk)
    {
        var chunkIndex = chunk.Index;

        if (Chunks.TryGetValue(new Vector3I(chunkIndex.X + 1, chunkIndex.Y, chunkIndex.Z), out var positiveXNeighbor))
        {
            for (var y = 0; y < Chunk.SIZE; y++)
                for (var z = 0; z < Chunk.SIZE; z++)
                {
                    var block = chunk.GetBlock(Chunk.SIZE - 1, y, z);
                    positiveXNeighbor.SetMesherMask(0, y + 1, z + 1, block);

                    var neighborBlock = positiveXNeighbor.GetBlock(0, y, z);
                    chunk.SetMesherMask(Chunk.SIZE_P - 1, y + 1, z + 1, neighborBlock);
                }
        }

        if (Chunks.TryGetValue(new Vector3I(chunkIndex.X - 1, chunkIndex.Y, chunkIndex.Z), out var negativeXNeighbor))
        {
            for (var y = 0; y < Chunk.SIZE; y++)
                for (var z = 0; z < Chunk.SIZE; z++)
                {
                    var block = chunk.GetBlock(0, y, z);
                    negativeXNeighbor.SetMesherMask(Chunk.SIZE_P - 1, y + 1, z + 1, block);

                    var neighborBlock = negativeXNeighbor.GetBlock(Chunk.SIZE - 1, y, z);
                    chunk.SetMesherMask(0, y + 1, z + 1, neighborBlock);
                }
        }

        if (Chunks.TryGetValue(new Vector3I(chunkIndex.X, chunkIndex.Y + 1, chunkIndex.Z), out var positiveYNeighbor))
        {
            for (var x = 0; x < Chunk.SIZE; x++)
                for (var z = 0; z < Chunk.SIZE; z++)
                {
                    var block = chunk.GetBlock(x, Chunk.SIZE - 1, z);
                    positiveYNeighbor.SetMesherMask(x + 1, 0, z + 1, block);

                    var neighborBlock = positiveYNeighbor.GetBlock(x, 0, z);
                    chunk.SetMesherMask(x + 1, Chunk.SIZE_P - 1, z + 1, neighborBlock);
                }
        }

        if (Chunks.TryGetValue(new Vector3I(chunkIndex.X, chunkIndex.Y - 1, chunkIndex.Z), out var negativeYNeighbor))
        {
            for (var x = 0; x < Chunk.SIZE; x++)
                for (var z = 0; z < Chunk.SIZE; z++)
                {
                    var block = chunk.GetBlock(x, 0, z);
                    negativeYNeighbor.SetMesherMask(x + 1, Chunk.SIZE_P - 1, z + 1, block);

                    var neighborBlock = negativeYNeighbor.GetBlock(x, Chunk.SIZE - 1, z);
                    chunk.SetMesherMask(x + 1, 0, z + 1, neighborBlock);
                }
        }

        if (Chunks.TryGetValue(new Vector3I(chunkIndex.X, chunkIndex.Y, chunkIndex.Z + 1), out var positiveZNeighbor))
        {
            for (var x = 0; x < Chunk.SIZE; x++)
                for (var y = 0; y < Chunk.SIZE; y++)
                {
                    var block = chunk.GetBlock(x, y, Chunk.SIZE - 1);
                    positiveZNeighbor.SetMesherMask(x + 1, y + 1, 0, block);

                    var neighborBlock = positiveZNeighbor.GetBlock(x, y, 0);
                    chunk.SetMesherMask(x + 1, y + 1, Chunk.SIZE_P - 1, neighborBlock);
                }
        }

        if (Chunks.TryGetValue(new Vector3I(chunkIndex.X, chunkIndex.Y, chunkIndex.Z - 1), out var negativeZNeighbor))
        {
            for (var x = 0; x < Chunk.SIZE; x++)
                for (var y = 0; y < Chunk.SIZE; y++)
                {
                    var block = chunk.GetBlock(x, y, 0);
                    negativeZNeighbor.SetMesherMask(x + 1, y + 1, Chunk.SIZE_P - 1, block);

                    var neighborBlock = negativeZNeighbor.GetBlock(x, y, Chunk.SIZE - 1);
                    chunk.SetMesherMask(x + 1, y + 1, 0, neighborBlock);
                }
        }
    }

    #endregion

}