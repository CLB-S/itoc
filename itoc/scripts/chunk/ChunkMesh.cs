using Godot;

namespace ITOC;

public enum ChunkMeshState
{
    Created,
    Ready,
    Rendered,
}

public class ChunkMesh
{
    public ChunkMeshState State { get; set; } = ChunkMeshState.Created;
    private Chunk _chunk;
    public Chunk Chunk
    {
        get { return _chunk; }
        set
        {
            if (value == _chunk)
                return;

            _chunk = value;
            UpdateMesh();
        }
    }

    public MeshInstance3D MeshInstance { get; set; }
    public MeshInstance3D DebugMeshInstance { get; set; }
    public StaticBody3D CollisionBody { get; set; }
    public CollisionShape3D CollisionShape { get; set; }

    public int Lod => Chunk?.Lod ?? 0;
    public Vector3I Index => Chunk?.Index ?? Vector3I.Zero;
    public Vector3 Position => Chunk?.Position ?? Vector3.Zero;
    public Vector3 CenterPosition => Chunk?.CenterPosition ?? Vector3.Zero;
    public Vector3 Size => Chunk?.Size ?? Vector3.Zero;

    public ChunkMesh(Chunk chunk)
    {
        Chunk = chunk;

        if (Chunk != null)
            State = ChunkMeshState.Ready;
    }


    public void UpdateMesh()
    {
        if (Chunk == null || State != ChunkMeshState.Rendered)
            return;

        var mesh = Chunk.GetMesh();
        MeshInstance?.SetDeferred(MeshInstance3D.PropertyName.Mesh, mesh);
        CollisionShape?.SetDeferred(CollisionShape3D.PropertyName.Shape, mesh.CreateTrimeshShape());
    }
}