using Godot;
using ITOC.Core.Engine;
using ITOC.Core.WorldGeneration;

namespace ITOC.Core;

public class World : NodeAdapter
{
    public double Time { get; private set; }

    public IWorldGenerator Generator { get; private set; } // TODO: Remove this later.
    public ChunkManager ChunkManager => Generator.ChunkGenerator.ChunkManager;
    public WorldSettings Settings => Generator.WorldSettings; // TODO: Revise this later.

    public World(Node node, IWorldGenerator generator)
        : base(node)
    {
        Generator = generator;

        Time = Settings.MinutesPerDay * 60.0f / 3; // 8:00 AM
    }

    public override void OnPhysicsProcess(double delta) => Time += delta;

    #region Get
    // TODO: Revise.

    public Chunk GetChunkAt(Vector3 worldPos)
    {
        var chunkIndex = WorldPositionToChunkIndex(worldPos);
        ChunkManager.Chunks.TryGetValue(chunkIndex, out var chunk);
        return chunk;
    }

    public Chunk GetChunkAt(Vector3I chunkIndex)
    {
        ChunkManager.Chunks.TryGetValue(chunkIndex, out var chunk);
        return chunk;
    }

    public ChunkColumn GetChunkColumnAt(Vector2I columnPos)
    {
        ChunkManager.ChunkColumns.TryGetValue(columnPos, out var chunkColumn);
        return chunkColumn;
    }

    public Block GetBlockAt(Vector3I position)
    {
        var chunk = GetChunkAt(WorldPositionToChunkIndex(position));
        if (chunk == null)
            return null;

        var localPos = WorldToLocalPosition(position);
        return chunk.GetBlock(localPos.X, localPos.Y, localPos.Z);
    }

    public Block GetBlockAt(Vector3 worldPos)
    {
        var chunk = GetChunkAt(worldPos);
        if (chunk == null)
            return null;

        var localPos = WorldToLocalPosition(worldPos);
        return chunk.GetBlock(
            Mathf.FloorToInt(localPos.X),
            Mathf.FloorToInt(localPos.Y),
            Mathf.FloorToInt(localPos.Z)
        );
    }

    #endregion


    #region Set

    public void SetBlock(Vector3 worldPos, string blockId)
    {
        var block = BlockManager.Instance.GetBlock(blockId);
        SetBlock(worldPos, block);
    }

    public void SetBlock(Vector3 worldPos, Block block)
    {
        var chunk = GetChunkAt(worldPos);
        if (chunk == null)
            return;

        var localPos = WorldToLocalPosition(worldPos);
        chunk.SetBlock(
            Mathf.FloorToInt(localPos.X),
            Mathf.FloorToInt(localPos.Y),
            Mathf.FloorToInt(localPos.Z),
            block
        );
    }

    #endregion


    #region Utility methods

    public static Vector3I WorldPositionToChunkIndex(Vector3 worldPos) =>
        new Vector3I(
            Mathf.FloorToInt(worldPos.X / Chunk.SIZE),
            Mathf.FloorToInt(worldPos.Y / Chunk.SIZE),
            Mathf.FloorToInt(worldPos.Z / Chunk.SIZE)
        );

    public static Vector3I WorldToLocalPosition(Vector3I worldPos) =>
        new Vector3I(
            Mathf.PosMod(worldPos.X, Chunk.SIZE),
            Mathf.PosMod(worldPos.Y, Chunk.SIZE),
            Mathf.PosMod(worldPos.Z, Chunk.SIZE)
        );

    public static Vector3 WorldToLocalPosition(Vector3 worldPos) =>
        new Vector3(
            Mathf.PosMod(worldPos.X, Chunk.SIZE),
            Mathf.PosMod(worldPos.Y, Chunk.SIZE),
            Mathf.PosMod(worldPos.Z, Chunk.SIZE)
        );

    public static Vector2I WorldToChunkIndex(Vector2 worldPos) =>
        new Vector2I(
            Mathf.FloorToInt(worldPos.X / Chunk.SIZE),
            Mathf.FloorToInt(worldPos.Y / Chunk.SIZE)
        );

    public static Vector2 WorldToLocalPosition(Vector2 worldPos) =>
        new Vector2(Mathf.PosMod(worldPos.X, Chunk.SIZE), Mathf.PosMod(worldPos.Y, Chunk.SIZE));

    #endregion
}
