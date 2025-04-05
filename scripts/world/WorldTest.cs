using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

using DelaunatorSharp;

public partial class WorldTest : Node2D
{
    public enum PlateType
    {
        Continent,
        Oceans,
    }

    public class CellData
    {
        public int TriangleIndex;
        public VoronoiCell Cell;
        public Vector2 TectonicMovement;
        public PlateType PlateType;
        public float Altitude = 0f;
        public bool RoundPlateJunction = false;
    }

    public ulong Seed = 233;
    public float MaxTectonicMovement = 10.0f;
    public float MaxAltitude = 2000.0f;

    public float ContinentRatio = 0.6f;

    private List<Vector2> _points;
    private Delaunator _delaunator;

    private Dictionary<int, CellData> _cellDatas;

    private Edge[] _edges;
    private Noise _noise;
    private RandomNumberGenerator _rng;

    private float _minimumDistance = 5f;
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

    [Export] public Rect2 Rect = new Rect2(-500, -500, 1000, 1000);


    private Vector2 UniformPosition(Vector2 position, Rect2 rect)
    {
        position -= rect.Position;
        return new Vector2(
            Mathf.PosMod(position.X, rect.Size.X),
            Mathf.PosMod(position.Y, rect.Size.Y)
        ) + rect.Position;
    }

    private void RepeatPointsRoundEdges(List<Vector2> points, Rect2 rect, float edgeDistance)
    {
        var originalPoints = new List<Vector2>(points);

        foreach (var point in originalPoints)
        {
            if (point.X < rect.Position.X + edgeDistance)
                points.Add(new Vector2(point.X + rect.Size.X, point.Y));

            if (point.X > rect.Position.X + rect.Size.X - edgeDistance)
                points.Add(new Vector2(point.X - rect.Size.X, point.Y));

            if (point.Y < rect.Position.Y + edgeDistance)
                points.Add(new Vector2(point.X, point.Y + rect.Size.Y));

            if (point.Y > rect.Position.Y + rect.Size.Y - edgeDistance)
                points.Add(new Vector2(point.X, point.Y - rect.Size.Y));

            if (point.X < rect.Position.X + edgeDistance && point.Y < rect.Position.Y + edgeDistance)
                points.Add(new Vector2(point.X + rect.Size.X, point.Y + rect.Size.Y));

            if (point.X > rect.Position.X + rect.Size.X - edgeDistance && point.Y < rect.Position.Y + edgeDistance)
                points.Add(new Vector2(point.X - rect.Size.X, point.Y + rect.Size.Y));

            if (point.X < rect.Position.X + edgeDistance && point.Y > rect.Position.Y + rect.Size.Y - edgeDistance)
                points.Add(new Vector2(point.X + rect.Size.X, point.Y - rect.Size.Y));

            if (point.X > rect.Position.X + rect.Size.X - edgeDistance && point.Y > rect.Position.Y + rect.Size.Y - edgeDistance)
                points.Add(new Vector2(point.X - rect.Size.X, point.Y - rect.Size.Y));
        }
    }

    private void Construct()
    {
        _points = FastPoissonDiskSampling.Sampling(Rect.Position, Rect.End, MinimumDistance, _rng);
        RepeatPointsRoundEdges(_points, Rect, 2 * MinimumDistance);
        _delaunator = new Delaunator(_points.ToArray());
        _edges = _delaunator.GetVoronoiEdgesBasedOnCentroids().ToArray();
        var _cells = _delaunator.GetVoronoiCellsBasedOnCentroids().ToArray();
        _cellDatas = new Dictionary<int, CellData>(_cells.Length);
        for (int i = 0; i < _cells.Length; i++)
            _cellDatas[_cells[i].Index] = new CellData() { Cell = _cells[i] };

        for (int i = 0; i < _delaunator.Triangles.Length; i++)
            _cellDatas[_delaunator.Triangles[i]].TriangleIndex = i;

        InitTectonicProperties();
        SetInitialAltitudes();
        QueueRedraw();
    }

    private PlateType RandomPlateType(RandomNumberGenerator rng)
    {
        if (rng.Randf() < 1 - ContinentRatio)
            return PlateType.Oceans;

        return PlateType.Continent;
    }

    private void InitTectonicProperties()
    {
        var rng = new RandomNumberGenerator();

        foreach (var (i, cellData) in _cellDatas)
        {
            var pos = UniformPosition(_delaunator.Points[i], Rect);
            var mappedX = 2 * Mathf.Pi * pos.X / Rect.Size.X;
            var seed = _noise.GetNoise3D(Mathf.Cos(mappedX) * Rect.Size.X * 0.5f / Mathf.Pi, Mathf.Sin(mappedX) * Rect.Size.X * 0.5f / Mathf.Pi, pos.Y).ToString().Hash();
            rng.Seed = seed;
            var r = rng.Randf() * MaxTectonicMovement;
            var phi = rng.Randf() * Mathf.Pi * 2;
            cellData.TectonicMovement = new Vector2(Mathf.Cos(phi), Mathf.Sin(phi)) * r;
            cellData.PlateType = RandomPlateType(rng); // (PlateType)(rng.Randi() % Enum.GetNames(typeof(PlateType)).Length);
        }
    }

    private float LeakyRELU(float x, float k = 0.3f)
    {
        if (x >= 0) return x;
        else return x * k;
    }

    private void SetInitialAltitudes()
    {
        var initialIndices = new List<int>();

        foreach (var edge in _edges)
        {
            var cellP = _cellDatas[_delaunator.Triangles[edge.Index]];
            var cellQ = _cellDatas[_delaunator.Triangles[_delaunator.Halfedges[edge.Index]]];
            if (cellP.TectonicMovement != cellQ.TectonicMovement)
            {
                // [-1, 1]
                var relativeMovement = cellP.TectonicMovement.Dot(-cellQ.TectonicMovement) / MaxTectonicMovement / MaxTectonicMovement;

                if (Mathf.Abs(relativeMovement) < 0.2f)
                    continue;

                cellP.RoundPlateJunction = true;
                cellQ.RoundPlateJunction = true;
                initialIndices.Add(cellP.Cell.Index);
                initialIndices.Add(cellQ.Cell.Index);


                if (cellP.PlateType == PlateType.Continent && cellQ.PlateType == PlateType.Continent)
                {
                    float altitude;
                    if (relativeMovement < 0)
                    {
                        altitude = Mathf.Pow(relativeMovement, 3) / 2f + 0.5f;
                        altitude *= altitude;
                    }
                    else
                        altitude = 1 - 0.75f * (relativeMovement - 1) * (relativeMovement - 1);

                    cellP.Altitude = altitude * MaxAltitude;
                    cellQ.Altitude = altitude * MaxAltitude;
                }
                else if (cellP.PlateType == PlateType.Oceans && cellQ.PlateType == PlateType.Oceans)
                {
                    float altitude = Mathf.Pow(relativeMovement, 3) / 2f + 0.5f;
                    altitude = altitude * altitude * 0.5f - 0.3f;
                    if (relativeMovement > 0)
                        altitude += 0.25f * (1 - (1 - relativeMovement) * (1 - relativeMovement));

                    cellP.Altitude = altitude * MaxAltitude;
                    cellQ.Altitude = altitude * MaxAltitude;
                }
                else if (cellP.PlateType == PlateType.Continent && cellQ.PlateType == PlateType.Oceans)
                {
                    if (relativeMovement < 0)
                    {
                        // cellP.Altitude = -50f * relativeMovement;
                        // cellQ.Altitude = -100f * relativeMovement;
                    }
                    else
                    {
                        cellP.Altitude = 1 - (relativeMovement - 1) * (relativeMovement - 1);

                        float altitude = Mathf.Pow(relativeMovement, 3) / 2f + 0.5f;
                        altitude = altitude * altitude - 0.25f;
                        cellQ.Altitude = altitude;
                    }
                }
                else if (cellP.PlateType == PlateType.Oceans && cellQ.PlateType == PlateType.Continent)
                {
                    if (relativeMovement < 0)
                    {
                        // cellP.Altitude = -100f * relativeMovement;
                        // cellQ.Altitude = -50f * relativeMovement;
                    }
                    else
                    {
                        cellQ.Altitude = 1 - (relativeMovement - 1) * (relativeMovement - 1);

                        float altitude = Mathf.Pow(relativeMovement, 3) / 2f + 0.5f;
                        altitude = altitude * altitude - 0.25f;
                        cellP.Altitude = altitude;
                    }
                }
            }
        }

        PropagateAltitudes(initialIndices, 0.8f);
    }

    public void PropagateAltitudes(IEnumerable<int> initialIndices, float decrement = 0.9f, float sharpness = 0.1f)
    {
        var used = new HashSet<int>();
        var queue = new PriorityQueue<int, float>();

        foreach (var i in initialIndices)
        {
            used.Add(i);
            queue.Enqueue(i, -Mathf.Abs(_cellDatas[i].Altitude));
        }

        while (queue.Count > 0)
        {
            int currentIndex = queue.Dequeue();
            CellData currentCell = _cellDatas[currentIndex];
            float parentHeight = currentCell.Altitude;

            float propagatedHeight = parentHeight * decrement;
            if (Mathf.Abs(propagatedHeight) < 0.01f)
                continue;

            foreach (int i in _delaunator.EdgesAroundPoint(Delaunator.PreviousHalfedge(_cellDatas[currentIndex].TriangleIndex)))
            {
                var neighborIndex = _delaunator.Triangles[i];
                if (!used.Contains(neighborIndex))
                {
                    CellData neighbor = _cellDatas[neighborIndex];
                    float mod = sharpness == 0 ? 1.0f : (1.1f - sharpness) + (_rng.Randf() * sharpness);
                    float heightContribution = propagatedHeight * mod;

                    neighbor.Altitude += heightContribution;

                    used.Add(neighborIndex);
                    queue.Enqueue(neighborIndex, -Mathf.Abs(_cellDatas[neighborIndex].Altitude));
                }
            }
        }

    }

    public override void _Ready()
    {
        base._Ready();
        _rng = new RandomNumberGenerator() { Seed = Seed };
        _noise = new FastNoiseLite()
        {
            Seed = (int)Seed,
            NoiseType = FastNoiseLite.NoiseTypeEnum.Cellular,
            Frequency = 0.007f,
            CellularReturnType = FastNoiseLite.CellularReturnTypeEnum.CellValue,
            DomainWarpEnabled = true,
            DomainWarpAmplitude = 90f,
            DomainWarpFrequency = 0.007f,
            DomainWarpFractalType = FastNoiseLite.DomainWarpFractalTypeEnum.None,
            FractalType = FastNoiseLite.FractalTypeEnum.None,
        };

        Construct();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
    }

    private void DrawArrow(Vector2 start, Vector2 end, Color color, bool twoLineHead = false)
    {
        // the arrow size and flatness
        float arrowSize = (end - start).Length() * 0.4f;
        float flatness = 0.5f;

        // the line starting and ending points
        DrawLine(start, end, color, Mathf.Sqrt(arrowSize));

        // calculate the direction vector
        Vector2 direction = (end - start).Normalized();

        // calculate the side vectors
        Vector2 side1 = new Vector2(-direction.Y, direction.X);
        Vector2 side2 = new Vector2(direction.Y, -direction.X);

        // calculate the T-junction points
        Vector2 e1 = end + side1 * arrowSize * flatness;
        Vector2 e2 = end + side2 * arrowSize * flatness;

        // calculate the arrow edges
        Vector2 p1 = e1 - direction * arrowSize;
        Vector2 p2 = e2 - direction * arrowSize;

        // draw the arrow sides as a polygon
        if (!twoLineHead)
        {
            DrawColoredPolygon([end, p1, p2], color);
        }
        else
        {
            // alternatively, draw the arrow as two lines
            DrawLine(end, p1, color, 2);
            DrawLine(end, p2, color, 2);
        }
    }

    public override void _Draw()
    {

        if (_cellDatas != null)
        {
            foreach (var (i, cellData) in _cellDatas)
            {
                if (cellData.Cell.Points.Length >= 3)
                {
                    var pos = UniformPosition(_delaunator.Points[i], Rect);
                    // var seed = ((Vector2I)pos).ToString().Hash();
                    var mappedX = 2 * Mathf.Pi * pos.X / Rect.Size.X;
                    var seed = _noise.GetNoise3D(Mathf.Cos(mappedX) * Rect.Size.X * 0.5f / Mathf.Pi, Mathf.Sin(mappedX) * Rect.Size.X * 0.5f / Mathf.Pi, pos.Y).ToString().Hash();
                    // var color = ColorUtils.RandomColorHSV(seed);
                    // DrawColoredPolygon(cellData.Cell.Points, color);
                    using var rng = new RandomNumberGenerator() { Seed = seed };
                    var height = cellData.Altitude / MaxAltitude;
                    var color = height > 0 ? new Color(height, height, height) : new Color(0, 0, 1 + height * 3f);
                    DrawColoredPolygon(cellData.Cell.Points, color);
                    // DrawColoredPolygon(cellData.Cell.Points, ColorUtils.GetSmoothHeightColor(height));
                    // DrawColoredPolygon(cellData.Cell.Points, new Color((int)cellData.PlateType, (int)cellData.PlateType, (int)cellData.PlateType));
                    // DrawColoredPolygon(cellData.Cell.Points, new Color(cellData.Altitude * 0.6f + 0.4f, (int)cellData.PlateType, rng.Randf()));
                }
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

            // DrawMultiline(lines, Colors.White);
        }

        // foreach (var pos in _points)
        // {
        //     DrawCircle(pos, 2f, Colors.White);
        // }

        // foreach (var (i, cellData) in _cellDatas)
        // {
        //     var pos = _delaunator.Points[i];
        //     var end = pos + cellData.TectonicMovement * 3f;
        //     var length = cellData.TectonicMovement.LengthSquared() / 100f;
        //     DrawArrow(pos, end, new Color(length, 0.3f, 1 - length));
        // }

        DrawRect(Rect, Colors.Red, false);

        // DrawTextureRect(ImageTexture.CreateFromImage(_noise.GetImage((int)Rect.Size.X, (int)Rect.Size.Y)), Rect, false);

        // Find neigbour cells of a cell.
        // var itest = 99;
        // DrawCircle(_delaunator.Points[_delaunator.Triangles[itest]], 6f, Colors.Blue);
        // foreach (var item in _delaunator.EdgesAroundPoint(Delaunator.PreviousHalfedge(itest)))
        // {
        //     DrawCircle(_delaunator.Points[_delaunator.Triangles[item]], 4f, Colors.Red);
        // }

        // DrawCircle(_delaunator.Points[itest], 6f, Colors.Blue);
        // foreach (var item in _delaunator.EdgesAroundPoint(Delaunator.PreviousHalfedge(_cellDatas[itest].TriangleIndex)))
        // {
        //     DrawCircle(_delaunator.Points[_delaunator.Triangles[item]], 4f, Colors.Red);
        // }

        // Find the two cells forming the edge.
        // var edge1 = _edges[itest];
        // var cellP = _delaunator.Triangles[edge1.Index];
        // var cellQ = _delaunator.Triangles[_delaunator.Halfedges[edge1.Index]];

        // DrawCircle(_delaunator.Points[cellP], 4f, Colors.Red);
        // DrawCircle(_delaunator.Points[cellQ], 4f, Colors.Blue);
        // DrawLine(edge1.P, edge1.Q, Colors.Aqua);
    }
}
