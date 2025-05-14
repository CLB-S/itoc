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

    public override void _Ready()
    {
        _meshPool = new NodePool<MeshInstance3D>(() => new MeshInstance3D(), this, 100);
    }

    public void UpdatePlayerPosition(Vector3 position)
    {
        _playerPosition = position;
    }

    public void RenderAll()
    {
        foreach (var chunkMesh in ChunkMeshes.Values)
        {
            if (chunkMesh.IsRendering)
                continue;

            var meshNode = _meshPool.Get();
            meshNode.Position = chunkMesh.Position;
            meshNode.Mesh = chunkMesh.Mesh;
            chunkMesh.IsRendering = true;
        }
    }

    public void AddChunk(Chunk chunk)
    {
        var startTime = DateTime.Now;
        ChunkMeshes.TryAdd(chunk.Position, new ChunkMesh(chunk.Position, chunk.GetMesh()));
        GD.Print($"ChunkMesh at {chunk.Position} created in {(DateTime.Now - startTime).TotalMilliseconds} ms.");
    }

    public void UpdateChunk(Chunk chunk)
    {
        // TODO: Update node also.
        ChunkMeshes.AddOrUpdate(chunk.Position, new ChunkMesh(chunk.Position, chunk.GetMesh()),
            (key, oldValue) =>
            {
                oldValue.Mesh = chunk.GetMesh();
                return oldValue;
            });
    }
}