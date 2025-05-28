using Godot;

namespace ITOC.Core.NodePool;

/// <summary>
/// Provides utility methods for batch operations on node pools
/// </summary>
public static class NodePoolBatch
{
    /// <summary>
    /// Processes a batch of nodes from a pool
    /// </summary>
    /// <typeparam name="T">The type of node</typeparam>
    /// <param name="pool">The node pool to get nodes from</param>
    /// <param name="count">Number of nodes to process</param>
    /// <param name="processor">Action to perform on each node</param>
    /// <returns>A list of the processed nodes</returns>
    public static List<T> Process<T>(NodePool<T> pool, int count, Action<T, int> processor) where T : Node
    {
        ArgumentNullException.ThrowIfNull(pool);
        ArgumentNullException.ThrowIfNull(processor);
        if (count <= 0) return [];

        var nodes = new List<T>(count);
        for (int i = 0; i < count; i++)
        {
            var node = pool.Get();
            processor(node, i);
            nodes.Add(node);
        }

        return nodes;
    }

    /// <summary>
    /// Processes a batch of 2D nodes from a pool
    /// </summary>
    /// <typeparam name="T">The type of 2D node</typeparam>
    /// <param name="pool">The node pool to get nodes from</param>
    /// <param name="positions">Positions for the nodes</param>
    /// <param name="processor">Optional additional processing for each node</param>
    /// <returns>A list of the processed nodes</returns>
    public static List<T> ProcessAt<T>(Node2DPool<T> pool, IReadOnlyList<Vector2> positions, Action<T, int> processor = null) where T : Node2D
    {
        ArgumentNullException.ThrowIfNull(pool);
        ArgumentNullException.ThrowIfNull(positions);
        if (positions.Count == 0) return [];

        var nodes = new List<T>(positions.Count);
        for (int i = 0; i < positions.Count; i++)
        {
            var node = pool.GetAt(positions[i]);
            processor?.Invoke(node, i);
            nodes.Add(node);
        }

        return nodes;
    }

    /// <summary>
    /// Processes a batch of 3D nodes from a pool
    /// </summary>
    /// <typeparam name="T">The type of 3D node</typeparam>
    /// <param name="pool">The node pool to get nodes from</param>
    /// <param name="positions">Positions for the nodes</param>
    /// <param name="processor">Optional additional processing for each node</param>
    /// <returns>A list of the processed nodes</returns>
    public static List<T> ProcessAt<T>(Node3DPool<T> pool, IReadOnlyList<Vector3> positions, Action<T, int> processor = null) where T : Node3D
    {
        ArgumentNullException.ThrowIfNull(pool);
        ArgumentNullException.ThrowIfNull(positions);
        if (positions.Count == 0) return new List<T>();

        var nodes = new List<T>(positions.Count);

        for (int i = 0; i < positions.Count; i++)
        {
            var node = pool.GetAt(positions[i]);
            processor?.Invoke(node, i);
            nodes.Add(node);
        }

        return nodes;
    }

    /// <summary>
    /// Releases all nodes in the collection back to the pool
    /// </summary>
    /// <typeparam name="T">The type of node</typeparam>
    /// <param name="pool">The node pool to release nodes to</param>
    /// <param name="nodes">The collection of nodes to release</param>
    public static void ReleaseAll<T>(NodePool<T> pool, IEnumerable<T> nodes) where T : Node
    {
        ArgumentNullException.ThrowIfNull(pool);
        ArgumentNullException.ThrowIfNull(nodes);

        foreach (var node in nodes)
            pool.Release(node);
    }
}
