using Godot;

namespace ITOC;

public interface IChunkData
{
    int Lod => 0;
    Vector3I Index { get; }
    Vector3 WorldPosition => Index * ChunkMesher.CS * (1 << Lod);
    Vector3 CenterPosition => (Index + Vector3I.One / 2) * ChunkMesher.CS * (1 << Lod);
    int Size => ChunkMesher.CS * (1 << Lod);
    Block GetBlock(int axis, int a, int b, int c);
    Block GetBlock(int x, int y, int z);
    Block GetBlock(Vector3I pos) => GetBlock(pos.X, pos.Y, pos.Z);
    ChunkMesher.MeshData GetRawMeshData();
    double GetDistanceTo(Vector3 pos) => CenterPosition.DistanceTo(pos);
    void SetBlock(int x, int y, int z, string blockId);
    void SetBlock(Vector3I pos, string blockId) => SetBlock(pos.X, pos.Y, pos.Z, blockId);
    void SetBlock(int x, int y, int z, Block block);
    void SetBlock(Vector3I pos, Block block) => SetBlock(pos.X, pos.Y, pos.Z, block);
}