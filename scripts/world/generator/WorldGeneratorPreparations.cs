using Godot;
using System.Collections.Generic;
using System.Linq;
using DelaunatorSharp;

namespace WorldGenerator;

public partial class WorldGenerator
{
    private RandomNumberGenerator _rng;
    private List<Vector2> _points;
    private Dictionary<int, int> _edgePointsMap;
    private Delaunator _delaunator;
    private Dictionary<int, CellData> _cellDatas;
    private float _cellArea;
    private Edge[] _voronoiEdges;

    public IReadOnlyList<Vector2> SamplePoints { get => _points; }
    public IReadOnlyDictionary<int, CellData> CellDatas { get => _cellDatas; }
    public IReadOnlyCollection<Edge> CellEdges { get => _voronoiEdges; }

    private void InitializeResources()
    {
        ReportProgress("Initializing resources");
        _rng = new RandomNumberGenerator { Seed = Settings.Seed };
        var sizeY = Settings.Bounds.Size.Y;
        _plateNoise = new FastNoiseLite
        {
            Seed = (int)Settings.Seed,
            NoiseType = FastNoiseLite.NoiseTypeEnum.Cellular,
            Frequency = Settings.NoiseFrequency / sizeY,
            CellularReturnType = FastNoiseLite.CellularReturnTypeEnum.CellValue,
            DomainWarpEnabled = true,
            DomainWarpAmplitude = 0.75 * sizeY / Settings.NoiseFrequency,
            DomainWarpFrequency = Settings.NoiseFrequency / sizeY,
            DomainWarpFractalType = FastNoiseLite.DomainWarpFractalTypeEnum.None,
            FractalType = FastNoiseLite.FractalTypeEnum.None
        };

        _heightNoise = new FastNoiseLite
        {
            Seed = (int)Settings.Seed,
            NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin,
            // Frequency = Settings.NoiseFrequency / sizeY,
            FractalType = FastNoiseLite.FractalTypeEnum.Fbm,
            FractalOctaves = 4,
            DomainWarpEnabled = false
        };
    }

    private void GeneratePoints()
    {
        ReportProgress("Generating points");
        _points = FastPoissonDiskSampling.Sampling(Settings.Bounds.Position, Settings.Bounds.End, Settings.MinimumCellDistance, _rng);
        _edgePointsMap = RepeatPointsRoundEdges(_points, Settings.Bounds, 2 * Settings.MinimumCellDistance);

        _cellArea = (float)Settings.Bounds.Area / _points.Count;

        ReportProgress($"{_points.Count} points generated. Average area: {_cellArea}");
    }

    private void CreateVoronoiDiagram()
    {
        ReportProgress("Creating Voronoi diagram");
        _delaunator = new Delaunator(_points.ToArray());
        _voronoiEdges = _delaunator.GetVoronoiEdgesBasedOnCentroids().ToArray();
        var _cells = _delaunator.GetVoronoiCellsBasedOnCentroids().ToArray();
        _cellDatas = new Dictionary<int, CellData>(_cells.Length);

        for (var i = 0; i < _cells.Length; i++)
            _cellDatas[_cells[i].Index] = new CellData
            {
                Cell = _cells[i]
            };

        for (var i = 0; i < _delaunator.Triangles.Length; i++)
            _cellDatas[_delaunator.Triangles[i]].TriangleIndex = i;
    }
}
