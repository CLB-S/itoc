using Godot;

namespace ITOC.Core.NodePool;

/// <summary>
/// Specialized node pool for Node3D instances with additional features for 3D positioning
/// </summary>
/// <typeparam name="T">The type of Node3D to pool</typeparam>
public class Node3DPool<T> : NodePool<T> where T : Node3D
{
    protected override void SetVisibility(T node, bool visible)
    {
        node.Visible = visible;
    }

    /// <summary>
    /// Creates a new Node3DPool
    /// </summary>
    /// <param name="scene">The scene to instantiate</param>
    /// <param name="parent">The parent node to attach instances to</param>
    /// <param name="initialSize">Initial pool size</param>
    /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
    /// <param name="autoExpand">Whether to automatically create new instances when the pool is empty</param>
    /// <param name="resetAction">Optional action to reset a node before returning it to the pool</param>
    /// <param name="initializeAction">Optional action to initialize a node when it's retrieved from the pool</param>
    public Node3DPool(
        PackedScene scene,
        Node parent,
        int initialSize = 10,
        int maxSize = 0,
        bool autoExpand = true,
        Action<T> resetAction = null,
        Action<T> initializeAction = null)
        : base(scene, parent, initialSize, maxSize, autoExpand, resetAction, initializeAction)
    {
    }

    /// <summary>
    /// Creates a new Node3DPool with direct node instances instead of a scene
    /// </summary>
    /// <param name="nodeFactory">Function to create new node instances</param>
    /// <param name="parent">The parent node to attach instances to</param>
    /// <param name="initialSize">Initial pool size</param>
    /// <param name="maxSize">Maximum pool size (0 for unlimited)</param>
    /// <param name="autoExpand">Whether to automatically create new instances when the pool is empty</param>
    /// <param name="resetAction">Optional action to reset a node before returning it to the pool</param>
    /// <param name="initializeAction">Optional action to initialize a node when it's retrieved from the pool</param>
    public Node3DPool(
        Func<T> nodeFactory,
        Node parent,
        int initialSize = 10,
        int maxSize = 0,
        bool autoExpand = true,
        Action<T> resetAction = null,
        Action<T> initializeAction = null)
        : base(nodeFactory, parent, initialSize, maxSize, autoExpand, resetAction, initializeAction)
    {
    }

    /// <summary>
    /// Gets a node from the pool and positions it at the specified coordinates
    /// </summary>
    /// <param name="position">Position to place the node</param>
    /// <returns>A positioned node instance ready for use</returns>
    public T GetAt(Vector3 position)
    {
        var node = Get();
        node.Position = position;
        return node;
    }

    /// <summary>
    /// Gets a node from the pool and positions and rotates it as specified
    /// </summary>
    /// <param name="position">Position to place the node</param>
    /// <param name="rotation">Rotation in radians as a Vector3 (x, y, z)</param>
    /// <param name="scale">Scale to apply</param>
    /// <returns>A positioned and rotated node instance ready for use</returns>
    public T GetAt(Vector3 position, Vector3? rotation = null, Vector3? scale = null)
    {
        var node = Get();
        node.Position = position;
        if (rotation.HasValue)
            node.Rotation = rotation.Value;
        if (scale.HasValue)
            node.Scale = scale.Value;
        return node;
    }

    /// <summary>
    /// Gets a node from the pool and transforms it with a Transform3D
    /// </summary>
    /// <param name="transform">Transform to apply</param>
    /// <returns>A transformed node instance ready for use</returns>
    public T GetWithTransform(Transform3D transform)
    {
        var node = Get();
        node.GlobalTransform = transform;
        return node;
    }

    /// <summary>
    /// Gets a node from the pool and transforms it as specified
    /// </summary>
    /// <param name="position">Position to place the node</param>
    /// <param name="rotation">Rotation as a Quaternion</param>
    /// <param name="scale">Scale to apply</param>
    /// <returns>A transformed node instance ready for use</returns>
    public T GetAt(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        var node = Get();
        node.Position = position;
        node.Quaternion = rotation;
        node.Scale = scale;
        return node;
    }
}
