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

        _platePattern = new PatternTreeBuilder("plate_pattern", "Plate Pattern")
            .WithFastNoiseLite(new FastNoiseLiteSettings
            {
                Seed = (int)$"plate{Settings.Seed}".Hash(),
                NoiseType = NoiseType.Cellular,
                Frequency = Settings.NoiseFrequency,
                CellularReturnType = CellularReturnType.CellValue,
                DomainWarpEnabled = true,
                DomainWarpAmplitude = 0.75 / Settings.NoiseFrequency,
                DomainWarpFrequency = Settings.NoiseFrequency,
                DomainWarpFractalType = DomainWarpFractalType.None,
                FractalType = FractalType.None
            })
            .Build();

        _upliftPattern = new PatternTreeBuilder("uplift_pattern", "Uplift Pattern")
            .WithFastNoiseLite(new FastNoiseLiteSettings
            {
                Seed = (int)$"uplift{Settings.Seed}".Hash(),
                NoiseType = NoiseType.Perlin,
                FractalType = FractalType.None,
                Frequency = Settings.NoiseFrequency * Settings.UpliftNoiseFrequency,
            })
            .Build();

        _temperaturePattern = new PatternTreeBuilder("temperature_pattern", "Temperature Pattern")
            .WithFastNoiseLite(new FastNoiseLiteSettings
            {
                Seed = (int)$"temperature{Settings.Seed}".Hash(),
                NoiseType = NoiseType.Perlin,
                FractalOctaves = 3,
                Frequency = Settings.NoiseFrequency * Settings.TemperatureNoiseFrequency,
            })
            .ScaleXBy(3)
            .Multiply(Settings.TemperatureNoiseIntensity)
            .Build();

        _precipitationPattern = new PatternTreeBuilder("precipitation_pattern", "Precipitation Pattern")
            .WithFastNoiseLite(new FastNoiseLiteSettings
            {
                Seed = (int)$"precipitation{Settings.Seed}".Hash(),
                NoiseType = NoiseType.Perlin,
                FractalOctaves = 2,
                Frequency = Settings.NoiseFrequency * Settings.PrecipitationNoiseFrequency,
            })
            .ScaleXBy(2)
            .Multiply(Settings.PrecipitationNoiseIntensity)
            .Build();

        _heightPattern = new PatternTreeBuilder("height_pattern", "Height Pattern")
            .WithFastNoiseLite(new FastNoiseLiteSettings
            {
                Seed = (int)$"height{Settings.Seed}".Hash(),
                NoiseType = NoiseType.Perlin,
                // Frequency = Settings.NoiseFrequency / sizeY,
                FractalOctaves = 4,
            })
            .Subtract(0.5) // Remove later after adding oceans.
            .Multiply(30)
            .Build();

    }

    protected void GeneratePoints()
    {
        ReportProgress("Generating points");
        _points = FastPoissonDiskSampling.Sampling(Settings.Bounds.Position, Settings.Bounds.End,
            Settings.MinimumCellDistance, _rng, Settings.PoisosonDiskSamplingIterations);
        _edgePointsMap = RepeatPointsRoundEdges(_points, Settings.Bounds, 2 * Settings.MinimumCellDistance);

        _cellArea = (double)Settings.Bounds.Size.X * Settings.Bounds.Size.Y / _points.Count;

        ReportProgress($"{_points.Count} points generated. Average area: {_cellArea:f2}");
    }

    protected void CreateVoronoiDiagram()
    {
        ReportProgress("Creating Voronoi diagram");
        _delaunator = new Delaunator(_points.ToArray());
        _voronoiEdges = _delaunator.GetVoronoiEdgesBasedOnCentroids().ToArray();
        var _cells = _delaunator.GetVoronoiCellsBasedOnCentroids().ToArray();
        _cellDatas = new Dictionary<int, CellData>(_cells.Length);

        for (var i = 0; i < _cells.Length; i++)
        {
            var pos = SamplePoints[_cells[i].Index];
            if (!((Rect2)Settings.Bounds).HasPoint(pos))
                continue;

            var latitude = GetLatitude(pos);
            var precipitationNoiseValue = _precipitationPattern.EvaluateSeamlessX(pos, Settings.Bounds);
            var temperatureNoiseValue = _temperaturePattern.EvaluateSeamlessX(pos, Settings.Bounds);

            _cellDatas[_cells[i].Index] = new CellData
            {
                Cell = _cells[i],
                Area = GeometryUtils.CalculatePolygonArea(_cells[i].Points),
                Precipitation = ClimateUtils.GetPrecipitation(latitude, Settings.MaxPrecipitation) *
                            (1 + precipitationNoiseValue),
                Temperature = ClimateUtils.GetTemperature(latitude, Settings.EquatorialTemperature, Settings.PolarTemperature) +
                            temperatureNoiseValue,
            };
        }

        for (var i = 0; i < _delaunator.Triangles.Length; i++)
            if (_cellDatas.TryGetValue(_delaunator.Triangles[i], out var cellData))
                cellData.TriangleIndex = i;
    }
}