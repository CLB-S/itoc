using System;
using System.Collections.Generic;
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
        foreach (var i in _delaunator.EdgesAroundPoint(Delaunator.PreviousHalfedge(_cellDatas[index].TriangleIndex)))
        {
            var neighborIndex = _delaunator.Triangles[i];

            if (_edgePointsMap.ContainsKey(neighborIndex))
                neighborIndex = _edgePointsMap[neighborIndex];

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

    public IEnumerable<CellData> FindCellDatasNearby(Vector2 pos, int numNeighbors = 1)
    {
        return FindCellDatasNearby(pos.X, pos.Y, numNeighbors);
    }
}