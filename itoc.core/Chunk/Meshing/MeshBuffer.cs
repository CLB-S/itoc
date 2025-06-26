using System.Buffers;

namespace ITOC.Core.ChunkMeshing;

public class MeshBuffer : IDisposable
{
    public ulong[] FaceMasks;
    public byte[] ForwardMerged;
    public byte[] RightMerged;
    public ulong[] OpaqueMask;
    public ulong[] TransparentMask;

    private const int FACE_MASKS_LENGTH = ChunkMesher.CS_2 * 6;
    private const int FORWARD_MERGED_LENGTH = ChunkMesher.CS_2;
    private const int RIGHT_MERGED_LENGTH = ChunkMesher.CS;
    private bool _disposed;

    public MeshBuffer(ulong[] opaqueMask, ulong[] transparentMask = null)
    {
        FaceMasks = ArrayPool<ulong>.Shared.Rent(FACE_MASKS_LENGTH);
        ForwardMerged = ArrayPool<byte>.Shared.Rent(FORWARD_MERGED_LENGTH);
        RightMerged = ArrayPool<byte>.Shared.Rent(RIGHT_MERGED_LENGTH);

        Array.Clear(FaceMasks, 0, FACE_MASKS_LENGTH);
        Array.Clear(ForwardMerged, 0, FORWARD_MERGED_LENGTH);
        Array.Clear(RightMerged, 0, RIGHT_MERGED_LENGTH);

        OpaqueMask = opaqueMask;
        TransparentMask = transparentMask;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            ArrayPool<ulong>.Shared.Return(FaceMasks);
            ArrayPool<byte>.Shared.Return(ForwardMerged);
            ArrayPool<byte>.Shared.Return(RightMerged);
            _disposed = true;
        }
    }

    ~MeshBuffer()
    {
        Dispose();
    }
}
