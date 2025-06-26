using System.Collections.Concurrent;
using Godot;
using ITOC.Core.DelaunatorSharp;
using ITOC.Core.Utils;

namespace ITOC.Core.WorldGeneration.Vanilla;

public partial class VanillaWorldGenerator
{
    /// <summary>
    ///     Creates duplicate points around the edges of a region to ensure continuity in a wrapped world.
    /// </summary>
    /// <param name="points">The original list of points</param>
    /// <param name="rect">The rectangular bounds of the region</param>
    /// <param name="edgeDistance">The distance from the edge within which points should be repeated</param>
    /// <returns>A dictionary mapping duplicated point indices to their original indices</returns>
    private static Dictionary<int, int> RepeatPointsRoundEdges(
        List<Vector2> points,
        Rect2 rect,
        double edgeDistance
    )
    {
        var indexMap = new Dictionary<int, int>();
        var count = points.Count;
        for (var i = 0; i < count; i++)
        {
            var point = points[i];

            if (point.X < rect.Position.X + edgeDistance)
            {
                points.Add(new Vector2(point.X + rect.Size.X, point.Y));
                indexMap[points.Count - 1] = i;
            }

            if (point.X > rect.Position.X + rect.Size.X - edgeDistance)
            {
                points.Add(new Vector2(point.X - rect.Size.X, point.Y));
                indexMap[points.Count - 1] = i;
            }

            if (point.Y < rect.Position.Y + edgeDistance)
            {
                points.Add(new Vector2(point.X, point.Y + rect.Size.Y));
                indexMap[points.Count - 1] = i;
            }

            if (point.Y > rect.Position.Y + rect.Size.Y - edgeDistance)
            {
                points.Add(new Vector2(point.X, point.Y - rect.Size.Y));
                indexMap[points.Count - 1] = i;
            }

            if (
                point.X < rect.Position.X + edgeDistance
                && point.Y < rect.Position.Y + edgeDistance
            )
            {
                points.Add(new Vector2(point.X + rect.Size.X, point.Y + rect.Size.Y));
                indexMap[points.Count - 1] = i;
            }

            if (
                point.X > rect.Position.X + rect.Size.X - edgeDistance
                && point.Y < rect.Position.Y + edgeDistance
            )
            {
                points.Add(new Vector2(point.X - rect.Size.X, point.Y + rect.Size.Y));
                indexMap[points.Count - 1] = i;
            }

            if (
                point.X < rect.Position.X + edgeDistance
                && point.Y > rect.Position.Y + rect.Size.Y - edgeDistance
            )
            {
                points.Add(new Vector2(point.X + rect.Size.X, point.Y - rect.Size.Y));
                indexMap[points.Count - 1] = i;
            }

            if (
                point.X > rect.Position.X + rect.Size.X - edgeDistance
                && point.Y > rect.Position.Y + rect.Size.Y - edgeDistance
            )
            {
                points.Add(new Vector2(point.X - rect.Size.X, point.Y - rect.Size.Y));
                indexMap[points.Count - 1] = i;
            }
        }

        return indexMap;
    }

    /// <summary>
    ///     Transforms a position to ensure it lies within the world's bounds.
    /// </summary>
    /// <param name="position">The position to normalize</param>
    /// <returns>The position wrapped within the world bounds</returns>
    public Vector2 UniformPosition(Vector2 position)
    {
        position -= Settings.Bounds.Position;
        return new Vector2(
                Mathf.PosMod(position.X, Settings.Bounds.Size.X),
                Mathf.PosMod(position.Y, Settings.Bounds.Size.Y)
            ) + Settings.Bounds.Position;
    }

    /// <summary>
    ///     Calculates the shortest distance between two points in a wrapped world.
    /// </summary>
    /// <returns>The distance between the two points, accounting for world wrapping</returns>
    public double UniformDistance(Vector2 pos1, Vector2 pos2)
    {
        var dx = Mathf.Abs(pos1.X - pos2.X);
        var dy = Mathf.Abs(pos1.Y - pos2.Y);

        if (dx > Settings.Bounds.Size.X / 2)
            dx = Settings.Bounds.Size.X - dx;

        if (dy > Settings.Bounds.Size.Y / 2)
            dy = Settings.Bounds.Size.Y - dy;

        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    ///     Gets the indices of cells that are neighbors to the specified cell.
    /// </summary>
    /// <param name="cell">The cell to find neighbors for</param>
    /// <returns>An enumerable of neighbor cell indices</returns>
    public IEnumerable<int> GetNeighborCellIndices(CellData cell) =>
        GetNeighborCellIndices(cell.Index);

    /// <summary>
    ///     Gets the indices of cells that are neighbors to the cell with the specified index.
    /// </summary>
    /// <param name="index">The index of the cell to find neighbors for</param>
    /// <returns>An enumerable of neighbor cell indices</returns>
    public IEnumerable<int> GetNeighborCellIndices(int index)
    {
        foreach (
            var i in _delaunator.EdgesAroundPoint(
                Delaunator.PreviousHalfedge(_triangleIndicesMap[index])
            )
        )
        {
            var neighborIndex = _delaunator.Triangles[i];

            if (_edgePointsMap.TryGetValue(neighborIndex, out var value))
                neighborIndex = value;

            yield return neighborIndex;
        }
    }

    /// <summary>
    ///     Gets the cells that are neighbors to the specified cell.
    /// </summary>
    /// <param name="cell">The cell to find neighbors for</param>
    /// <returns>An enumerable of neighbor cells</returns>
    public IEnumerable<CellData> GetNeighborCells(CellData cell)
    {
        foreach (var i in GetNeighborCellIndices(cell))
            yield return _cellDatas[i];
    }

    /// <summary>
    ///     Gets the cells that are neighbors to the cell with the specified index.
    /// </summary>
    /// <param name="index">The index of the cell to find neighbors for</param>
    /// <returns>An enumerable of neighbor cells</returns>
    public IEnumerable<CellData> GetNeighborCells(int index)
    {
        foreach (var i in GetNeighborCellIndices(index))
            yield return _cellDatas[i];
    }

    /// <summary>
    ///     Converts a position to a latitude value in degrees.
    /// </summary>
    /// <param name="position">The position to convert</param>
    /// <returns>The latitude in degrees, ranging from -90 to 90</returns>
    public double GetLatitude(Vector2 position)
    {
        var normalizedPos =
            (position - Settings.WorldCenter) / Settings.Bounds.Size + Vector2.One / 2;
        return -Mathf.Lerp(-90, 90, normalizedPos.Y);
    }

    /// <summary>
    ///     Converts a position to a longitude value in degrees.
    /// </summary>
    /// <param name="position">The position to convert</param>
    /// <returns>The longitude in degrees, ranging from -180 to 180</returns>
    public double GetLongitude(Vector2 position)
    {
        var normalizedPos =
            (position - Settings.WorldCenter) / Settings.Bounds.Size + Vector2.One / 2;
        return Mathf.Lerp(-180, 180, normalizedPos.X);
    }

    /// <summary>
    ///     Finds cell data near a specified position.
    /// </summary>
    /// <param name="x">The x-coordinate</param>
    /// <param name="y">The y-coordinate</param>
    /// <param name="numNeighbors">The number of nearby cells to find</param>
    /// <returns>An enumerable of the nearest cell data</returns>
    public IEnumerable<CellData> GetCellDatasNearby(double x, double y, int numNeighbors = 1)
    {
        if (State != WorldGenerationState.Ready)
            throw new InvalidOperationException("Cell datas are not initialized yet.");

        var mappedX = 2 * Mathf.Pi * x / Settings.Bounds.Size.X;
        var mappedPosition = new[]
        {
            Mathf.Cos(mappedX) * Settings.Bounds.Size.X * 0.5 / Mathf.Pi,
            Mathf.Sin(mappedX) * Settings.Bounds.Size.X * 0.5 / Mathf.Pi,
            y,
        };

        var results = _cellDatasKdTree.NearestNeighbors(mappedPosition, numNeighbors);

        foreach (var data in results)
            yield return _cellDatas[data.Item2];
    }

    /// <summary>
    ///     Gets cell data near a specified position.
    /// </summary>
    /// <param name="pos">The position to search near</param>
    /// <param name="numNeighbors">The number of nearby cells to find</param>
    /// <returns>An enumerable of the nearest cell data</returns>
    public IEnumerable<CellData> GetCellDatasNearby(Vector2 pos, int numNeighbors = 1) =>
        GetCellDatasNearby(pos.X, pos.Y, numNeighbors);

    /// <summary>
    ///     Finds the triangle that contains a specified point and calculates its barycentric coordinates.
    /// </summary>
    /// <param name="point">The point to locate</param>
    /// <param name="barycentricPos">The barycentric coordinates of the point within the triangle</param>
    /// <returns>A tuple of the three vertex indices that form the triangle</returns>
    public (int, int, int) GetTriangleContainingPoint(Vector2 point, out Vector3 barycentricPos)
    {
        var mappedX = 2 * Mathf.Pi * point.X / Settings.Bounds.Size.X;
        var p = new[]
        {
            Mathf.Cos(mappedX) * Settings.Bounds.Size.X * 0.5 / Mathf.Pi,
            Mathf.Sin(mappedX) * Settings.Bounds.Size.X * 0.5 / Mathf.Pi,
            point.Y,
        };

        var nearestNeighbors = _cellDatasKdTree.NearestNeighbors(p, 2);

        foreach (var nearestNeighbor in nearestNeighbors)
        {
            var nearestId = nearestNeighbor.Item2;
            foreach (
                var i in _delaunator.EdgesAroundPoint(
                    Delaunator.PreviousHalfedge(_triangleIndicesMap[nearestId])
                )
            )
            {
                var triangleIndex = Delaunator.TriangleOfEdge(i);
                var points = _delaunator.PointsOfTriangle(triangleIndex).ToArray();
                if (
                    GeometryUtils.IsPointInTriangle(
                        point,
                        _points[points[0]],
                        _points[points[1]],
                        _points[points[2]],
                        out barycentricPos
                    )
                )
                    return (points[0], points[1], points[2]);
            }
        }

        throw new InvalidOperationException("No triangle found containing the point.");
    }

    /// <summary>
    ///     Finds the triangle that contains a specified point.
    /// </summary>
    /// <param name="point">The point to locate</param>
    /// <returns>A tuple of the three vertex indices that form the triangle</returns>
    public (int, int, int) GetTriangleContainingPoint(Vector2 point) =>
        GetTriangleContainingPoint(point, out _);

    /// <summary>
    ///     Finds the triangle that contains a specified point and calculates its barycentric coordinates.
    /// </summary>
    /// <param name="x">The x-coordinate of the point</param>
    /// <param name="y">The y-coordinate of the point</param>
    /// <param name="barycentricPos">The barycentric coordinates of the point within the triangle</param>
    /// <returns>A tuple of the three vertex indices that form the triangle</returns>
    public (int, int, int) GetTriangleContainingPoint(
        double x,
        double y,
        out Vector3 barycentricPos
    ) => GetTriangleContainingPoint(new Vector2(x, y), out barycentricPos);

    /// <summary>
    ///     Finds the triangle that contains a specified point.
    /// </summary>
    /// <param name="x">The x-coordinate of the point</param>
    /// <param name="y">
    ///     The y-coordinate of the point</returns>
    /// <returns>A tuple of the three vertex indices that form the triangle</returns>
    public (int, int, int) GetTriangleContainingPoint(double x, double y) =>
        GetTriangleContainingPoint(new Vector2(x, y), out _);

    public Vector2 Warp(Vector2 point) => Warp(point, _domainWarpPattern);

    #region Loop subdivision

    private readonly ConcurrentDictionary<(int, int), Vector2> _edgeMidpoints = new();
    private readonly ConcurrentDictionary<(int, int), double> _edgeMidpointHeights = new();
    private readonly ConcurrentDictionary<int, double> _adjustedVertexHeights = new();

    private double GetAdjustedVertexHeight(int vertexIndex) =>
        _adjustedVertexHeights.GetOrAdd(
            vertexIndex,
            index =>
            {
                // Get neighbors of this vertex to calculate the adjusted height
                var neighbors = GetNeighborCellIndices(index).ToList();
                var n = neighbors.Count;

                if (n <= 2)
                    return CellDatas[index].Height;

                // Loop subdivision weight formula: (1-n*beta) * original + beta * sum(neighbors)
                // where beta = 1/n * (5/8 - (3/8 + 1/4 * cos(2*PI/n))^2)
                double beta;
                if (n == 3)
                    beta = 3.0 / 16.0;
                else
                    beta = 3.0 / (8.0 * n);

                // Calculate the sum of neighbor heights
                double neighborSum = 0;
                foreach (var neighbor in neighbors)
                    neighborSum += CellDatas[neighbor].Height;

                // Calculate the adjusted height using Loop formula
                var originalHeight = CellDatas[index].Height;
                return (1 - n * beta) * originalHeight + beta * neighborSum;
            }
        );

    private (Vector2, double) GetOrCreateEdgeMidpoint(int i, int j)
    {
        // Ensure i < j for consistent dictionary keys
        if (i > j)
            (i, j) = (j, i);

        var key = (i, j);

        // Attempt to get existing midpoint
        if (
            _edgeMidpoints.TryGetValue(key, out var midpoint)
            && _edgeMidpointHeights.TryGetValue(key, out var height)
        )
            return (midpoint, height);

        // Calculate the midpoint position and height using Loop subdivision rules
        var newMidpoint = (_points[i] + _points[j]) * 0.5f;
        var newHeight = (CellDatas[i].Height + CellDatas[j].Height) * 0.5;

        // Use GetOrAdd to ensure thread-safety for both dictionaries
        midpoint = _edgeMidpoints.GetOrAdd(key, newMidpoint);
        height = _edgeMidpointHeights.GetOrAdd(key, newHeight);

        return (midpoint, height);
    }

    public (Vector2[], double[]) SubdivideTriangle(int i0, int i1, int i2)
    {
        // Get original triangle vertices and heights
        var p0 = _points[i0];
        var p1 = _points[i1];
        var p2 = _points[i2];

        var h0 = GetAdjustedVertexHeight(i0);
        var h1 = GetAdjustedVertexHeight(i1);
        var h2 = GetAdjustedVertexHeight(i2);

        // Get or create edge midpoints
        var (e01, h01) = GetOrCreateEdgeMidpoint(i0, i1);
        var (e12, h12) = GetOrCreateEdgeMidpoint(i1, i2);
        var (e20, h20) = GetOrCreateEdgeMidpoint(i2, i0);

        // Return all points and heights of the subdivided triangle
        // Original vertices + edge midpoints
        return ([p0, p1, p2, e01, e12, e20], [h0, h1, h2, h01, h12, h20]);
    }

    public (Vector2, Vector2, Vector2, double, double, double) GetSubdividedTriangleContainingPoint(
        Vector2 point,
        int i0,
        int i1,
        int i2
    )
    {
        var (points, heights) = SubdivideTriangle(i0, i1, i2);

        // Original vertices
        var p0 = points[0];
        var p1 = points[1];
        var p2 = points[2];

        // Edge midpoints
        var e01 = points[3];
        var e12 = points[4];
        var e20 = points[5];

        // Heights
        var h0 = heights[0];
        var h1 = heights[1];
        var h2 = heights[2];
        var h01 = heights[3];
        var h12 = heights[4];
        var h20 = heights[5];

        // Check which of the four subdivided triangles contains the point
        if (GeometryUtils.IsPointInTriangle(point, p0, e01, e20))
            return (p0, e01, e20, h0, h01, h20);

        if (GeometryUtils.IsPointInTriangle(point, e01, p1, e12))
            return (e01, p1, e12, h01, h1, h12);

        if (GeometryUtils.IsPointInTriangle(point, e20, e12, p2))
            return (e20, e12, p2, h20, h12, h2);

        if (GeometryUtils.IsPointInTriangle(point, e01, e12, e20))
            return (e01, e12, e20, h01, h12, h20);

        // Fallback - should not happen if the point is in the original triangle
        GD.PrintErr("Point is not in any subdivided triangle.");
        return (p0, p1, p2, h0, h1, h2);
    }

    #endregion
}
