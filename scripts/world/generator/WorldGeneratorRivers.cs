// Depracated.
/*

using Godot;
using System.Collections.Generic;
using System.Linq;

namespace WorldGenerator;

public class RiverSegment
{
    public int ID;
    public float Length;
    public float Width;
    public List<Vector2> Path = new();
}

public partial class WorldGenerator
{
    private Dictionary<int, RiverSegment> _rivers;
    private int _riverIdCounter;

    public IReadOnlyDictionary<int, RiverSegment> Rivers { get => _rivers; }

    private void CalculateWaterFlux()
    {
        ReportProgress("Calculating water flux");
        _rivers = [];
        var orderedCells = _cellDatas.Values
            .Where(c => c.PlateType != PlateType.Oceans)
            .OrderByDescending(c => c.Uplift)
            .ToList();

        foreach (var cell in orderedCells)
        {
            var neighbors = GetNeighborCellIndices(cell);
            var lowest = neighbors.OrderBy(n => _cellDatas[n].Uplift).First();
            if (_cellDatas[lowest].Uplift < 0)
                continue;

            _cellDatas[lowest].Flux += 1f + cell.Flux;
            cell.Flux = 0;

            if (_cellDatas[lowest].Flux > 3.0f) UpdateRiverPath(cell, _cellDatas[lowest]);
        }
    }

    private void UpdateRiverPath(CellData from, CellData to)
    {
        if (from.River == null)
        {
            var river = new RiverSegment { ID = _riverIdCounter++ };
            river.Path.Add(_points[from.Cell.Index]);
            _rivers[river.ID] = river;
            from.River = river;
        }

        var riverSeg = from.River;
        riverSeg.Path.Add(_points[to.Cell.Index]);
        to.River = riverSeg;

        // Add meandering
        if (riverSeg.Path.Count >= 2)
        {
            var last = riverSeg.Path[^2];
            var current = riverSeg.Path[^1];
            var dir = (current - last).Normalized();
            var perpendicular = new Vector2(-dir.Y, dir.X);

            // Add jitter to create meanders
            var jitter = perpendicular * _rng.RandfRange(-0.4f, 0.4f);
            riverSeg.Path[^1] += jitter;
        }

        const float baseWidth = 1.0f;
        const float widthGrowth = 0.2f;

        riverSeg.Width = baseWidth + widthGrowth * riverSeg.Path.Count;
    }
}
*/