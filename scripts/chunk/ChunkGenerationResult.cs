using Godot;

namespace ChunkGenerator;

public class ChunkGenerationResult
{
    public ChunkGenerationResult(ChunkData chunkData, ChunkMesher.MeshData meshData, Mesh mesh, Shape3D collisionShape)
    {
        ChunkData = chunkData;
        MeshData = meshData;
        Mesh = mesh;
        CollisionShape = collisionShape;
    }

    public Mesh Mesh { get; }
    public ChunkMesher.MeshData MeshData { get; }
    public Shape3D CollisionShape { get; }
    public ChunkData ChunkData { get; }
}