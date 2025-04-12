using Godot;

namespace ChunkGenerator;

public class ChunkGenerationResult
{
    public ChunkGenerationResult(ChunkData chunkData, Mesh mesh, Shape3D collisionShape)
    {
        ChunkData = chunkData;
        Mesh = mesh;
        CollisionShape = collisionShape;
    }

    public Mesh Mesh { get; }
    public Shape3D CollisionShape { get; }
    public ChunkData ChunkData { get; }
}