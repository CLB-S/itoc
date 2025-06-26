namespace ITOC.Core.ChunkMeshing;

public class MeshResult
{
    public int[] FaceVertexBegin = new int[6];
    public int[] FaceVertexLength = new int[6];
    public List<Block> QuadBlocks;
    public List<ulong> Quads = new(1000);
}
