using System;
using Godot;

namespace ChunkGenerator;


public class ChunkGenerationRequest
{
    public ChunkGenerationState State { get; private set; } = ChunkGenerationState.NotStarted;
    public Vector3I ChunkPosition { get; }
    public ChunkColumn ChunkColumn { get; }
    public Action<ChunkGenerationResult> Callback { get; }
    public readonly WorldGenerator.WorldGenerator WorldGenerator;
    public bool CreateCollisionShape { get; set; }


    public ChunkGenerationRequest(
        WorldGenerator.WorldGenerator worldGenerator,
        Vector3I position,
        ChunkColumn chunkColumn,
        Action<ChunkGenerationResult> callback,
        bool createCollisionShape)
    {
        WorldGenerator = worldGenerator;
        ChunkPosition = position;
        ChunkColumn = chunkColumn;
        Callback = callback;
        CreateCollisionShape = createCollisionShape;
    }

    public ChunkGenerationResult Execute()
    {
        return new ChunkGenerationPipeline(this).Execute();
    }
}
