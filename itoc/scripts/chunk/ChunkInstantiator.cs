using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Godot;
using ITOC;
using ITOC.Libs.NodePool;
using ITOC.Multithreading;

public partial class ChunkInstantiator : Node3D
{
    #region Fields

    // Player and world tracking
    private World _world;
    private Vector3 _playerPosition = Vector3.Zero;
    private Vector3I _playerChunkIndex = Vector3I.Zero;

    // LOD configuration
    private int _maxLodLevel = 0;
    private double[] _lodDistanceThresholds;

    // Chunk storage
    private Dictionary<int, ConcurrentDictionary<Vector3I, ChunkMesh>> _lodChunkMeshes = new();
    private Dictionary<int, ConcurrentDictionary<Vector3I, ChunkLod>> _lodChunks = new();
    private ConcurrentQueue<Chunk> _chunksAdded = new();
    private ConcurrentDictionary<(int, Vector3I), Chunk> _chunksUpdated = new();

    // Node pools
    private NodePool<MeshInstance3D> _meshPool;
    private NodePool<StaticBody3D> _collisionBodyPool;

    #endregion

    #region Initialization

    public ChunkInstantiator(World world)
    {
        _world = world;
        _world.OnPlayerMoved += (s, pos) => UpdatePlayerPosition(pos);

        _world.OnPlayerMovedHalfAChunk += (s, pos) =>
        {
            UpdateVisibilityForAll();
            UpdateCollisionShapesForAll();
        };

        _world.OnChunkGenerated += (s, chunk) => AddChunk(chunk);
    }

    public override void _Ready()
    {
        InitializeNodePools();
        InitializeLodSettings();
        _playerChunkIndex = World.WorldToChunkIndex(_playerPosition);
    }

    private void InitializeNodePools()
    {
        _meshPool = new NodePool<MeshInstance3D>(() => new MeshInstance3D(), this, 100);
        _collisionBodyPool = new NodePool<StaticBody3D>(() =>
        {
            var body = new StaticBody3D();
            body.AddChild(new CollisionShape3D());
            return body;
        }, this, 50);
    }

    private void InitializeLodSettings()
    {
        // Initialize LOD dictionaries
        _maxLodLevel = Core.Instance.Settings.MaxLodLevel;
        for (int lod = 0; lod <= _maxLodLevel; lod++)
            _lodChunkMeshes[lod] = new ConcurrentDictionary<Vector3I, ChunkMesh>();

        for (int lod = 1; lod <= _maxLodLevel; lod++)
            _lodChunks[lod] = new ConcurrentDictionary<Vector3I, ChunkLod>();

        _lodDistanceThresholds = new double[_maxLodLevel];
        UpdateLodThreshoulds();
    }

    #endregion

    #region Engine Callbacks

    public override void _PhysicsProcess(double delta)
    {
        // Process added chunks
        while (_chunksAdded.TryDequeue(out var chunk))
        {
            // Update visibility and collision for the added chunk
            UpdateVisibility(chunk);
            if (_lodChunkMeshes[0].TryGetValue(chunk.Index, out var chunkMesh))
                UpdateCollision(chunkMesh);
        }

        // Process updated chunks
        foreach (var chunk in _chunksUpdated.Values)
            AddOrUpdateChunkMesh(chunk);

        _chunksUpdated.Clear();
    }

    #endregion

    #region Player Tracking

    public void UpdatePlayerPosition(Vector3 position)
    {
        _playerPosition = position;
        _playerChunkIndex = World.WorldToChunkIndex(_playerPosition);
    }

    #endregion


    #region Chunk Management

    public void AddChunk(Chunk chunk)
    {
        if (chunk.Lod != 0)
            throw new ArgumentException("Chunk must be at LOD 0 to be added.");

        chunk.OnMeshUpdated += (s, args) => _chunksUpdated.TryAdd((chunk.Lod, chunk.Index), chunk);

        var task = new ActionTask(() =>
        {
            AddOrUpdateChunkMesh(chunk);
            GenerateOrUpdateParentLodChunks(chunk);
            _chunksAdded.Enqueue(chunk);
        }, "ChunkInstantiator.AddChunk", TaskPriority.High);

        Core.Instance.TaskManager.EnqueueTask(task);
    }

    private ChunkMesh AddOrUpdateChunkMesh(Chunk chunk)
    {
        // Add or update the LOD chunk mesh
        _lodChunkMeshes[chunk.Lod].AddOrUpdate(chunk.Index,
            new ChunkMesh(chunk.Index, chunk.GetMesh(), chunk.Lod),
            (key, oldValue) =>
            {
                oldValue.Mesh = chunk.GetMesh();
                // GD.Print($"Chunk mesh updated at {key} for LOD {chunk.Lod}. {oldValue.CollisionShape}");
                return oldValue;
            });

        return _lodChunkMeshes[chunk.Lod][chunk.Index];
    }

    private void GenerateOrUpdateParentLodChunks(Chunk chunk)
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
            lodChunk.OnMeshUpdated += (s, args) => _chunksUpdated.TryAdd((lodChunk.Lod, lodChunk.Index), lodChunk);
        }

        // Set this chunk as a child of the LOD chunk
        var localPos = new Vector3I(
            Mathf.PosMod(chunk.Index.X, 2),
            Mathf.PosMod(chunk.Index.Y, 2),
            Mathf.PosMod(chunk.Index.Z, 2)
        );

        lodChunk.SetOrUpdateChildChunk(localPos, chunk);
        AddOrUpdateChunkMesh(lodChunk);

        GenerateOrUpdateParentLodChunks(lodChunk);
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

        // Process LOD chunks removal
        RemoveLodChunksIfEmpty(index);
    }

    private void RemoveLodChunksIfEmpty(Vector3I chunkIndex, int parentLod = 1)
    {
        if (parentLod > _maxLodLevel)
            return;

        // Calculate parent index at this LOD level
        Vector3I parentIndex = CalculateLodParentIndex(chunkIndex);

        // Check if the parent LOD chunk exists
        if (!_lodChunks[parentLod].TryGetValue(parentIndex, out var lodChunk))
            return;

        // Get the local position of this chunk within its parent
        var localPos = new Vector3I(
            Mathf.PosMod(chunkIndex.X, 2),
            Mathf.PosMod(chunkIndex.Y, 2),
            Mathf.PosMod(chunkIndex.Z, 2)
        );

        // Remove this chunk from its parent
        lodChunk.RemoveChildChunk(localPos);

        // If the parent has no more children, remove it as well
        if (lodChunk.ChildCount == 0)
        {
            // Remove from dictionaries
            _lodChunks[parentLod].TryRemove(parentIndex, out _);

            // Clean up mesh resources if the mesh exists
            if (_lodChunkMeshes[parentLod].TryRemove(parentIndex, out var parentMesh))
            {
                if (parentMesh.MeshInstance != null)
                    _meshPool.Release(parentMesh.MeshInstance);

                if (parentMesh.CollisionBody != null)
                    _collisionBodyPool.Release(parentMesh.CollisionBody);
            }

            // Recursively check higher LOD levels
            RemoveLodChunksIfEmpty(parentIndex, parentLod + 1);
        }
    }

    #endregion


    #region LOD Management

    private void UpdateLodThreshoulds()
    {
        double pixelThreshold = Core.Instance.Settings.LodPixelThreshold;

        // Calculate distance thresholds for each LOD level
        for (int lod = 0; lod < _maxLodLevel; lod++)
            _lodDistanceThresholds[lod] = CameraHelper.Instance.CalculateDistanceThresholdForPixels(pixelThreshold, 1 << lod);

        for (int lod = 0; lod < _lodDistanceThresholds.Length; lod++)
            GD.Print($"LOD {lod + 1} distance threshold: {_lodDistanceThresholds[lod]}");
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

    #endregion

    #region Visibility Management

    public void UpdateVisibilityForAll()
    {
        // Track chunks that should be visible at each LOD level
        var visibleChunks = new HashSet<(int, Vector3I)>();

        DetermineVisibleChunks(visibleChunks);
        ApplyVisibilityChanges(visibleChunks);
    }

    private void DetermineVisibleChunks(HashSet<(int, Vector3I)> visibleChunks)
    {
        // Calculate LOD visibility starting from base level
        foreach (var chunkMesh in _lodChunkMeshes[0].Values)
            visibleChunks.Add((0, chunkMesh.Index));

        for (int lod = 1; lod <= _maxLodLevel; lod++)
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

    private void ApplyVisibilityChanges(HashSet<(int, Vector3I)> visibleChunks)
    {
        for (int i = 0; i <= _maxLodLevel; i++)
            foreach (var chunkMesh in _lodChunkMeshes[i].Values)
            {
                if (!visibleChunks.Contains((i, chunkMesh.Index)))
                    HideChunkMesh(chunkMesh);
                else
                    RenderChunkMesh(chunkMesh);
            }
    }

    public void UpdateVisibility(Chunk chunk)
    {
        if (chunk.Lod != 0)
            return;

        // Calculate distance to player
        if (!_lodChunkMeshes[0].TryGetValue(chunk.Index, out var chunkMesh))
            return;

        // Check if any parent LOD chunk should be visible instead
        bool shouldBeHidden = false;

        // Check each LOD level above this chunk
        for (int lod = 1; lod <= _maxLodLevel; lod++)
        {
            Vector3I parentIndex = CalculateLodParentIndex(chunk.Index, lod);

            // If the parent LOD chunk exists and should be visible
            if (_lodChunkMeshes[lod].TryGetValue(parentIndex, out var parentChunkMesh))
            {
                double parentDistance = parentChunkMesh.CenterPosition.DistanceTo(_playerPosition);
                int parentAppropriateLevel = DetermineLodLevel(parentDistance);

                if (parentAppropriateLevel >= lod)
                {
                    // Parent LOD should be visible, so this chunk should be hidden
                    shouldBeHidden = true;
                    RenderChunkMesh(parentChunkMesh);
                }
            }
        }

        // Update this chunk's visibility
        if (shouldBeHidden)
            HideChunkMesh(chunkMesh);
        else
            RenderChunkMesh(chunkMesh);
    }


    /// <summary>
    /// Renders the chunk mesh if it is not already rendered.
    /// This method will not update the chunk mesh if already rendered. 
    /// </summary>
    /// <param name="chunkMesh"> The chunk mesh to render.</param>
    private void RenderChunkMesh(ChunkMesh chunkMesh)
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

    #endregion

    #region Collision Management

    private void UpdateCollisionShapesForAll()
    {
        foreach (var chunkMesh in _lodChunkMeshes[0].Values)
            UpdateCollision(chunkMesh);
    }

    /// <summary>
    /// This method will not update the collision shape if already created. 
    /// </summary>
    private void UpdateCollision(ChunkMesh chunkMesh)
    {
        if (chunkMesh.Lod != 0)
            return;

        var physicsDistance = Core.Instance.Settings.PhysicsDistance;
        bool shouldHaveCollision = chunkMesh.Index.DistanceTo(_playerChunkIndex) <= physicsDistance;
        bool shouldRemoveCollision = chunkMesh.Index.DistanceTo(_playerChunkIndex) >= physicsDistance + 1;

        // Add collision if needed and not already present
        if (shouldHaveCollision && chunkMesh.State == ChunkMeshState.Rendered && chunkMesh.CollisionBody == null)
        {
            var collisionBody = _collisionBodyPool.Get();
            collisionBody.Position = chunkMesh.Position;

            var collisionShape = collisionBody.GetChild<CollisionShape3D>(0);
            collisionShape.Shape = chunkMesh.Mesh?.CreateTrimeshShape();

            chunkMesh.CollisionBody = collisionBody;
            chunkMesh.CollisionShape = collisionShape;
        }
        // Remove collision if it's out of range but has collision
        else if (shouldRemoveCollision && chunkMesh.CollisionBody != null)
        {
            _collisionBodyPool.Release(chunkMesh.CollisionBody);
            chunkMesh.CollisionBody = null;
            chunkMesh.CollisionShape = null;
        }
    }

    private void UpdateCollisionShapesDeferred()
    {
        CallDeferred(nameof(UpdateCollisionShapesForAll));
    }

    #endregion

    #region Cleanup

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

    #endregion
}