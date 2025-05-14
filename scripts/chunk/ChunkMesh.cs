using Godot;

namespace ITOC;

public class ChunkMesh
{
    public bool IsRendering { get; set; } = false;
    public Mesh Mesh { get; set; }

    public int LodLevel { get; private set; }
    public Vector3I Index { get; private set; }
    public Vector3 Position => Index * ChunkMesher.CS * (1 << LodLevel);

    public ChunkMesh(Vector3I index, Mesh mesh, int lodLevel = 0)
    {
        Index = index;
        Mesh = mesh;
        LodLevel = lodLevel;
    }
}