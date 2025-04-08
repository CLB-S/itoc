using Godot;
using System.Collections.Generic;
using DelaunatorSharp;

namespace WorldGenerator;

public partial class WorldGenerator
{

    private static Dictionary<int, int> RepeatPointsRoundEdges(List<Vector2> points, Rect2 rect, float edgeDistance)
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

    private IEnumerable<int> GetNeighborCells(CellData cell)
    {
        return GetNeighborCells(cell.Cell.Index);
    }

    private IEnumerable<int> GetNeighborCells(int index)
    {
        foreach (var i in _delaunator.EdgesAroundPoint(Delaunator.PreviousHalfedge(_cellDatas[index].TriangleIndex)))
        {
            var neighborIndex = _delaunator.Triangles[i];

            if (_edgePointsMap.ContainsKey(neighborIndex))
                neighborIndex = _edgePointsMap[neighborIndex];

            yield return neighborIndex;
        }
    }
}
