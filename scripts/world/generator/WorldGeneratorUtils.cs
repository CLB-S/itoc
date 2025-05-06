using System;
using System.Collections.Generic;
using System.Linq;
using DelaunatorSharp;
using Godot;

namespace WorldGenerator;

public partial class WorldGenerator
{
    private static Dictionary<int, int> RepeatPointsRoundEdges(List<Vector2> points, Rect2 rect, double edgeDistance)
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

            if (point.X < rect.Position.X + edgeDistance && point.Y < rect.Position.Y + edgeDistance)
            {
                points.Add(new Vector2(point.X + rect.Size.X, point.Y + rect.Size.Y));
                indexMap[points.Count - 1] = i;
            }

            if (point.X > rect.Position.X + rect.Size.X - edgeDistance && point.Y < rect.Position.Y + edgeDistance)
            {
                points.Add(new Vector2(point.X - rect.Size.X, point.Y + rect.Size.Y));
                indexMap[points.Count - 1] = i;
            }

            if (point.X < rect.Position.X + edgeDistance && point.Y > rect.Position.Y + rect.Size.Y - edgeDistance)
            {
                points.Add(new Vector2(point.X + rect.Size.X, point.Y - rect.Size.Y));
                indexMap[points.Count - 1] = i;
            }

            if (point.X > rect.Position.X + rect.Size.X - edgeDistance &&
                point.Y > rect.Position.Y + rect.Size.Y - edgeDistance)
            {
                points.Add(new Vector2(point.X - rect.Size.X, point.Y - rect.Size.Y));
                indexMap[points.Count - 1] = i;
            }
        }

        return indexMap;
    }

    public Vector2 UniformPosition(Vector2 position)
    {
        position -= Settings.Bounds.Position;
        return new Vector2(
            Mathf.PosMod(position.X, Settings.Bounds.Size.X),
            Mathf.PosMod(position.Y, Settings.Bounds.Size.Y)
        ) + Settings.Bounds.Position;
    }

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

    public IEnumerable<int> GetNeighborCellIndices(CellData cell)
    {
        return GetNeighborCellIndices(cell.Index);
    }

    public IEnumerable<int> GetNeighborCellIndices(int index)
    {
        foreach (var i in _delaunator.EdgesAroundPoint(Delaunator.PreviousHalfedge(_triangleIndicesMap[index])))
        {
            var neighborIndex = _delaunator.Triangles[i];

            if (_edgePointsMap.TryGetValue(neighborIndex, out int value))
                neighborIndex = value;

            yield return neighborIndex;
        }
    }

    public IEnumerable<CellData> GetNeighborCells(CellData cell)
    {
        foreach (var i in GetNeighborCellIndices(cell))
            yield return _cellDatas[i];
    }

    public IEnumerable<CellData> GetNeighborCells(int index)
    {
        foreach (var i in GetNeighborCellIndices(index))
            yield return _cellDatas[i];
    }

    public double GetLatitude(Vector2 position)
    {
        var normalizedPos = (position - Settings.WorldCenter) / Settings.Bounds.Size +
            Vector2.One / 2;
        return -Mathf.Lerp(-90, 90, normalizedPos.Y);
    }

    public double GetLongitude(Vector2 position)
    {
        var normalizedPos = (position - Settings.WorldCenter) / Settings.Bounds.Size +
            Vector2.One / 2;
        return Mathf.Lerp(-180, 180, normalizedPos.X);
    }

    public IEnumerable<CellData> FindCellDatasNearby(double x, double y, int numNeighbors = 1)
    {
        if (State != GenerationState.Completed)
            throw new InvalidOperationException("Cell datas are not initialized yet.");

        var mappedX = 2 * Mathf.Pi * x / Settings.Bounds.Size.X;
        var mappedPosition = new[] { Mathf.Cos(mappedX) * Settings.Bounds.Size.X * 0.5 / Mathf.Pi,
                Mathf.Sin(mappedX) * Settings.Bounds.Size.X * 0.5 / Mathf.Pi,
                y };

        var results = _cellDatasKdTree.NearestNeighbors(mappedPosition, numNeighbors);

        foreach (var data in results)
            yield return _cellDatas[data.Item2];
    }

    public IEnumerable<CellData> GetCellDatasNearby(Vector2 pos, int numNeighbors = 1)
    {
        return FindCellDatasNearby(pos.X, pos.Y, numNeighbors);
    }

    public (int, int, int) GetTriangleContainingPoint(Vector2 point, out Vector3 barycentricPos)
    {
        var mappedX = 2 * Mathf.Pi * point.X / Settings.Bounds.Size.X;
        var p = new[] { Mathf.Cos(mappedX) * Settings.Bounds.Size.X * 0.5 / Mathf.Pi,
                Mathf.Sin(mappedX) * Settings.Bounds.Size.X * 0.5 / Mathf.Pi,
                point.Y };

        var nearestNeighbors = _cellDatasKdTree.NearestNeighbors(p, 2);

        foreach (var nearestNeighbor in nearestNeighbors)
        {
            var nearestId = nearestNeighbor.Item2;
            foreach (var i in _delaunator.EdgesAroundPoint(Delaunator.PreviousHalfedge(_triangleIndicesMap[nearestId])))
            {
                var triangleIndex = Delaunator.TriangleOfEdge(i);
                var points = _delaunator.PointsOfTriangle(triangleIndex).ToArray();
                if (GeometryUtils.IsPointInTriangle(point, SamplePoints[points[0]], SamplePoints[points[1]], SamplePoints[points[2]], out barycentricPos))
                    return (points[0], points[1], points[2]);
            }
        }

        throw new InvalidOperationException("No triangle found containing the point.");
    }

    public (int, int, int) GetTriangleContainingPoint(Vector2 point)
    {
        return GetTriangleContainingPoint(point, out _);
    }

    public (int, int, int) GetTriangleContainingPoint(double x, double y, out Vector3 barycentricPos)
    {
        return GetTriangleContainingPoint(new Vector2(x, y), out barycentricPos);
    }

    public (int, int, int) GetTriangleContainingPoint(double x, double y)
    {
        return GetTriangleContainingPoint(new Vector2(x, y), out _);
    }
}