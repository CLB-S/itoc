using Godot;

namespace ITOC.Core;

public enum ChunkMeshState
{
    Created,
    Ready,
    Rendered,
}

public class ChunkMesh
{
    public ChunkMeshState State { get; set; } = ChunkMeshState.Created;
    public Chunk Chunk { get; set; }

    public MeshInstance3D MeshInstance { get; set; }
    public MeshInstance3D DebugMeshInstance { get; set; }
    public StaticBody3D CollisionBody { get; set; }
    public CollisionShape3D CollisionShape { get; set; }

    public int Lod => Chunk.Lod;
    public Vector3I Index => Chunk.Index;
    public Vector3 Position => Chunk.Position;
    public Vector3 CenterPosition => Chunk.CenterPosition;
    public Vector3 Size => Chunk.Size;

    public ChunkMesh(Chunk chunk)
    {
        Chunk = chunk;

        if (Chunk != null)
            State = ChunkMeshState.Ready;
    }

    public void UpdateMesh(Material materialOverride = null)
    {
        if (Chunk == null || State != ChunkMeshState.Rendered)
            return;

        MeshInstance?.SetDeferred(
            MeshInstance3D.PropertyName.Mesh,
            Chunk.GetMesh(materialOverride)
        );
        CollisionShape?.SetDeferred(CollisionShape3D.PropertyName.Shape, Chunk.GetCollisionShape());
    }
}
