namespace ChunkGenerator;

public class ChunkGenerationResult
{
    public ChunkGenerationResult(ChunkData chunkData, ChunkColumn chunkColumn)
    {
        ChunkData = chunkData;
        // Mesh = mesh;
        // CollisionShape = collisionShape;
        ChunkColumn = chunkColumn;
    }

    // public Mesh Mesh { get; }
    // public Shape3D CollisionShape { get; }
    public ChunkData ChunkData { get; }
    public ChunkColumn ChunkColumn { get; }
}