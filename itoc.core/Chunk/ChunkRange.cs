using Godot;

namespace ITOC.Core;

public class ChunkRange
{
    public Vector3 Center { get; set; }

    public Vector3I CenterIndex => World.WorldPositionToChunkIndex(Center);

    /// <summary>
    /// The radius in chunks for chunk column loading.
    /// </summary>
    public int ChunkColumnRadius { get; }

    /// <summary>
    /// The radius in chunks for chunk loading.
    /// </summary>
    public int ChunkRadius { get; }

    public ChunkRange(int chunkColumnRadius, int chunkRadius)
    {
        ChunkColumnRadius = chunkColumnRadius;
        ChunkRadius = chunkRadius;
    }

    public IEnumerable<Vector2I> ChunkColumns() => ChunkColumnRange(Center, ChunkColumnRadius);

    public IEnumerable<(Vector2I, int)> ChunkColumnsSorted() =>
        ChunkColumnRangeSorted(Center, ChunkColumnRadius);

    public IEnumerable<Vector3I> Chunks() => Chunks(Center, ChunkRadius);

    public bool ContainsChunkColumnAt(Vector2I index) =>
        InChunkColumnRange(index, CenterIndex, ChunkColumnRadius);

    public bool ContainsChunkColumnAt(Vector2 position) =>
        ContainsChunkColumnAt(World.WorldToChunkIndex(position));

    public bool ContainsChunkColumnAt(Vector3 position) =>
        ContainsChunkColumnAt(World.WorldToChunkIndex(new Vector2(position.X, position.Z)));

    public bool ContainsChunkColumnAt(Vector3I position) =>
        ContainsChunkColumnAt(World.WorldToChunkIndex(new Vector2(position.X, position.Z)));

    public bool ContainsChunkAt(Vector3I position) =>
        InChunkRange(position, CenterIndex, ChunkRadius);

    public bool ContainsChunkAt(Vector3 position) =>
        InChunkRange(World.WorldPositionToChunkIndex(position), CenterIndex, ChunkRadius);

    #region Static Methods

    public static IEnumerable<Vector2I> ChunkColumnRange(Vector2 center, int radius)
    {
        var centerIndex = World.WorldToChunkIndex(center);
        for (var x = -radius; x <= radius; x++)
        for (var y = -radius; y <= radius; y++)
            if ((x * x + y * y) <= radius * radius)
                yield return new Vector2I(centerIndex.X + x, centerIndex.Y + y);
    }

    /// <returns>
    /// Sorted list of chunk column indices and their squared distances from the center.
    /// The list is sorted by distance, with the closest first.
    /// </returns>
    public static IEnumerable<(Vector2I, int)> ChunkColumnRangeSorted(Vector2 center, int radius)
    {
        var centerIndex = World.WorldToChunkIndex(center);
        var positionDistPairs = new List<(Vector2I, int)>();

        for (var x = -radius; x <= radius; x++)
        for (var y = -radius; y <= radius; y++)
        {
            var distanceSquared = x * x + y * y;
            if (distanceSquared <= radius * radius)
            {
                var chunkIndex = new Vector2I(centerIndex.X + x, centerIndex.Y + y);
                positionDistPairs.Add((chunkIndex, distanceSquared));
            }
        }

        positionDistPairs.Sort((a, b) => a.Item2.CompareTo(b.Item2));
        return positionDistPairs;
    }

    public static IEnumerable<Vector2I> ChunkColumnRange(Vector3 center, int radius)
    {
        var centerIndex = World.WorldPositionToChunkIndex(center);
        for (var x = -radius; x <= radius; x++)
        for (var y = -radius; y <= radius; y++)
            if ((x * x + y * y) <= radius * radius)
                yield return new Vector2I(centerIndex.X + x, centerIndex.Z + y);
    }

    /// <returns>
    /// Sorted list of chunk column indices and their squared distances from the center.
    /// The list is sorted by distance, with the closest first.
    /// </returns>
    public static IEnumerable<(Vector2I, int)> ChunkColumnRangeSorted(Vector3 center, int radius)
    {
        var centerIndex = World.WorldPositionToChunkIndex(center);
        var positionDistPairs = new List<(Vector2I, int)>();

        for (var x = -radius; x <= radius; x++)
        for (var y = -radius; y <= radius; y++)
        {
            var distanceSquared = x * x + y * y;
            if (distanceSquared <= radius * radius)
            {
                var chunkIndex = new Vector2I(centerIndex.X + x, centerIndex.Z + y);
                positionDistPairs.Add((chunkIndex, distanceSquared));
            }
        }

        positionDistPairs.Sort((a, b) => a.Item2.CompareTo(b.Item2));
        return positionDistPairs;
    }

    public static IEnumerable<Vector3I> Chunks(Vector3 center, int radius)
    {
        var centerIndex = World.WorldPositionToChunkIndex(center);
        if (radius == 0)
            yield return centerIndex;
        else if (radius == 1)
        {
            // 3x3x3 box
            for (var x = -radius; x <= radius; x++)
            for (var y = -radius; y <= radius; y++)
            for (var z = -radius; z <= radius; z++)
                yield return centerIndex + new Vector3I(x, y, z);
        }
        else
        {
            for (var x = -radius; x <= radius; x++)
            for (var y = -radius; y <= radius; y++)
            for (var z = -radius; z <= radius; z++)
                if ((x * x + y * y + z * z) <= radius * radius)
                    yield return centerIndex + new Vector3I(x, y, z);
        }
    }

    /// <returns>
    /// Sorted list of chunk indices and their squared distances from the center.
    /// The list is sorted by distance, with the closest chunks first.
    /// </returns>
    public static IEnumerable<(Vector3I, int)> ChunkRangeSorted(Vector3 center, int radius)
    {
        var centerIndex = World.WorldPositionToChunkIndex(center);
        var positionDistPairs = new List<(Vector3I, int)>();

        if (radius == 0)
        {
            positionDistPairs.Add((centerIndex, 0));
        }
        else if (radius == 1)
        {
            // 3x3x3 box
            for (var x = -radius; x <= radius; x++)
            for (var y = -radius; y <= radius; y++)
            for (var z = -radius; z <= radius; z++)
            {
                var chunkIndex = centerIndex + new Vector3I(x, y, z);
                positionDistPairs.Add((chunkIndex, x * x + y * y + z * z));
            }
        }
        else
        {
            for (var x = -radius; x <= radius; x++)
            for (var y = -radius; y <= radius; y++)
            for (var z = -radius; z <= radius; z++)
            {
                var distanceSquared = x * x + y * y + z * z;
                if (distanceSquared <= radius * radius)
                {
                    var chunkIndex = centerIndex + new Vector3I(x, y, z);
                    positionDistPairs.Add((chunkIndex, distanceSquared));
                }
            }
        }

        positionDistPairs.Sort((a, b) => a.Item2.CompareTo(b.Item2));
        return positionDistPairs;
    }

    public static bool InChunkColumnRange(Vector2I columnIndex, Vector2I columnCenter, int radius)
    {
        var dx = columnIndex.X - columnCenter.X;
        var dy = columnIndex.Y - columnCenter.Y;
        return (dx * dx + dy * dy) <= radius * radius;
    }

    public static bool InChunkColumnRange(Vector2I columnIndex, Vector3I chunkCenter, int radius)
    {
        var dx = columnIndex.X - chunkCenter.X;
        var dy = columnIndex.Y - chunkCenter.Z;
        return (dx * dx + dy * dy) <= radius * radius;
    }

    public static bool InChunkRange(Vector3I positionIndex, Vector3I chunkIndex, int radius)
    {
        var dx = positionIndex.X - chunkIndex.X;
        var dy = positionIndex.Y - chunkIndex.Y;
        var dz = positionIndex.Z - chunkIndex.Z;

        if (radius == 1)
            return MathF.Abs(MathF.Max(dx, MathF.Max(dy, dz))) <= 1;
        else
            return (dx * dx + dy * dy + dz * dz) <= radius * radius;
    }

    #endregion
}
