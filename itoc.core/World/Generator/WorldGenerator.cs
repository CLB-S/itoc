using Godot;
using ITOC.Core.DelaunatorSharp;
using ITOC.Core.Interpolators;
using ITOC.Core.Palette;
using ITOC.Core.PatternSystem;
using ITOC.Core.Utils;
using Supercluster.KDTree;

namespace ITOC.Core.WorldGeneration;

public partial class WorldGenerator : WorldGeneratorBase
{
    #region Fields and Properties

    private RandomNumberGenerator _rng;
    private List<Vector2> _points;
    private Dictionary<int, int> _edgePointsMap;
    private Dictionary<int, int> _triangleIndicesMap;
    private Delaunator _delaunator;
    private Dictionary<int, CellData> _cellDatas;
    private KDTree<double, int> _cellDatasKdTree;
    private double _cellArea;
    private Edge[] _voronoiEdges;
    private readonly HashSet<int> _riverMouths = new();

    private FluvialEroder _fluvialEroder;

    // Patterns
    private PatternTree _platePattern;
    private PatternTree _upliftPattern;
    private PatternTree _temperaturePattern;
    private PatternTree _precipitationPattern;
    private PatternTree _domainWarpPattern;
    private PatternTree _heightPattern;

    public WorldSettings Settings { get; }

    public PatternLibrary PatternLibrary { get; private set; }
    public double MaxHeight { get; private set; }


    public IReadOnlyDictionary<int, CellData> CellDatas => _cellDatas;
    public IReadOnlyCollection<Edge> CellEdges => _voronoiEdges;
    public IReadOnlyList<Vector2> SamplePoints => _points;


    public IReadOnlySet<int> Lakes => _fluvialEroder.Lakes;
    public IReadOnlyDictionary<int, int> Receivers => _fluvialEroder.Receivers;
    public IReadOnlyDictionary<int, double> Drainages => _fluvialEroder.Drainages;

    #endregion

    public WorldGenerator(WorldSettings settings = null) : base()
    {
        Settings = settings ?? new WorldSettings();
    }

    protected override void InitializePipeline()
    {
        _generationPipeline.AddLast(new WorldGenerationStep("initialize_resources", InitializeResources));
        _generationPipeline.AddLast(new WorldGenerationStep("generate_sample_points", GenerateSamplePoints));
        _generationPipeline.AddLast(new WorldGenerationStep("initialize_cell_datas", InitializeCellDatas));
        _generationPipeline.AddLast(new WorldGenerationStep("initialize_tectonics", InitializeTectonicProperties));
        _generationPipeline.AddLast(new WorldGenerationStep("calculate_uplifts", CalculateUplifts));
        _generationPipeline.AddLast(new WorldGenerationStep("find_river_mouths", FindRiverMouths));
        _generationPipeline.AddLast(new WorldGenerationStep("process_fluvial_erosion", ProcessFluvialErosion));
        _generationPipeline.AddLast(new WorldGenerationStep("adjust_temperature", AdjustTemperatureAccordingToHeight));
        _generationPipeline.AddLast(new WorldGenerationStep("set_biomes", SetBiomes));
    }

    #region Preperations

    private void InitializeResources()
    {
        _rng = new RandomNumberGenerator { Seed = Settings.Seed };
        PatternLibrary = new PatternLibrary((int)Settings.Seed);

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
                Frequency = Settings.NoiseFrequency * Settings.UpliftNoiseFrequency
            })
            .Build();

        _temperaturePattern = new PatternTreeBuilder("temperature_pattern", "Temperature Pattern")
            .WithFastNoiseLite(new FastNoiseLiteSettings
            {
                Seed = (int)$"temperature{Settings.Seed}".Hash(),
                NoiseType = NoiseType.Perlin,
                FractalOctaves = 3,
                Frequency = Settings.NoiseFrequency * Settings.TemperatureNoiseFrequency
            })
            .ScaleXBy(3)
            .Multiply(Settings.TemperatureNoiseIntensity)
            .Build();

        _precipitationPattern = new PatternTreeBuilder("precipitation_pattern", "Precipitation Pattern")
            .WithFastNoiseLite(new FastNoiseLiteSettings
            {
                Seed = (int)$"precipitation{Settings.Seed}".Hash(),
                NoiseType = NoiseType.SimplexSmooth,
                FractalOctaves = 3,
                Frequency = Settings.NoiseFrequency * Settings.PrecipitationNoiseFrequency
            })
            .ScaleXBy(2)
            .Multiply(1.3)
            .Subtract(0.3)
            .Min(Settings.PrecipitationNoiseIntensity)
            .Max(-1)
            .Build();

        _domainWarpPattern = new PatternTreeBuilder("domain_warp_pattern", "Domain Warp Pattern")
            .WithFastNoiseLite(new FastNoiseLiteSettings
            {
                Seed = (int)$"domain_warp{Settings.Seed}".Hash(),
                NoiseType = NoiseType.Perlin,
                // FractalType = FractalType.None,
                FractalOctaves = 2,
                Frequency = Settings.DomainWarpFrequency
            })
            .Multiply(Settings.DomainWarpIntensity)
            .Build();

        _heightPattern = PatternLibrary.Instance.GetPattern("plain");
    }

    private void GenerateSamplePoints()
    {
        _points = FastPoissonDiskSampling.Sampling(Settings.Bounds.Position, Settings.Bounds.End,
            Settings.MinimumCellDistance, _rng, Settings.PoisosonDiskSamplingIterations);
        _edgePointsMap = RepeatPointsRoundEdges(_points, Settings.Bounds, 2 * Settings.MinimumCellDistance);

        _delaunator = new Delaunator(_points.ToArray());
        _voronoiEdges = _delaunator.GetVoronoiEdgesBasedOnCentroids().ToArray();

        ReportProgress($"{_points.Count} points generated.");
    }

    private void InitializeCellDatas()
    {
        var _cells = _delaunator.GetVoronoiCellsBasedOnCentroids().ToArray();
        _cellDatas = new Dictionary<int, CellData>(_cells.Length);

        for (var i = 0; i < _cells.Length; i++)
        {
            var pos = _points[_cells[i].Index];
            if (!((Rect2)Settings.Bounds).HasPoint(pos))
                continue;

            var latitude = GetLatitude(pos);
            var precipitationNoiseValue = _precipitationPattern.EvaluateSeamlessX(pos, Settings.Bounds);
            var temperatureNoiseValue = _temperaturePattern.EvaluateSeamlessX(pos, Settings.Bounds);

            _cellDatas[_cells[i].Index] = new CellData
            {
                Cell = _cells[i],
                Position = pos,
                Area = GeometryUtils.CalculatePolygonArea(_cells[i].Points),
                Precipitation = ClimateUtils.GetPrecipitation(latitude, Settings.MaxPrecipitation) *
                                (1 + precipitationNoiseValue),
                Temperature =
                    ClimateUtils.GetTemperature(latitude, Settings.EquatorialTemperature, Settings.PolarTemperature) +
                    temperatureNoiseValue
            };
        }

        _triangleIndicesMap = new Dictionary<int, int>(_delaunator.Triangles.Length);
        for (var i = 0; i < _delaunator.Triangles.Length; i++)
            if (_cellDatas.ContainsKey(_delaunator.Triangles[i]))
                _triangleIndicesMap[_delaunator.Triangles[i]] = i;

        var pointsData = _cellDatas.Keys.Select(i =>
        {
            var mappedX = 2 * Mathf.Pi * _points[i].X / Settings.Bounds.Size.X;
            return new[]
            {
                Mathf.Cos(mappedX) * Settings.Bounds.Size.X * 0.5 / Mathf.Pi,
                Mathf.Sin(mappedX) * Settings.Bounds.Size.X * 0.5 / Mathf.Pi,
                _points[i].Y
            };
        }).ToArray();

        static double l2Norm(double[] x, double[] y)
        {
            double dist = 0;
            for (var i = 0; i < x.Length; i++) dist += (x[i] - y[i]) * (x[i] - y[i]);
            return dist;
        }

        _cellDatasKdTree = new KDTree<double, int>(3, pointsData, _cellDatas.Keys.ToArray(), l2Norm);

        _cellArea = (double)Settings.Bounds.Size.X * Settings.Bounds.Size.Y / _cellDatas.Count;

    }


    #endregion

    #region Tectonics

    private PlateType RandomPlateType(RandomNumberGenerator rng)
    {
        if (rng.Randf() < 1 - Settings.ContinentRatio)
            return PlateType.Oceans;

        return PlateType.Continent;
    }

    private void InitializeTectonicProperties()
    {
        // Use Parallel.ForEach to process cells in parallel
        Parallel.ForEach(_cellDatas, cellDataPair =>
        {
            var i = cellDataPair.Key;
            var cellData = cellDataPair.Value;

            // Create a thread-local RNG instance
            using var rng = new RandomNumberGenerator();

            var noiseValue = _platePattern.EvaluateSeamlessX(cellData.Position, Settings.Bounds);
            var seed = noiseValue.ToString().Hash();
            rng.Seed = seed;
            var r = rng.Randf() * Settings.MaxTectonicMovement;
            var phi = rng.Randf() * Mathf.Pi * 2;
            cellData.TectonicMovement = new Vector2(Mathf.Cos(phi), Mathf.Sin(phi)) * r;
            cellData.PlateType = RandomPlateType(rng);
            cellData.PlateSeed = seed;
        });
    }


    #endregion

    #region Uplifts

    private void CalculateUplifts()
    {
        var _initialAltitudeIndices = new HashSet<int>();

        void SetInitialUplift(CellData cell, double uplift)
        {
            var f = _upliftPattern.EvaluateSeamlessX(cell.Position, Settings.Bounds);

            cell.Uplift += uplift * Settings.MaxUplift * (1 + (1 - f) * Settings.UpliftNoiseIntensity);
            _initialAltitudeIndices.Add(cell.Index);
        }

        // Calculating initial uplifts.
        foreach (var edge in _voronoiEdges)
        {
            var cellPId = _delaunator.Triangles[edge.Index];
            var cellQId = _delaunator.Triangles[_delaunator.Halfedges[edge.Index]];
            if (_cellDatas.TryGetValue(cellPId, out var cellP) && _cellDatas.TryGetValue(cellQId, out var cellQ))
            {
                if (cellP.TectonicMovement == cellQ.TectonicMovement) continue;

                // [-1, 1]
                var l = cellP.Position - cellQ.Position;
                var relativeMovement = (cellQ.TectonicMovement.Dot(l) - cellP.TectonicMovement.Dot(l)) /
                                       (2 * l.Length() * Settings.MaxTectonicMovement);

                if (Mathf.Abs(relativeMovement) < 0.5)
                    relativeMovement = Mathf.Pow(relativeMovement * 2, 3) / 2;
                else if (relativeMovement > 0.5)
                    relativeMovement = 1 - 2 * (1 - relativeMovement) * (1 - relativeMovement);
                else if (relativeMovement < -0.5)
                    relativeMovement = -1 + 2 * (1 + relativeMovement) * (1 + relativeMovement);

                // if (Mathf.Abs(relativeMovement) < 0.15)
                //     continue;

                cellP.RoundPlateJunction = true;
                cellQ.RoundPlateJunction = true;

                double uplift;
                if (cellP.PlateType == PlateType.Continent && cellQ.PlateType == PlateType.Continent)
                {
                    if (relativeMovement < 0)
                    {
                        uplift = Mathf.Pow(relativeMovement, 3) / 4.0 + 0.25;
                        uplift *= uplift;
                    }
                    else
                    {
                        uplift = 1 - 0.75f * (1 - relativeMovement) * (1 - relativeMovement);
                    }

                    SetInitialUplift(cellP, uplift);
                    SetInitialUplift(cellQ, uplift);
                }
                else if (cellP.PlateType == PlateType.Oceans && cellQ.PlateType == PlateType.Oceans)
                {
                    uplift = Mathf.Pow(relativeMovement, 3) / 2f + 0.5f;
                    uplift = uplift * uplift * 0.5f - 0.3f;
                    if (relativeMovement > 0)
                        uplift += 0.25f * (1 - (1 - relativeMovement) * (1 - relativeMovement));

                    SetInitialUplift(cellP, uplift);
                    SetInitialUplift(cellQ, uplift);
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
                        uplift = 1 - 0.75 * (1 - relativeMovement) * (1 - relativeMovement) * (1 - relativeMovement);
                        SetInitialUplift(cellP, uplift);

                        // var altitude = Mathf.Pow(relativeMovement, 3) / 2f + 0.5f;
                        // altitude = altitude * altitude - 0.2f;
                        // cellQ.Uplift += altitude;
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
                        uplift = 1 - 0.75 * (1 - relativeMovement) * (1 - relativeMovement) * (1 - relativeMovement);
                        SetInitialUplift(cellQ, uplift);

                        // var altitude = Mathf.Pow(relativeMovement, 3) / 2f + 0.5f;
                        // altitude = altitude * altitude - 0.25f;
                        // cellP.Uplift += altitude;
                    }
                }
            }
        }

        // Propagting uplifts.
        var used = new HashSet<int>();
        var queue = new PriorityQueue<int, double>();
        var sharpness = Settings.UpliftPropagationSharpness;

        foreach (var i in _initialAltitudeIndices)
        {
            used.Add(i);
            queue.Enqueue(i, -Mathf.Abs(_cellDatas[i].Uplift));
        }

        while (queue.Count > 0)
        {
            var currentIndex = queue.Dequeue();
            var currentCell = _cellDatas[currentIndex];
            var parentHeight = currentCell.Uplift;

            var propagatedHeight = parentHeight *
                                   Mathf.Pow(Settings.UpliftPropagationDecrement,
                                       Settings.NormalizedMinimumCellDistance);
            if (Mathf.Abs(propagatedHeight) < 0.01f)
                continue;

            foreach (var neighborIndex in GetNeighborCellIndices(currentIndex))
                if (!used.Contains(neighborIndex) && _cellDatas[neighborIndex].PlateType == PlateType.Continent)
                {
                    var neighbor = _cellDatas[neighborIndex];
                    var mod = sharpness == 0 ? 1.0f : 1.0f + (_rng.Randf() - 0.5) * sharpness;
                    var heightContribution = propagatedHeight * mod;

                    neighbor.Uplift += heightContribution;

                    used.Add(neighborIndex);
                    queue.Enqueue(neighborIndex, -Mathf.Abs(_cellDatas[neighborIndex].Uplift));
                }
        }
    }

    #endregion

    #region Fluvial Erosion

    private void FindRiverMouths()
    {
        _riverMouths.Clear();
        foreach (var edge in _voronoiEdges)
        {
            var cellPId = _delaunator.Triangles[edge.Index];
            var cellQId = _delaunator.Triangles[_delaunator.Halfedges[edge.Index]];
            if (_cellDatas.TryGetValue(cellPId, out var cellP) && _cellDatas.TryGetValue(cellQId, out var cellQ))
            {
                if (cellP.PlateType == PlateType.Continent && cellQ.PlateType == PlateType.Oceans)
                {
                    cellP.IsRiverMouth = true;
                    _riverMouths.Add(cellP.Index);
                }
                else if (cellP.PlateType == PlateType.Oceans && cellQ.PlateType == PlateType.Continent)
                {
                    cellQ.IsRiverMouth = true;
                    _riverMouths.Add(cellQ.Index);
                }
            }
        }
    }

    private void ProcessFluvialErosion()
    {
        var eroderSettings = new FluvialEroderSettings
        {
            ErosionRate = Settings.ErosionRate,
            ErosionTimeStep = Settings.ErosionTimeStep,
            ErosionConvergenceThreshold = Settings.ErosionConvergenceThreshold,
            MaxErosionIterations = Settings.MaxErosionIterations,
            MaxErosionSlopeAngle = Settings.MaxErosionSlopeAngle,
            DefaultCellArea = _cellArea,
        };

        _fluvialEroder = new FluvialEroder(_cellDatas, _riverMouths, eroderSettings, GetNeighborCells, UniformDistance);
        _fluvialEroder.OnProgressReport += ReportProgress;

        _fluvialEroder.Erode();
        MaxHeight = _fluvialEroder.MaxHeight;
    }

    #endregion

    #region Finishing

    private void AdjustTemperatureAccordingToHeight()
    {
        ReportProgress("Adjusting temperature");

        Parallel.ForEach(_cellDatas.Values, cell =>
        {
            if (cell.Height > 0)
                cell.Temperature -= cell.Height * Settings.TemperatureGradientWithAltitude;
        });
    }

    private void SetBiomes()
    {
        ReportProgress("Setting biomes");

        Parallel.ForEach(_cellDatas.Values, cell =>
        {
            cell.Biome = BiomeLibrary.Instance.GetBiomeForConditions(cell.Temperature, cell.Precipitation,
                cell.Height);
        });
    }

    #endregion

    #region Height Map

    protected virtual double NoiseOverlay(double x, double y)
    {
        return _heightPattern.Evaluate(x, y);
    }

    public double GetRawHeight(double x, double y, bool loopDivision = true, bool domainWarping = false,
        bool noiseOverlay = false)
    {
        var point = new Vector2(x, y);
        if (domainWarping)
            point = Warp(point, _domainWarpPattern);

        var (i0, i1, i2) = GetTriangleContainingPoint(point);

        double height;
        if (loopDivision)
        {
            var (p0, p1, p2, h0, h1, h2) = GetSubdividedTriangleContainingPoint(point, i0, i1, i2);
            height = LinearInterpolator.Interpolate(p0, p1, p2, h0, h1, h2, point);
        }
        else
        {
            var p0 = _points[i0];
            var p1 = _points[i1];
            var p2 = _points[i2];

            height = LinearInterpolator.Interpolate(p0, p1, p2,
                CellDatas[i0].Height, CellDatas[i1].Height, CellDatas[i2].Height,
                new Vector2(x, y));
        }

        if (noiseOverlay)
            height += NoiseOverlay(x, y);
        return height;
    }

    public double[,] CalculateHeightMap(int resolutionX, int resolutionY, Rect2I bounds, bool parallel = false,
        int upscaleLevel = 2)
    {
        if (State != WorldGenerationState.Completed)
            throw new InvalidOperationException("World generation is not completed yet.");

        return HeightMapUtils.ConstructHeightMap(resolutionX, resolutionY, bounds, (x, y) => GetRawHeight(x, y),
            parallel, upscaleLevel);
    }

    public double[,] CalculateFullHeightMap(int resolutionX, int resolutionY)
    {
        return CalculateHeightMap(resolutionX, resolutionY, Settings.Bounds, true);
    }

    public ImageTexture GetFullHeightMapImageTexture(int resolutionX, int resolutionY)
    {
        var heightMap = CalculateFullHeightMap(resolutionX, resolutionY);
        var image = Image.CreateEmpty(resolutionX, resolutionY, false, Image.Format.Rgb8);
        for (var x = 0; x < resolutionX; x++)
            for (var y = 0; y < resolutionY; y++)
            {
                var h = (float)(0.5 * (1 + heightMap[x, y] / MaxHeight));
                image.SetPixel(x, y, new Color(h, h, h));
            }

        return ImageTexture.CreateFromImage(image);
    }

    #endregion

    #region ChunkColumn Generation

    public virtual double[,] CalculateChunkHeightMap(Vector2I chunkColumnIndex, Func<double, double, double> getHeight)
    {
        if (State != WorldGenerationState.Completed)
            throw new InvalidOperationException("World generation is not completed yet.");

        var rect = new Rect2I(chunkColumnIndex * ChunkMesher.CS, ChunkMesher.CS, ChunkMesher.CS);
        return HeightMapUtils.ConstructChunkHeightMap(rect, getHeight, 2);
    }

    public override ChunkColumn GenerateChunkColumn(Vector2I chunkColumnIndex)
    {
        // Biome
        var biomePalette = new Palette<Biome>(BiomeLibrary.Instance.GetBiome("plain"));
        var biomePaletteStorage = new PaletteStorage<Biome>(biomePalette);

        for (var x = 0; x < ChunkColumn.BIOME_MAP_SIZE; x++)
            for (var z = 0; z < ChunkColumn.BIOME_MAP_SIZE; z++)
            {
                var point = chunkColumnIndex * ChunkMesher.CS +
                            new Vector2(x, z) * ChunkMesher.CS / (ChunkColumn.BIOME_MAP_SIZE - 1);
                point = Warp(point, _domainWarpPattern);

                var cell = GetCellDatasNearby(point).First();
                biomePaletteStorage.Set(ChunkColumn.GetBiomeIndex(x, z), cell.Biome);
            }

        var chunkColumn = new ChunkColumn(chunkColumnIndex, biomePaletteStorage);

        // Height map
        var getHeight = new Func<double, double, double>((x, y) =>
        {
            var height = GetRawHeight(x, y, true, true);

            var biomeWeights = chunkColumn.GetBiomeWeights(x, y);
            foreach (var (biome, weight) in biomeWeights)
                // TODO: Use the biome's pattern ?
                height += weight * PatternLibrary.GetPattern(biome.Id).Evaluate(x, y, Settings.Seed);

            return height;
        });

        var heightMap = CalculateChunkHeightMap(chunkColumnIndex, getHeight);
        chunkColumn.SetHeightMap(heightMap);
        return chunkColumn;
    }

    #endregion
}