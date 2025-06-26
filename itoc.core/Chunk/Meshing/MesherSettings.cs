namespace ITOC.Core.ChunkMeshing;

public class MesherSettings
{
    /// <summary>
    /// Enable greedy meshing.
    /// Greedy meshing is a technique to reduce the number of quads by merging adjacent quads of the same type.
    /// </summary>
    public bool EnableGreedyMeshing = true;

    /// <summary>
    /// Ignore the block type when merging quads.
    /// </summary>
    public bool IgnoreBlockType = false;
}
