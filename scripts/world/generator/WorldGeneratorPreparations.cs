using System.Collections.Generic;
using System.Linq;
using DelaunatorSharp;
using Godot;
using PatternSystem;

namespace WorldGenerator;

public partial class WorldGenerator
{
    private RandomNumberGenerator _rng;
    private List<Vector2> _points;
    private Dictionary<int, int> _edgePointsMap;
    private Delaunator _delaunator;
    protected Dictionary<int, CellData> _cellDatas;
    private double _cellArea;
    private Edge[] _voronoiEdges;

    public IReadOnlyList<Vector2> SamplePoints => _points;
    public IReadOnlyDictionary<int, CellData> CellDatas => _cellDatas;
    public IReadOnlyCollection<Edge> CellEdges => _voronoiEdges;

    protected void InitializeResources()
    {
        ReportProgress("Initializing resources");
        _rng = new RandomNumberGenerator { Seed = Settings.Seed };

        var plateNoise = new FastNoiseLite
        {
            Seed = (int)Settings.Seed,
            NoiseType = FastNoiseLite.NoiseTypeEnum.Cellular,
            Frequency = Settings.NoiseFrequency,
            CellularReturnType = FastNoiseLite.CellularReturnTypeEnum.CellValue,
            DomainWarpEnabled = true,
            DomainWarpAmplitude = 0.75 / Settings.NoiseFrequency,
            DomainWarpFrequency = Settings.NoiseFrequency,
            DomainWarpFractalType = FastNoiseLite.DomainWarpFractalTypeEnum.None,
            FractalType = FastNoiseLite.FractalTypeEnum.None
        };

        _platePattern = new PatternTree("plate_pattern", "Plate Pattern", new FastNoiseLiteNode(plateNoise));

        var upliftNoise = new FastNoiseLiteSettings
        {
            Seed = (int)Settings.Seed + 1,
            NoiseType = NoiseType.Perlin,
            FractalType = FractalType.None,
            Frequency = Settings.NoiseFrequency * 1.3,
            DomainWarpEnabled = false
        };

        _upliftPattern = new PatternTree("uplift_pattern", "Uplift Pattern", new FastNoiseLiteNode(upliftNoise));

        var heightNoise = new FastNoiseLite
        {
            Seed = (int)Settings.Seed + 2,
            NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin,
            // Frequency = Settings.NoiseFrequency / sizeY,
            FractalType = FastNoiseLite.FractalTypeEnum.Fbm,
            FractalOctaves = 4,
            DomainWarpEnabled = false
        };

        _heightPattern = new PatternTree("height_pattern", "Height Pattern", new FastNoiseLiteNode(heightNoise));
    }

    protected void GeneratePoints()
    {
        ReportProgress("Generating points");
        _points = FastPoissonDiskSampling.Sampling(Settings.Bounds.Position, Settings.Bounds.End,
            Settings.MinimumCellDistance, _rng, Settings.PoisosonDiskSamplingIterations);
        _edgePointsMap = RepeatPointsRoundEdges(_points, Settings.Bounds, 2 * Settings.MinimumCellDistance);

        _cellArea = Settings.Bounds.Area / _points.Count;

        ReportProgress($"{_points.Count} points generated. Average area: {_cellArea}");
    }

    protected void CreateVoronoiDiagram()
    {
        ReportProgress("Creating Voronoi diagram");
        _delaunator = new Delaunator(_points.ToArray());
        _voronoiEdges = _delaunator.GetVoronoiEdgesBasedOnCentroids().ToArray();
        var _cells = _delaunator.GetVoronoiCellsBasedOnCentroids().ToArray();
        _cellDatas = new Dictionary<int, CellData>(_cells.Length);

        for (var i = 0; i < _cells.Length; i++)
            _cellDatas[_cells[i].Index] = new CellData
            {
                Cell = _cells[i],
                Area = GeometryUtils.CalculatePolygonArea(_cells[i].Points)
            };

        for (var i = 0; i < _delaunator.Triangles.Length; i++)
            _cellDatas[_delaunator.Triangles[i]].TriangleIndex = i;
    }
}