using System;
using System.Collections.Generic;
using Godot;

namespace ITOC.Libs.NodePool;

/// <summary>
/// A generic object pool implementation for Godot nodes.
/// Allows efficient reuse of node instances to reduce garbage collection and improve performance.
/// </summary>
/// <typeparam name="T">The type of Node to pool</typeparam>
public class NodePool<T>
    where T : Node
{
    private readonly PackedScene _scene;
    private readonly Node _parent;
    private readonly Queue<T> _inactiveNodes = new();
    private readonly HashSet<T> _activeNodes = new();
    private readonly int _initialSize;
    private readonly int _maxSize;
    private readonly bool _autoExpand;
    private readonly Action<T> _resetAction;
    private readonly Action<T> _initializeAction;

    /// <summary>
    /// Gets the number of active nodes currently in use
    /// </summary>
    public int ActiveCount => _activeNodes.Count;

    /// <summary>
    /// Gets the number of inactive nodes available for reuse
    /// </summary>
    public int InactiveCount => _inactiveNodes.Count;

    /// <summary>
    /// Gets the total number of nodes managed by this pool
    /// </summary>
    public int TotalCount => ActiveCount + InactiveCount;

    protected virtual void SetVisibility(T node, bool visible)
    {
        if (node is CanvasItem canvasItem)
            canvasItem.Visible = visible;
        else if (node is Node3D node3D)
            node3D.Visible = visible;
    }

    /// <summary>
    /// Creates a new NodePool
    /// </summary>
    /// <param name="scene">The scene to instantiate</param>
    /// <param name="parent">The parent node to attach instances to</param>
    /// <param name="initialSize">Initial pool size</param>
    /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
    /// <param name="autoExpand">Whether to automatically create new instances when the pool is empty</param>
    /// <param name="resetAction">Optional action to reset a node before returning it to the pool</param>
    /// <param name="initializeAction">Optional action to initialize a node when it's retrieved from the pool</param>
    public NodePool(
        PackedScene scene,
        Node parent,
        int initialSize = 10,
        int maxSize = 0,
        bool autoExpand = true,
        Action<T> resetAction = null,
        Action<T> initializeAction = null)
    {
        _scene = scene ?? throw new ArgumentNullException(nameof(scene));
        _parent = parent ?? throw new ArgumentNullException(nameof(parent));
        _initialSize = Math.Max(0, initialSize);
        _maxSize = maxSize > 0 ? maxSize : 0;
        _autoExpand = autoExpand;
        _resetAction = resetAction;
        _initializeAction = initializeAction;

        PrewarmPool(_initialSize);
    }

    /// <summary>
    /// Creates a new NodePool with direct node instances instead of a scene
    /// </summary>
    /// <param name="nodeFactory">Function to create new node instances</param>
    /// <param name="parent">The parent node to attach instances to</param>
    /// <param name="initialSize">Initial pool size</param>
    /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
    /// <param name="autoExpand">Whether to automatically create new instances when the pool is empty</param>
    /// <param name="resetAction">Optional action to reset a node before returning it to the pool</param>
    /// <param name="initializeAction">Optional action to initialize a node when it's retrieved from the pool</param>
    public NodePool(
        Func<T> nodeFactory,
        Node parent,
        int initialSize = 10,
        int maxSize = 0,
        bool autoExpand = true,
        Action<T> resetAction = null,
        Action<T> initializeAction = null)
    {
        ArgumentNullException.ThrowIfNull(nodeFactory);
        _parent = parent ?? throw new ArgumentNullException(nameof(parent));
        _initialSize = Math.Max(0, initialSize);
        _maxSize = maxSize > 0 ? maxSize : 0;
        _autoExpand = autoExpand;
        _resetAction = resetAction;
        _initializeAction = initializeAction;

        // Create initial nodes using the factory
        for (int i = 0; i < _initialSize; i++)
        {
            var node = nodeFactory();
            PrepareNodeForPool(node);
            _inactiveNodes.Enqueue(node);
        }
    }

    /// <summary>
    /// Pre-initializes the pool with the specified number of instances
    /// </summary>
    /// <param name="count">Number of instances to create</param>
    public void PrewarmPool(int count)
    {
        if (_scene == null) return;

        for (int i = 0; i < count; i++)
        {
            if (_maxSize > 0 && TotalCount >= _maxSize) break;

            var instance = CreateNewInstance();
            _inactiveNodes.Enqueue(instance);
        }
    }

    /// <summary>
    /// Gets a node from the pool or creates a new one if needed
    /// </summary>
    /// <returns>An instance of T ready for use</returns>
    public T Get()
    {
        T node;

        if (_inactiveNodes.Count > 0)
            node = _inactiveNodes.Dequeue();
        else if (_autoExpand && (_maxSize <= 0 || TotalCount < _maxSize))
            node = CreateNewInstance();
        else
            throw new InvalidOperationException("Node pool is empty and cannot expand.");

        // Add to active set
        _activeNodes.Add(node);

        // Make node visible
        SetVisibility(node, true);

        // Call initialization action if provided
        _initializeAction?.Invoke(node);

        return node;
    }

    /// <summary>
    /// Returns a node to the pool for later reuse
    /// </summary>
    /// <param name="node">The node to return to the pool</param>
    public void Release(T node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (!_activeNodes.Contains(node))
        {
            GD.PushWarning($"Trying to release a node that is not managed by this pool: {node.Name}");
            return;
        }

        // Remove from active set
        _activeNodes.Remove(node);

        // Reset the node if a reset action was provided
        _resetAction?.Invoke(node);

        // Hide the node
        SetVisibility(node, false);

        // Add back to inactive queue
        _inactiveNodes.Enqueue(node);
    }

    /// <summary>
    /// Releases all active nodes back to the pool
    /// </summary>
    public void ReleaseAll()
    {
        var nodesToRelease = new List<T>(_activeNodes);
        foreach (var node in nodesToRelease)
            Release(node);
    }

    /// <summary>
    /// Clear all inactive nodes from the pool
    /// </summary>
    public void ClearInactive()
    {
        while (_inactiveNodes.Count > 0)
        {
            var node = _inactiveNodes.Dequeue();
            node.QueueFree();
        }
    }


    /// <summary>
    /// Clears the pool, removing all nodes from the scene tree
    /// </summary>
    public void Clear()
    {
        // Clear active nodes
        foreach (var node in _activeNodes)
            node.QueueFree();
        _activeNodes.Clear();

        // Clear inactive nodes
        ClearInactive();
    }

    /// <summary>
    /// Resizes the pool to the specified capacity
    /// </summary>
    /// <param name="targetSize">The desired capacity of inactive nodes</param>
    public void Resize(int targetSize)
    {
        // Remove excess nodes if we have too many
        while (_inactiveNodes.Count > targetSize)
        {
            var node = _inactiveNodes.Dequeue();
            node.QueueFree();
        }

        // Add nodes if we don't have enough
        int nodesToAdd = targetSize - _inactiveNodes.Count;
        PrewarmPool(nodesToAdd);
    }

    private T CreateNewInstance()
    {
        if (_scene == null) throw new InvalidOperationException("Cannot create instance: no scene provided.");

        var instance = _scene.Instantiate<T>();
        PrepareNodeForPool(instance);
        return instance;
    }

    private void PrepareNodeForPool(T node)
    {
        _parent.AddChild(node);
        SetVisibility(node, false);
    }
}
