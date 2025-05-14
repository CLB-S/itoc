using System;
using System.Collections.Concurrent;
using Godot;
using ITOC;
using ITOC.Libs.NodePool;

public partial class ChunkInstantiator : Node3D
{
    public ConcurrentDictionary<Vector3I, ChunkMesh> ChunkMeshes { get; private set; } = new();

    private NodePool<MeshInstance3D> _meshPool;
    private NodePool<StaticBody3D> _collisionBodyPool;
    private Vector3 _playerPosition = Vector3.Zero;
    private ConcurrentDictionary<Vector3I, Chunk> _chunksToUpdateMesh = new();

    public override void _Ready()
    {
        _meshPool = new NodePool<MeshInstance3D>(() => new MeshInstance3D(), this, 100);
        _collisionBodyPool = new NodePool<StaticBody3D>(() =>
        {
            var body = new StaticBody3D();
            body.AddChild(new CollisionShape3D());
            return body;
        }, this, 50);
    }

    public void UpdatePlayerPosition(Vector3 position)
    {
        _playerPosition = position;

        // Update collision shapes based on physics distance
        UpdateCollisionShapes();
    }

    private void UpdateCollisionShapes()
    {
        var playerChunkPos = World.WorldToChunkPosition(_playerPosition);
        var physicsDistance = Core.Instance.Settings.PhysicsDistance;

        foreach (var chunkMesh in ChunkMeshes.Values)
        {
            bool shouldHaveCollision = chunkMesh.Index.DistanceTo(playerChunkPos) <= physicsDistance;
            bool shouldRemoveCollision = chunkMesh.Index.DistanceTo(playerChunkPos) >= physicsDistance + 1;

            // Add collision if needed and not already present
            if (shouldHaveCollision && chunkMesh.State == ChunkMeshState.Rendered && chunkMesh.CollisionBody == null)
            {
                var collisionBody = _collisionBodyPool.Get();
                collisionBody.Position = chunkMesh.Position;

                var collisionShape = collisionBody.GetChild<CollisionShape3D>(0);
                collisionShape.Shape = chunkMesh.Mesh?.CreateTrimeshShape();

                chunkMesh.CollisionBody = collisionBody;
            }
            // Remove collision if it's out of range but has collision
            else if (shouldRemoveCollision && chunkMesh.CollisionBody != null)
            {
                _collisionBodyPool.Release(chunkMesh.CollisionBody);
                chunkMesh.CollisionBody = null;
            }
        }
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

                // If the mesh is already rendered, mark it for update
                if (oldValue.State == ChunkMeshState.Rendered)
                    oldValue.State = ChunkMeshState.NeedUpdate;

                return oldValue;
            });
    }

    public void RemoveChunk(Vector3I position)
    {
        if (ChunkMeshes.TryRemove(position, out var chunkMesh))
        {
            if (chunkMesh.MeshInstance != null)
            {
                _meshPool.Release(chunkMesh.MeshInstance);
            }

            if (chunkMesh.CollisionBody != null)
            {
                _collisionBodyPool.Release(chunkMesh.CollisionBody);
            }
        }
    }

    public void Cleanup()
    {
        foreach (var chunkMesh in ChunkMeshes.Values)
        {
            if (chunkMesh.MeshInstance != null)
                _meshPool.Release(chunkMesh.MeshInstance);

            if (chunkMesh.CollisionBody != null)
                _collisionBodyPool.Release(chunkMesh.CollisionBody);
        }

        ChunkMeshes.Clear();
    }
}