using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

using DelaunatorSharp;


public partial class WorldTest : Node2D
{
    private List<Vector2> _points;
    private Delaunator _delaunator;
    private IEnumerable<VoronoiCell> _cells;
    private IEnumerable<Edge> _edges;

    private float _minimumDistance = 20f;
    [Export]
    public float MinimumDistance
    {
        get { return _minimumDistance; }
        set
        {
            _minimumDistance = value;
            Construct();
        }
    }

    [Export] public Rect2 Rect = new Rect2(-500, -400, 1000, 800);


    private void Construct()
    {
        _points = FastPoissonDiskSampling.Sampling(Rect.Position, Rect.End, MinimumDistance);
        _delaunator = new Delaunator(_points.ToArray());
        _edges = _delaunator.GetVoronoiEdgesBasedOnCentroids();
        _cells = _delaunator.GetVoronoiCellsBasedOnCentroids();
        QueueRedraw();
    }

    public override void _Ready()
    {
        base._Ready();
        Construct();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
    }

    public override void _Draw()
    {

        if (_cells != null)
        {
            foreach (var cell in _cells)
            {
                if (cell.Points.Length >= 3)
                    DrawColoredPolygon(cell.Points, ColorUtils.GetRandomColorHSV());
            }
        }

        if (_edges != null)
        {
            var lines = new Vector2[_edges.Count() * 2];
            int i = 0;
            foreach (var edge in _edges)
            {
                lines[i++] = edge.P;
                lines[i++] = edge.Q;
            }

            DrawMultiline(lines, Colors.White);
        }

        foreach (var pos in _points)
        {
            DrawCircle(pos, 2f, Colors.White);
        }

        DrawRect(Rect, Colors.Red, false);

    }
}
