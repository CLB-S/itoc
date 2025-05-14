using System;
using System.Collections.Concurrent;
using Godot;
using ITOC;
using ITOC.Libs.NodePool;

public partial class ChunkRenderer : Node3D
{
    public ConcurrentDictionary<Vector3I, ChunkMesh> ChunkMeshes { get; private set; } = new();

    private NodePool<MeshInstance3D> _meshPool;
    private Vector3 _playerPosition = Vector3.Zero;
    private ConcurrentDictionary<Vector3I, Chunk> _chunksToUpdateMesh = new();

    public override void _Ready()
    {
        _meshPool = new NodePool<MeshInstance3D>(() => new MeshInstance3D(), this, 100);
    }

    public void UpdatePlayerPosition(Vector3 position)
    {
        _playerPosition = position;
    }

    public void UpdateRendering()
    {
        // Update chunk meshes and remove them from the dictionary
        foreach (var chunk in _chunksToUpdateMesh)
            UpdateChunk(chunk.Value);
        _chunksToUpdateMesh.Clear();

        foreach (var chunkMesh in ChunkMeshes.Values)
        {
            if (chunkMesh.State == ChunkMeshState.Ready)
            {
                var meshNode = _meshPool.Get();
                meshNode.Position = chunkMesh.Position;
                meshNode.Mesh = chunkMesh.Mesh;
                chunkMesh.State = ChunkMeshState.Rendered;
                chunkMesh.MeshInstance = meshNode;
            }
            else if (chunkMesh.State == ChunkMeshState.NeedUpdate)
            {
                chunkMesh.MeshInstance.Mesh = chunkMesh.Mesh;
                chunkMesh.State = ChunkMeshState.Rendered;
            }
        }
    }

    public void AddChunk(Chunk chunk)
    {
        var startTime = DateTime.Now;
        ChunkMeshes.TryAdd(chunk.Position, new ChunkMesh(chunk.Position, chunk.GetMesh()));
        GD.Print($"ChunkMesh at {chunk.Position} created in {(DateTime.Now - startTime).TotalMilliseconds} ms.");
    }

    public void QueueChunkForUpdate(Chunk chunk)
    {
        _chunksToUpdateMesh.TryAdd(chunk.Position, chunk);
    }

    public void UpdateChunk(Chunk chunk)
    {
        ChunkMeshes.AddOrUpdate(chunk.Position, new ChunkMesh(chunk.Position, chunk.GetMesh()),
            (key, oldValue) =>
            {
                oldValue.Mesh = chunk.GetMesh();
                if (oldValue.State == ChunkMeshState.Rendered)
                    oldValue.State = ChunkMeshState.NeedUpdate;
                return oldValue;
            });
    }
}