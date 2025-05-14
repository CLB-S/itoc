using Godot;

namespace ITOC;

public enum ChunkMeshState
{
    Created,
    Ready,
    Rendered,
    NeedUpdate,
}

public class ChunkMesh
{
    public ChunkMeshState State { get; set; } = ChunkMeshState.Created;
    public Mesh Mesh { get; set; }
    public MeshInstance3D MeshInstance { get; set; }

    public int LodLevel { get; private set; }
    public Vector3I Index { get; private set; }
    public Vector3 Position => Index * ChunkMesher.CS * (1 << LodLevel);

    public ChunkMesh(Vector3I index, Mesh mesh, int lodLevel = 0)
    {
        Index = index;
        Mesh = mesh;
        LodLevel = lodLevel;

        if (Mesh != null)
            State = ChunkMeshState.Ready;
    }
}