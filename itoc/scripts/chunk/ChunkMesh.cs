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
    private Mesh _mesh;
    public Mesh Mesh
    {
        get { return _mesh; }
        set
        {
            _mesh = value;
            MeshInstance?.SetDeferred(MeshInstance3D.PropertyName.Mesh, value);
            CollisionShape?.SetDeferred(CollisionShape3D.PropertyName.Shape, value.CreateTrimeshShape());
        }
    }

    public MeshInstance3D MeshInstance { get; set; }
    public StaticBody3D CollisionBody { get; set; }
    public CollisionShape3D CollisionShape { get; set; }

    public int Lod { get; private set; }
    public Vector3I Index { get; private set; }
    public Vector3 Position => Index * ChunkMesher.CS * (1 << Lod);
    public Vector3 CenterPosition => Position + Vector3I.One * (ChunkMesher.CS * (1 << Lod) / 2);

    public ChunkMesh(Vector3I index, Mesh mesh, int lod = 0)
    {
        Index = index;
        Mesh = mesh;
        Lod = lod;

        if (Mesh != null)
            State = ChunkMeshState.Ready;
    }
}