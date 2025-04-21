using Godot;

namespace ChunkGenerator;

public class ChunkGenerationResult
{
    public ChunkGenerationResult(ChunkData chunkData, Mesh mesh, Shape3D collisionShape, ChunkColumn chunkColumn)
    {
        ChunkData = chunkData;
        Mesh = mesh;
        CollisionShape = collisionShape;
        ChunkColumn = chunkColumn;
    }

    public Mesh Mesh { get; }
    public Shape3D CollisionShape { get; }
    public ChunkData ChunkData { get; }
    public ChunkColumn ChunkColumn { get; }
}