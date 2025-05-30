namespace ITOC.Core.ChunkMeshing;

public class MeshBuffer
{
    public int Lod = 0;
    public ulong[] FaceMasks = new ulong[ChunkMesher.CS_2 * 6];
    public byte[] ForwardMerged = new byte[ChunkMesher.CS_2];
    public byte[] RightMerged = new byte[ChunkMesher.CS];
    public ulong[] OpaqueMask; // Each mask is 32 KB.
    public ulong[] TransparentMask;

    public MeshBuffer(ulong[] opaqueMask, ulong[] transparentMask = null)
    {
        OpaqueMask = opaqueMask;
        TransparentMask = transparentMask;
    }

    public MeshBuffer()
    {
        OpaqueMask = new ulong[ChunkMesher.CS_P2];
    }
}