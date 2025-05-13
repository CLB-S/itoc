using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Godot;
using ITOC.Chunks;

namespace ITOC;

public class World
{
    public WorldGenerator Generator { get; private set; }
    public WorldSettings Settings => Generator.Settings;

    public Vector3I PlayerChunk { get; private set; } = Vector3I.Zero;
    public double Time { get; private set; }

    public readonly ConcurrentDictionary<Vector3I, ChunkNode> Chunks = new();
    public readonly ConcurrentDictionary<Vector2I, ChunkColumn> ChunkColumns = new();

    public ChunkMultiPassGenerator ChunkGenerator { get; private set; }

    public bool UseDebugMaterial = false;
    public ShaderMaterial DebugMaterial;

    private IPass _chunkGenerationPass0;
    private IPass _chunkGenerationPass1;
    private bool _ready; //TODO: State
    private Vector3 _lastPlayerPosition = Vector3.Inf;
    private readonly Queue<Vector2I> _chunkColumnsGenerationQueue = new();

    public Vector3 PlayerPos { get; private set; } = Vector3.Zero;

    public World()
    {
        Generator = Core.Instance.WorldGenerator;

        Time += Settings.MinutesPerDay * 60.0f / 3; // 8:00 AM

        _chunkGenerationPass0 = new ChunkColumnGenerationInitialPass(this);
        _chunkGenerationPass1 = new ChunkColumnGenerationSecondaryPass(this);
        ChunkGenerator = new ChunkMultiPassGenerator(true, _chunkGenerationPass0, _chunkGenerationPass1);

        DebugMaterial = ResourceLoader.Load<ShaderMaterial>("res://assets/graphics/chunk_debug_shader_material.tres");

        _ready = true;
    }

    public void PhysicsProcess(double delta)
    {
        if (!_ready) return;

        Time += delta;
        PlayerPos = GetPlayerPosition();

        if ((PlayerPos - _lastPlayerPosition).Length() > ChunkMesher.CS / 2)
        {
            PlayerChunk = WorldToChunkPosition(PlayerPos);
            UpdateChunkLoading();
            _lastPlayerPosition = PlayerPos;
        }

        var processed = 0;
        while (_chunkColumnsGenerationQueue.Count > 0 && processed < Core.Instance.Settings.MaxChunkGenerationsPerFrame)
        {
            var pos = _chunkColumnsGenerationQueue.Dequeue();

            // var columnTask = new FunctionTask<ChunkColumn>(
            //     () => Generator.GenerateChunkColumn(pos),
            //     ChunkColumnGenerationCallback
            // );
            // Core.Instance.TaskManager.EnqueueTask(columnTask);
            _chunkGenerationPass0.ExecuteAt(pos);

            processed++;
        }
    }

    #region Get and Set methods
    // TODO: Revise.

    public ChunkNode GetChunkWorldPos(Vector3 worldPos)
    {
        var chunkPos = WorldToChunkPosition(worldPos);
        Chunks.TryGetValue(chunkPos, out var chunk);
        return chunk;
    }

    public ChunkNode GetChunk(Vector3I chunkPos)
    {
        Chunks.TryGetValue(chunkPos, out var chunk);
        return chunk;
    }

    public ChunkColumn GetChunkColumn(Vector2I columnPos)
    {
        ChunkColumns.TryGetValue(columnPos, out var chunkColumn);
        return chunkColumn;
    }

    public Block GetBlock(Vector3 worldPos)
    {
        var chunk = GetChunkWorldPos(worldPos);
        if (chunk == null) return null;

        var localPos = WorldToLocalPosition(worldPos);
        return chunk.GetBlock(Mathf.FloorToInt(localPos.X), Mathf.FloorToInt(localPos.Y), Mathf.FloorToInt(localPos.Z));
    }

    public void SetBlock(Vector3 worldPos, string blockId)
    {
        var block = BlockManager.Instance.GetBlock(blockId);
        SetBlock(worldPos, block);
    }

    public void SetBlock(Vector3 worldPos, Block block)
    {
        var chunk = GetChunkWorldPos(worldPos);
        if (chunk == null) return;

        var localPos = WorldToLocalPosition(worldPos);
        chunk.SetBlock(Mathf.FloorToInt(localPos.X), Mathf.FloorToInt(localPos.Y), Mathf.FloorToInt(localPos.Z), block);
    }

    private Vector3 GetPlayerPosition()
    {
        return CameraHelper.Instance.GetCameraPosition();
    }

    #endregion


    #region Utility methods

    public static Vector3I WorldToChunkPosition(Vector3 worldPos)
    {
        return new Vector3I(
            Mathf.FloorToInt(worldPos.X / ChunkMesher.CS),
            Mathf.FloorToInt(worldPos.Y / ChunkMesher.CS),
            Mathf.FloorToInt(worldPos.Z / ChunkMesher.CS)
        );
    }

    public static Vector3 WorldToLocalPosition(Vector3 worldPos)
    {
        return new Vector3(
            Mathf.PosMod(worldPos.X, ChunkMesher.CS),
            Mathf.PosMod(worldPos.Y, ChunkMesher.CS),
            Mathf.PosMod(worldPos.Z, ChunkMesher.CS)
        );
    }

    #endregion

    #region Chunk generation

    private void UpdateChunkLoading()
    {
        var playerChunkXZ = new Vector2I(PlayerChunk.X, PlayerChunk.Z);
        var renderArea = new HashSet<Vector2I>();

        // Load Area
        for (var x = -Core.Instance.Settings.RenderDistance; x <= Core.Instance.Settings.RenderDistance; x++)
            for (var z = -Core.Instance.Settings.RenderDistance; z <= Core.Instance.Settings.RenderDistance; z++)
            {
                var pos = playerChunkXZ + new Vector2I(x, z);
                if (pos.DistanceTo(playerChunkXZ) <= Core.Instance.Settings.RenderDistance) renderArea.Add(pos);
            }

        // TODO: Unload
        // foreach (var existingPos in Chunks.Keys)
        //     if (!renderArea.Contains(new Vector2I(existingPos.X, existingPos.Z)))
        //         if (Chunks.TryRemove(existingPos, out var chunk))
        //             chunk.UnloadDeferred();

        // foreach (var existingPos in ChunkColumns.Keys)
        //     if (!renderArea.Contains(existingPos))
        //         if (ChunkColumns.TryRemove(existingPos, out var chunkColumn))
        //             chunkColumn = null;

        // ChunkColumns to generate 
        var toGenerate = new List<Vector2I>();
        foreach (var pos in renderArea)
            if (!ChunkColumns.ContainsKey(pos))
                toGenerate.Add(pos);

        // Sort by distance.
        toGenerate.Sort((a, b) => a.DistanceTo(playerChunkXZ).CompareTo(b.DistanceTo(playerChunkXZ)));

        // Reset the generation queue and queued set to ensure proper sorting by new player position
        _chunkColumnsGenerationQueue.Clear();
        foreach (var pos in toGenerate)
            _chunkColumnsGenerationQueue.Enqueue(pos);

        // Generate 3x3x3 chunks around the player for existing ChunkColumns
        // GeneratePlayerSurroundingChunks();
    }

    // TODO
    private void GeneratePlayerSurroundingChunks()
    {
        // Generate 3x3x3 area around player
        for (var x = -1; x <= 1; x++)
            for (var y = -1; y <= 1; y++)
                for (var z = -1; z <= 1; z++)
                {
                    var chunkPos = PlayerChunk + new Vector3I(x, y, z);
                    var columnPos = new Vector2I(chunkPos.X, chunkPos.Z);

                    // Skip if chunk already exists
                    if (Chunks.ContainsKey(chunkPos))
                        continue;

                    // Only generate chunks for columns that already exist
                    if (ChunkColumns.TryGetValue(columnPos, out var chunkColumn))
                    {
                        var createCollisionShape = chunkPos.DistanceTo(PlayerChunk) <= Core.Instance.Settings.PhysicsDistance;
                        var chunkTask = new ChunkGenerationTask(Generator, chunkPos, chunkColumn, ChunkGenerationCallback);
                        Core.Instance.TaskManager.EnqueueTask(chunkTask);
                    }
                }
    }

    private void ChunkGenerationCallback(ChunkData result)
    {
        if (result == null) return;

        // var currentPlayerPos = GetPlayerPosition();
        // var currentCenter = WorldToChunkPosition(currentPlayerPos);
        // if (result.ChunkData.GetPosition().DistanceTo(currentCenter) > Core.Instance.Settings.LoadDistance) return;

        var position = result.GetPosition();
        var positionXZ = new Vector2I(position.X, position.Z);
        var playerPosition = new Vector2I(PlayerChunk.X, PlayerChunk.Z);
        if (!Chunks.ContainsKey(position) && ChunkColumns.TryGetValue(positionXZ, out var chunkColumn)
                                          && playerPosition.DistanceTo(positionXZ) <=
                                          Core.Instance.Settings.RenderDistance)
        {
            // TODO: Rendering
            var chunk = new ChunkNode(result);
            Chunks[position] = chunk;
            chunkColumn.Chunks[position] = chunk;
            // CallDeferred(Node.MethodName.AddChild, chunk);
            UpdateNeighborMesherMasks(chunk);
            chunk.LoadDeferred();
        }
    }

    public void UpdateNeighborMesherMasks(ChunkNode chunk)
    {
        var chunkPos = chunk.ChunkPosition;

        if (Chunks.TryGetValue(new Vector3I(chunkPos.X + 1, chunkPos.Y, chunkPos.Z), out var positiveXNeighbor))
        {
            for (var y = 0; y < ChunkMesher.CS; y++)
                for (var z = 0; z < ChunkMesher.CS; z++)
                {
                    var block = chunk.GetBlock(ChunkMesher.CS - 1, y, z);
                    positiveXNeighbor.ChunkData.SetMesherMask(0, y + 1, z + 1, block);

                    var neighborBlock = positiveXNeighbor.GetBlock(0, y, z);
                    chunk.ChunkData.SetMesherMask(ChunkMesher.CS_P - 1, y + 1, z + 1, neighborBlock);
                }

            positiveXNeighbor.UpdateMeshIfNeededDeferred();
        }

        if (Chunks.TryGetValue(new Vector3I(chunkPos.X - 1, chunkPos.Y, chunkPos.Z), out var negativeXNeighbor))
        {
            for (var y = 0; y < ChunkMesher.CS; y++)
                for (var z = 0; z < ChunkMesher.CS; z++)
                {
                    var block = chunk.GetBlock(0, y, z);
                    negativeXNeighbor.ChunkData.SetMesherMask(ChunkMesher.CS_P - 1, y + 1, z + 1, block);

                    var neighborBlock = negativeXNeighbor.GetBlock(ChunkMesher.CS - 1, y, z);
                    chunk.ChunkData.SetMesherMask(0, y + 1, z + 1, neighborBlock);
                }

            negativeXNeighbor.UpdateMeshIfNeededDeferred();
        }

        if (Chunks.TryGetValue(new Vector3I(chunkPos.X, chunkPos.Y + 1, chunkPos.Z), out var positiveYNeighbor))
        {
            for (var x = 0; x < ChunkMesher.CS; x++)
                for (var z = 0; z < ChunkMesher.CS; z++)
                {
                    var block = chunk.GetBlock(x, ChunkMesher.CS - 1, z);
                    positiveYNeighbor.ChunkData.SetMesherMask(x + 1, 0, z + 1, block);

                    var neighborBlock = positiveYNeighbor.GetBlock(x, 0, z);
                    chunk.ChunkData.SetMesherMask(x + 1, ChunkMesher.CS_P - 1, z + 1, neighborBlock);
                }

            positiveYNeighbor.UpdateMeshIfNeededDeferred();
        }

        if (Chunks.TryGetValue(new Vector3I(chunkPos.X, chunkPos.Y - 1, chunkPos.Z), out var negativeYNeighbor))
        {
            for (var x = 0; x < ChunkMesher.CS; x++)
                for (var z = 0; z < ChunkMesher.CS; z++)
                {
                    var block = chunk.GetBlock(x, 0, z);
                    negativeYNeighbor.ChunkData.SetMesherMask(x + 1, ChunkMesher.CS_P - 1, z + 1, block);

                    var neighborBlock = negativeYNeighbor.GetBlock(x, ChunkMesher.CS - 1, z);
                    chunk.ChunkData.SetMesherMask(x + 1, 0, z + 1, neighborBlock);
                }

            negativeYNeighbor.UpdateMeshIfNeededDeferred();
        }

        if (Chunks.TryGetValue(new Vector3I(chunkPos.X, chunkPos.Y, chunkPos.Z + 1), out var positiveZNeighbor))
        {
            for (var x = 0; x < ChunkMesher.CS; x++)
                for (var y = 0; y < ChunkMesher.CS; y++)
                {
                    var block = chunk.GetBlock(x, y, ChunkMesher.CS - 1);
                    positiveZNeighbor.ChunkData.SetMesherMask(x + 1, y + 1, 0, block);

                    var neighborBlock = positiveZNeighbor.GetBlock(x, y, 0);
                    chunk.ChunkData.SetMesherMask(x + 1, y + 1, ChunkMesher.CS_P - 1, neighborBlock);
                }

            positiveZNeighbor.UpdateMeshIfNeededDeferred();
        }

        if (Chunks.TryGetValue(new Vector3I(chunkPos.X, chunkPos.Y, chunkPos.Z - 1), out var negativeZNeighbor))
        {
            for (var x = 0; x < ChunkMesher.CS; x++)
                for (var y = 0; y < ChunkMesher.CS; y++)
                {
                    var block = chunk.GetBlock(x, y, 0);
                    negativeZNeighbor.ChunkData.SetMesherMask(x + 1, y + 1, ChunkMesher.CS_P - 1, block);

                    var neighborBlock = negativeZNeighbor.GetBlock(x, y, ChunkMesher.CS - 1);
                    chunk.ChunkData.SetMesherMask(x + 1, y + 1, 0, neighborBlock);
                }

            negativeZNeighbor.UpdateMeshIfNeededDeferred();
        }
    }

    #endregion
}