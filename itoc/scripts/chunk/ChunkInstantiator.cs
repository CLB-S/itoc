using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Godot;
using ITOC;
using ITOC.Libs.NodePool;
using ITOC.Multithreading;

public partial class ChunkInstantiator : Node3D
{
    // Dictionary for each LOD level, with key = chunk index, value = ChunkMesh
    private Dictionary<int, ConcurrentDictionary<Vector3I, ChunkMesh>> _lodChunkMeshes = new();

    private NodePool<MeshInstance3D> _meshPool;
    private NodePool<StaticBody3D> _collisionBodyPool;
    private Vector3 _playerPosition = Vector3.Zero;
    private ConcurrentDictionary<(int, Vector3I), Chunk> _chunksToUpdateMesh = new();
    // Dictionary to track ChunkLods at each level
    private Dictionary<int, ConcurrentDictionary<Vector3I, ChunkLod>> _lodChunks = new();
    private int _maxLodLevel = 0;
    private double[] _lodDistanceThresholds;

    private World _world;

    public ChunkInstantiator(World world)
    {
        _world = world;
        _world.OnPlayerMoved += (s, pos) => UpdatePlayerPosition(pos);
        _world.OnPlayerMovedHalfAChunk += (s, pos) => UpdateCollisionShapes();
        _world.OnChunkGenerated += (s, chunk) => AddChunk(chunk);
        _world.OnChunkMeshUpdated += (s, chunk) => QueueChunkForUpdate(chunk);
    }

    public override void _Ready()
    {
        _meshPool = new NodePool<MeshInstance3D>(() => new MeshInstance3D(), this, 100);
        _collisionBodyPool = new NodePool<StaticBody3D>(() =>
        {
            var body = new StaticBody3D();
            body.AddChild(new CollisionShape3D());
            return body;
        }, this, 50);

        // Initialize LOD dictionaries
        _maxLodLevel = Core.Instance.Settings.MaxLodLevel;
        for (int lod = 0; lod <= _maxLodLevel; lod++)
            _lodChunkMeshes[lod] = new ConcurrentDictionary<Vector3I, ChunkMesh>();

        for (int lod = 1; lod <= _maxLodLevel; lod++)
            _lodChunks[lod] = new ConcurrentDictionary<Vector3I, ChunkLod>();

        _lodDistanceThresholds = new double[_maxLodLevel];
        UpdateLodThreshoulds();
    }

    public void UpdatePlayerPosition(Vector3 position)
    {
        _playerPosition = position;
        UpdateInstances();
    }

    private void UpdateCollisionShapes()
    {
        var playerChunkPos = World.WorldToChunkPosition(_playerPosition);
        var physicsDistance = Core.Instance.Settings.PhysicsDistance;

        foreach (var chunkMesh in _lodChunkMeshes[0].Values)
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

    private void UpdateLodThreshoulds()
    {
        double pixelThreshold = Core.Instance.Settings.LodPixelThreshold;

        // Calculate distance thresholds for each LOD level
        for (int lod = 0; lod < _maxLodLevel; lod++)
            _lodDistanceThresholds[lod] = CameraHelper.Instance.CalculateDistanceThresholdForPixels(pixelThreshold, 1 << lod);

        for (int lod = 0; lod < _lodDistanceThresholds.Length; lod++)
            GD.Print($"LOD {lod} distance threshold: {_lodDistanceThresholds[lod]}");
    }

    // Determine the appropriate LOD level based on distance
    private int DetermineLodLevel(double distance)
    {
        for (int lod = 0; lod < _lodDistanceThresholds.Length; lod++)
            if (distance < _lodDistanceThresholds[lod])
                return lod;

        return _lodDistanceThresholds.Length - 1; // Return the highest LOD level if beyond all thresholds
    }

    // Calculate the parent chunk index at a specific LOD level
    private static Vector3I CalculateLodParentIndex(Vector3I chunkIndex)
    {
        return new Vector3I(
            Mathf.FloorToInt(chunkIndex.X / 2.0),
            Mathf.FloorToInt(chunkIndex.Y / 2.0),
            Mathf.FloorToInt(chunkIndex.Z / 2.0)
        );
    }

    private static Vector3I CalculateLodParentIndex(Vector3I chunkIndex, int lodLevel)
    {
        var scalingFactor = 1 << lodLevel; // 2^lodLevel
        return new Vector3I(
            Mathf.FloorToInt(chunkIndex.X / (double)scalingFactor),
            Mathf.FloorToInt(chunkIndex.Y / (double)scalingFactor),
            Mathf.FloorToInt(chunkIndex.Z / (double)scalingFactor)
        );
    }

    public void UpdateInstances()
    {
        // Update chunk meshes and remove them from the dictionary
        foreach (var chunk in _chunksToUpdateMesh)
            AddOrUpdateChunkMesh(chunk.Value);
        _chunksToUpdateMesh.Clear();

        // Track chunks that should be visible at each LOD level
        var visibleChunks = new HashSet<(int, Vector3I)>();

        // Calculate LOD visibility starting from base level
        foreach (var chunkMesh in _lodChunkMeshes[0].Values)
            visibleChunks.Add((0, chunkMesh.Index));

        for (int lod = 1; lod <= _maxLodLevel; lod++)
        {
            foreach (var chunkMesh in _lodChunkMeshes[lod].Values)
            {
                var distanceToPlayer = chunkMesh.CenterPosition.DistanceTo(_playerPosition);
                int appropriateLod = DetermineLodLevel(distanceToPlayer);

                // If this chunk should be rendered at this LOD level
                if (appropriateLod >= lod)
                {
                    // Mark this chunk as visible
                    visibleChunks.Add((lod, chunkMesh.Index));

                    // If this is a higher LOD level, mark child chunks as covered
                    if (_lodChunks[lod].TryGetValue(chunkMesh.Index, out var lodChunk))
                        foreach (var childChunk in lodChunk.GetChildChunks())
                            visibleChunks.Remove((childChunk.Lod, childChunk.Index));
                }
            }
        }

        for (int i = 0; i <= _maxLodLevel; i++)
            foreach (var chunkMesh in _lodChunkMeshes[i].Values)
            {
                if (!visibleChunks.Contains((i, chunkMesh.Index)))
                    HideChunkMesh(chunkMesh);
                else
                    EnsureChunkIsRendered(chunkMesh);
            }
    }

    public void UpdateInstancesDeferred()
    {
        CallDeferred(nameof(UpdateInstances));
    }

    private void EnsureChunkIsRendered(ChunkMesh chunkMesh)
    {
        if (chunkMesh.State == ChunkMeshState.Rendered)
            return;

        if (chunkMesh.Mesh == null || chunkMesh.State == ChunkMeshState.Created)
            return;

        // Create mesh instance if needed
        if (chunkMesh.MeshInstance == null)
        {
            chunkMesh.MeshInstance = _meshPool.Get();
            chunkMesh.MeshInstance.Position = chunkMesh.Position;
        }

        chunkMesh.MeshInstance.Mesh = chunkMesh.Mesh;
        chunkMesh.MeshInstance.Visible = true;
        chunkMesh.State = ChunkMeshState.Rendered;
    }

    private void HideChunkMesh(ChunkMesh chunkMesh)
    {
        if (chunkMesh.MeshInstance != null)
        {
            _meshPool.Release(chunkMesh.MeshInstance);
            chunkMesh.MeshInstance = null;
            chunkMesh.State = ChunkMeshState.Ready;
        }
    }

    public void AddChunk(Chunk chunk)
    {
        if (chunk.Lod != 0)
            throw new ArgumentException("Chunk must be at LOD 0 to be added.");

        var task = new ActionTask(() =>
        {
            AddOrUpdateChunkMesh(chunk);
            GenerateOrUpdateParentLodChunk(chunk);
            UpdateInstancesDeferred();
        }, "ChunkInstantiator.AddChunk");

        Core.Instance.TaskManager.EnqueueTask(task);
    }

    private void AddOrUpdateChunkMesh(Chunk chunk)
    {
        // Add or update the LOD chunk mesh
        _lodChunkMeshes[chunk.Lod].AddOrUpdate(chunk.Index,
            new ChunkMesh(chunk.Index, chunk.GetMesh(), chunk.Lod),
            (key, oldValue) =>
            {
                oldValue.Mesh = chunk.GetMesh();
                oldValue.State = ChunkMeshState.NeedUpdate;
                return oldValue;
            });
    }

    private void GenerateOrUpdateParentLodChunk(Chunk chunk)
    {
        if (chunk.Lod == _maxLodLevel)
            return;

        // Calculate the parent chunk index at this LOD level
        Vector3I lodParentIndex = CalculateLodParentIndex(chunk.Index);
        var parentLod = chunk.Lod + 1;
        // Get or create the ChunkLod for this level
        if (!_lodChunks[parentLod].TryGetValue(lodParentIndex, out ChunkLod lodChunk))
        {
            lodChunk = new ChunkLod(lodParentIndex, parentLod);
            _lodChunks[parentLod].TryAdd(lodParentIndex, lodChunk);
            // lodChunk.OnMeshUpdated += (sender, args) => QueueChunkForUpdate(lodChunk);
        }

        // Set this chunk as a child of the LOD chunk
        var localPos = new Vector3I(
            Mathf.PosMod(chunk.Index.X, 2),
            Mathf.PosMod(chunk.Index.Y, 2),
            Mathf.PosMod(chunk.Index.Z, 2)
        );

        lodChunk.SetChildChunk(localPos, chunk);
        AddOrUpdateChunkMesh(lodChunk);

        GenerateOrUpdateParentLodChunk(lodChunk);
    }

    public void QueueChunkForUpdate(Chunk chunk)
    {
        _chunksToUpdateMesh.TryAdd((chunk.Lod, chunk.Index), chunk);
    }

    public void RemoveChunk(Vector3I index)
    {
        // Remove the base chunk mesh (LOD 0)
        if (_lodChunkMeshes[0].TryRemove(index, out var chunkMesh))
        {
            if (chunkMesh.MeshInstance != null)
                _meshPool.Release(chunkMesh.MeshInstance);

            if (chunkMesh.CollisionBody != null)
                _collisionBodyPool.Release(chunkMesh.CollisionBody);
        }

        // TODO: LOD logic
    }


    public void Cleanup()
    {
        // Clean up base chunk meshes (LOD 0)
        foreach (var chunkMesh in _lodChunkMeshes[0].Values)
        {
            if (chunkMesh.MeshInstance != null)
                _meshPool.Release(chunkMesh.MeshInstance);

            if (chunkMesh.CollisionBody != null)
                _collisionBodyPool.Release(chunkMesh.CollisionBody);
        }
        _lodChunkMeshes[0].Clear();

        // Clean up LOD chunk meshes
        for (int lod = 1; lod <= _maxLodLevel; lod++)
        {
            foreach (var lodMesh in _lodChunkMeshes[lod].Values)
            {
                if (lodMesh.MeshInstance != null)
                    _meshPool.Release(lodMesh.MeshInstance);
            }
            _lodChunkMeshes[lod].Clear();
            _lodChunks[lod].Clear();
        }
    }
}