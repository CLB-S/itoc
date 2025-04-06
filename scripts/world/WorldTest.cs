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

    public enum ColorPreset
    {
        Plates,
        Height,
        PlateTypes,
        Precipitation,
    }

    public class CellData
    {
        public int TriangleIndex;
        public VoronoiCell Cell;
        public Vector2 TectonicMovement;
        public PlateType PlateType;
        public float Altitude = 0f;
        public bool RoundPlateJunction = false;
        public float Precipitation;
        public float Flux;
        public RiverSegment River;
    }

    public class RiverSegment
    {
        public int ID;
        public float Length;
        public float Width;
        public List<Vector2> Path = new();
    }


    private ulong _seed = 234;
    public ulong Seed
    {
        get
        {
            if (_seed == 0) return GD.Randi();
            return _seed;
        }
        set { _seed = value; }
    }

    public float MaxTectonicMovement = 10.0f;
    public float MaxAltitude = 2000.0f;

    public float ContinentRatio = 0.4f;
    public float PlateMergeRatio = 0.13f;
    [Export] public float MinimumCellDistance = 5f;
    [Export] public Rect2 Rect = new Rect2(-500, -500, 1000, 1000);
    public Texture2D HeightMapTexture;

    public ColorPreset DrawingCorlorPreset = ColorPreset.Height;
    public bool DrawTectonicMovement = false;
    public bool DrawCellOutlines = false;
    public bool DrawRivers = false;
    public bool DrawInterpolatedHeightMap = false;


    private List<Vector2> _points;
    private Dictionary<int, int> _edgePointsMap;
    private Delaunator _delaunator;
    private Dictionary<int, CellData> _cellDatas;
    private Edge[] _edges;
    private Noise _plateNoise;
    private Noise _heightNoise;
    private RandomNumberGenerator _rng;
    private readonly Dictionary<int, RiverSegment> _rivers = new();
    private int _riverIdCounter;
    private int _heightMapResolution = 1000;

    public override void _Ready()
    {
        base._Ready();
        GenerateMap();
    }

    public void GenerateMap()
    {
        var seed = Seed;
        _rng = new RandomNumberGenerator { Seed = seed };
        _plateNoise = new FastNoiseLite
        {
            Seed = (int)seed,
            NoiseType = FastNoiseLite.NoiseTypeEnum.Cellular,
            Frequency = 0.010f,
            CellularReturnType = FastNoiseLite.CellularReturnTypeEnum.CellValue,
            DomainWarpEnabled = true,
            DomainWarpAmplitude = 90f,
            DomainWarpFrequency = 0.010f,
            DomainWarpFractalType = FastNoiseLite.DomainWarpFractalTypeEnum.None,
            FractalType = FastNoiseLite.FractalTypeEnum.None
        };

        _heightNoise = new FastNoiseLite
        {
            Seed = (int)seed,
            NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin,
            Frequency = 0.010f,
            FractalType = FastNoiseLite.FractalTypeEnum.Fbm,
            FractalOctaves = 4,
            DomainWarpEnabled = false
        };

        _points = FastPoissonDiskSampling.Sampling(Rect.Position, Rect.End, MinimumCellDistance, _rng);
        _edgePointsMap = RepeatPointsRoundEdges(_points, Rect, 2 * MinimumCellDistance);
        _delaunator = new Delaunator(_points.ToArray());
        _edges = _delaunator.GetVoronoiEdgesBasedOnCentroids().ToArray();
        var _cells = _delaunator.GetVoronoiCellsBasedOnCentroids().ToArray();
        _cellDatas = new Dictionary<int, CellData>(_cells.Length);
        for (var i = 0; i < _cells.Length; i++)
            _cellDatas[_cells[i].Index] = new CellData
            {
                Cell = _cells[i]
                // Altitude = (_heightNoise.GetNoise2Dv(_points[i]) + 1f) * MaxAltitude * 0.01f,
            };

        for (var i = 0; i < _delaunator.Triangles.Length; i++)
            _cellDatas[_delaunator.Triangles[i]].TriangleIndex = i;

        InitTectonicProperties();
        CalculateAltitudes();
        // ResolveDepressions();
        // CalculatePrecipitation();
        CalculateWaterFlux();
        HeightMapTexture = GetHeightMapImageTexture();
        QueueRedraw();
    }

    public float[,] CalculateFullHeightMap()
    {
        var posList = new List<Vector2>(_cellDatas.Count);
        var dataList = new List<float>(_cellDatas.Count);
        for (var i = 0; i < _cellDatas.Count; i++)
        {
            posList.Add(_points[i]);
            dataList.Add(_cellDatas[i].Altitude);
        }

        return IdwInterpolator.ConstructHeightMap(posList, dataList, _heightMapResolution, _heightMapResolution, Rect);
    }

    public ImageTexture GetHeightMapImageTexture()
    {
        var heightMap = CalculateFullHeightMap();
        var image = Image.CreateEmpty(_heightMapResolution, _heightMapResolution, false, Image.Format.Rgb8);
        for (var x = 0; x < _heightMapResolution; x++)
            for (var y = 0; y < _heightMapResolution; y++)
            {
                var h = 0.5f * (1 + heightMap[x, y] / MaxAltitude);
                image.SetPixel(x, y, new Color(h, h, h));
            }

        return ImageTexture.CreateFromImage(image);
    }

    private Vector2 UniformPosition(Vector2 position, Rect2 rect)
    {
        position -= rect.Position;
        return new Vector2(
            Mathf.PosMod(position.X, rect.Size.X),
            Mathf.PosMod(position.Y, rect.Size.Y)
        ) + rect.Position;
    }

    private Dictionary<int, int> RepeatPointsRoundEdges(List<Vector2> points, Rect2 rect, float edgeDistance)
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

    private PlateType RandomPlateType(RandomNumberGenerator rng)
    {
        if (rng.Randf() < 1 - ContinentRatio)
            return PlateType.Oceans;

        return PlateType.Continent;
    }

    private float MergeNoiseValue(float value)
    {
        if (PlateMergeRatio > 0)
        {
            var normalized = (value + 1) * 0.5f;
            return 2 * Mathf.Floor(normalized / PlateMergeRatio) * PlateMergeRatio - 1;
        }

        return value;
    }

    private void InitTectonicProperties()
    {
        var rng = new RandomNumberGenerator();

        foreach (var (i, cellData) in _cellDatas)
        {
            var pos = UniformPosition(_points[i], Rect);
            var mappedX = 2 * Mathf.Pi * pos.X / Rect.Size.X;
            var noiseValue = _plateNoise.GetNoise3D(Mathf.Cos(mappedX) * Rect.Size.X * 0.5f / Mathf.Pi,
                Mathf.Sin(mappedX) * Rect.Size.X * 0.5f / Mathf.Pi, pos.Y);
            var seed = MergeNoiseValue(noiseValue).ToString().Hash();
            rng.Seed = seed;
            var r = rng.Randf() * MaxTectonicMovement;
            var phi = rng.Randf() * Mathf.Pi * 2;
            cellData.TectonicMovement = new Vector2(Mathf.Cos(phi), Mathf.Sin(phi)) * r;
            cellData.PlateType =
                RandomPlateType(rng); // (PlateType)(rng.Randi() % Enum.GetNames(typeof(PlateType)).Length);
        }
    }

    private void CalculateAltitudes()
    {
        var initialIndices = new List<int>();

        foreach (var edge in _edges)
        {
            var cellPId = _delaunator.Triangles[edge.Index];
            var cellQId = _delaunator.Triangles[_delaunator.Halfedges[edge.Index]];
            var cellP = _cellDatas[cellPId];
            var cellQ = _cellDatas[cellQId];
            if (cellP.TectonicMovement != cellQ.TectonicMovement)
            {
                // [-1, 1]
                var l = _points[cellPId] - _points[cellQId];
                var relativeMovement = (cellQ.TectonicMovement.Dot(l) - cellP.TectonicMovement.Dot(l)) /
                                       (2 * l.Length() *
                                        MaxTectonicMovement); // cellP.TectonicMovement.Dot(cellQ.TectonicMovement) / MaxTectonicMovement / MaxTectonicMovement;

                if (Mathf.Abs(relativeMovement) < 0.25f)
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
                    {
                        altitude = 1 - 0.75f * (relativeMovement - 1) * (relativeMovement - 1);
                    }

                    cellP.Altitude += altitude * MaxAltitude;
                    cellQ.Altitude += altitude * MaxAltitude;
                }
                else if (cellP.PlateType == PlateType.Oceans && cellQ.PlateType == PlateType.Oceans)
                {
                    var altitude = Mathf.Pow(relativeMovement, 3) / 2f + 0.5f;
                    altitude = altitude * altitude * 0.5f - 0.3f;
                    if (relativeMovement > 0)
                        altitude += 0.25f * (1 - (1 - relativeMovement) * (1 - relativeMovement));

                    cellP.Altitude += altitude * MaxAltitude;
                    cellQ.Altitude += altitude * MaxAltitude;
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
                        cellP.Altitude += 1 - (relativeMovement - 1) * (relativeMovement - 1);

                        var altitude = Mathf.Pow(relativeMovement, 3) / 2f + 0.5f;
                        altitude = altitude * altitude - 0.25f;
                        cellQ.Altitude += altitude;
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
                        cellQ.Altitude += 1 - (relativeMovement - 1) * (relativeMovement - 1);

                        var altitude = Mathf.Pow(relativeMovement, 3) / 2f + 0.5f;
                        altitude = altitude * altitude - 0.25f;
                        cellP.Altitude += altitude;
                    }
                }
            }
        }

        PropagateAltitudes(initialIndices, 0.8f);
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

    private void PropagateAltitudes(IEnumerable<int> initialIndices, float decrement = 0.9f, float sharpness = 0.1f)
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
            var currentIndex = queue.Dequeue();
            var currentCell = _cellDatas[currentIndex];
            var parentHeight = currentCell.Altitude;

            var propagatedHeight = parentHeight * decrement;
            if (Mathf.Abs(propagatedHeight) < 0.01f)
                continue;

            foreach (var neighborIndex in GetNeighborCells(currentIndex))
                if (!used.Contains(neighborIndex))
                {
                    var neighbor = _cellDatas[neighborIndex];
                    var mod = sharpness == 0 ? 1.0f : 1.1f - sharpness + _rng.Randf() * sharpness;
                    var heightContribution = propagatedHeight * mod;

                    neighbor.Altitude += heightContribution;

                    used.Add(neighborIndex);
                    queue.Enqueue(neighborIndex, -Mathf.Abs(_cellDatas[neighborIndex].Altitude));
                }
        }
    }

    private void CalculatePrecipitation()
    {
        // Simple west-to-east moisture simulation
        const float initialMoisture = 1.0f;
        const float evaporationRate = 0.05f;

        foreach (var cellEntry in _cellDatas.OrderBy(c => _points[c.Key].X))
        {
            var cell = cellEntry.Value;
            if (cell.PlateType == PlateType.Oceans)
            {
                cell.Precipitation = initialMoisture;
                continue;
            }

            var neighbors = GetNeighborCells(cell);
            var highest = neighbors.OrderByDescending(n => _cellDatas[n].Altitude).First();

            if (_cellDatas[highest].Altitude > cell.Altitude)
                cell.Precipitation = Mathf.Max(0, _cellDatas[highest].Precipitation - evaporationRate);
        }
    }

    private void ResolveDepressions()
    {
        var landCells = _cellDatas.Values.Where(c => c.PlateType != PlateType.Oceans).ToList();
        bool hasDepressions;

        do
        {
            hasDepressions = false;
            foreach (var cell in landCells)
            {
                var lowestNeighbor = GetNeighborCells(cell)
                    .OrderBy(n => _cellDatas[n].Altitude).First();

                if (cell.Altitude <= _cellDatas[lowestNeighbor].Altitude)
                {
                    cell.Altitude = _cellDatas[lowestNeighbor].Altitude * 1.05f;
                    hasDepressions = true;
                }
            }
        } while (hasDepressions);
    }

    private void CalculateWaterFlux()
    {
        var orderedCells = _cellDatas.Values
            .Where(c => c.PlateType != PlateType.Oceans)
            .OrderByDescending(c => c.Altitude)
            .ToList();

        foreach (var cell in orderedCells)
        {
            var neighbors = GetNeighborCells(cell);
            var lowest = neighbors.OrderBy(n => _cellDatas[n].Altitude).First();
            // if (_cellDatas[lowest].Altitude < 0)
            //     continue;

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

        UpdateRiverWidth(riverSeg);
    }

    private void UpdateRiverWidth(RiverSegment river)
    {
        const float baseWidth = 1.0f;
        const float widthGrowth = 0.2f;

        river.Width = baseWidth + widthGrowth * river.Path.Count;
    }

    private void DrawArrow(Vector2 start, Vector2 end, Color color, bool twoLineHead = false)
    {
        // the arrow size and flatness
        var arrowSize = (end - start).Length() * 0.4f;
        var flatness = 0.5f;

        // the line starting and ending points
        DrawLine(start, end, color, Mathf.Sqrt(arrowSize));

        // calculate the direction vector
        var direction = (end - start).Normalized();

        // calculate the side vectors
        var side1 = new Vector2(-direction.Y, direction.X);
        var side2 = new Vector2(direction.Y, -direction.X);

        // calculate the T-junction points
        var e1 = end + side1 * arrowSize * flatness;
        var e2 = end + side2 * arrowSize * flatness;

        // calculate the arrow edges
        var p1 = e1 - direction * arrowSize;
        var p2 = e2 - direction * arrowSize;

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
            foreach (var (i, cellData) in _cellDatas)
            {
                if (!Rect.HasPoint(_points[i])) continue;

                if (cellData.Cell.Points.Length >= 3)
                {
                    var pos = UniformPosition(_points[i], Rect);

                    switch (DrawingCorlorPreset)
                    {
                        case ColorPreset.Plates:
                            var mappedX = 2 * Mathf.Pi * pos.X / Rect.Size.X;
                            var noiseValue = _plateNoise.GetNoise3D(Mathf.Cos(mappedX) * Rect.Size.X * 0.5f / Mathf.Pi,
                                Mathf.Sin(mappedX) * Rect.Size.X * 0.5f / Mathf.Pi, pos.Y);
                            var seed = MergeNoiseValue(noiseValue).ToString().Hash();
                            var color = ColorUtils.RandomColorHSV(seed);
                            DrawColoredPolygon(cellData.Cell.Points, color);
                            break;
                        case ColorPreset.Height:
                            var height = cellData.Altitude / MaxAltitude;
                            DrawColoredPolygon(cellData.Cell.Points, ColorUtils.GetHeightColor(height));
                            break;
                        case ColorPreset.PlateTypes:
                            DrawColoredPolygon(cellData.Cell.Points,
                                new Color(0.2f * (int)cellData.PlateType, 0.2f * (int)cellData.PlateType,
                                    (int)cellData.PlateType));
                            break;
                        case ColorPreset.Precipitation:
                            DrawColoredPolygon(cellData.Cell.Points,
                                new Color(cellData.Precipitation, cellData.Precipitation, cellData.Precipitation));
                            break;
                    }
                }
            }

        if (DrawCellOutlines && _edges != null)
        {
            var lines = new Vector2[_edges.Count() * 2];
            var i = 0;
            foreach (var edge in _edges)
            {
                lines[i++] = edge.P;
                lines[i++] = edge.Q;
            }

            DrawMultiline(lines, Colors.White);
        }

        // foreach (var pos in _points)
        // {
        //     DrawCircle(pos, 2f, Colors.White);
        // }

        // foreach (var i in _delaunator.GetHullPoints())
        // {
        //     DrawCircle(i, 2f, Colors.White);
        // }

        if (DrawTectonicMovement)
            foreach (var (i, cellData) in _cellDatas)
            {
                if (i % 10 != 0) continue;
                var pos = _points[i];
                var end = pos + cellData.TectonicMovement * 3f;
                var length = cellData.TectonicMovement.LengthSquared() / 100f;
                DrawArrow(pos, end, new Color(length, 0.5f, 1 - length));
            }


        // Draw rivers
        if (DrawRivers)
            foreach (var river in _rivers.Values)
            {
                if (river.Path.Count < 2) continue;

                var points = river.Path.ToArray();
                var color = new Color(0.2f, 0.4f, 0.8f);

                for (var i = 0; i < points.Length - 1; i++)
                {
                    if ((points[i] - points[i + 1]).LengthSquared() >
                        MinimumCellDistance * MinimumCellDistance * 5) continue;
                    DrawLine(points[i], points[i + 1], color, river.Width);
                }
                // Draw main river channel
                // DrawPolyline(points, color, river.Width);
                // Draw river banks
                // var bankColor = new Color(0.1f, 0.3f, 0.7f);
                // DrawPolyline(points, bankColor, river.Width * 1.2f);
            }

        // DrawTextureRect(ImageTexture.CreateFromImage(_noise.GetImage((int)Rect.Size.X, (int)Rect.Size.Y)), Rect, false);

        if (DrawInterpolatedHeightMap) DrawTextureRect(HeightMapTexture, Rect, false);

        // Find neigbour cells of a cell.
        // var itest = 99;
        // DrawCircle(_points[_delaunator.Triangles[itest]], 6f, Colors.Blue);
        // foreach (var item in _delaunator.EdgesAroundPoint(Delaunator.PreviousHalfedge(itest)))
        // {
        //     DrawCircle(_points[_delaunator.Triangles[item]], 4f, Colors.Red);
        // }

        // DrawCircle(_points[itest], 6f, Colors.Blue);
        // foreach (var item in _delaunator.EdgesAroundPoint(Delaunator.PreviousHalfedge(_cellDatas[itest].TriangleIndex)))
        // {
        //     DrawCircle(_points[_delaunator.Triangles[item]], 4f, Colors.Red);
        // }

        // Find the two cells forming the edge.
        // var edge1 = _edges[itest];
        // var cellP = _delaunator.Triangles[edge1.Index];
        // var cellQ = _delaunator.Triangles[_delaunator.Halfedges[edge1.Index]];

        // DrawCircle(_points[cellP], 4f, Colors.Red);
        // DrawCircle(_points[cellQ], 4f, Colors.Blue);
        // DrawLine(edge1.P, edge1.Q, Colors.Aqua);

        DrawRect(Rect, Colors.Red, false);
    }

    public void OnSeedSpinBoxValueChanged(float value)
    {
        Seed = (ulong)value;
    }

    public void OnContinentRatioSpinBoxValueChanged(float value)
    {
        ContinentRatio = value;
    }

    public void OnPlateMergeRatioSpinBoxValueChanged(float value)
    {
        PlateMergeRatio = value;
    }

    public void OnCellDistanceSpinBoxValueChanged(float value)
    {
        MinimumCellDistance = value;
    }

    public void OnRegenerateButtonPressed()
    {
        GenerateMap();
    }

    public void OnDrawingCorlorPresetSelected(int value)
    {
        DrawingCorlorPreset = (ColorPreset)value;
        QueueRedraw();
    }

    public void OnDrawTectonicMovementToggled(bool toggledOn)
    {
        DrawTectonicMovement = toggledOn;
        QueueRedraw();
    }

    public void OnDrawCellOutlinesToggled(bool toggledOn)
    {
        DrawCellOutlines = toggledOn;
        QueueRedraw();
    }

    public void OnDrawRiversToggled(bool toggledOn)
    {
        DrawRivers = toggledOn;
        QueueRedraw();
    }

    public void OnDrawInterpolatedHeightMapToggled(bool toggledOn)
    {
        DrawInterpolatedHeightMap = toggledOn;
        QueueRedraw();
    }
}
