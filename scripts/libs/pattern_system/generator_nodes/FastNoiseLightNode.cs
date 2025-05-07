using Godot;

namespace PatternSystem;

public class FastNoiseLiteNode : PatternTreeNode
{
    private readonly FastNoiseLite _noiseGenerator;
    private FastNoiseLiteSettings _settings;
    public FastNoiseLiteSettings Settings => _settings;

    public override void SetSeed(int seed)
    {
        _settings.Seed = seed;
        _noiseGenerator.SetSeed(seed);
    }

    public FastNoiseLiteNode(FastNoiseLite fastNoiseLite)
    {
        _noiseGenerator = fastNoiseLite;
        _settings = new FastNoiseLiteSettings
        {
            CellularDistanceFunction = (CellularDistanceFunction)_noiseGenerator.CellularDistanceFunction,
            CellularJitter = _noiseGenerator.CellularJitter,
            CellularReturnType = (CellularReturnType)_noiseGenerator.CellularReturnType,
            DomainWarpAmplitude = _noiseGenerator.DomainWarpAmplitude,
            DomainWarpEnabled = _noiseGenerator.DomainWarpEnabled,
            DomainWarpFractalGain = _noiseGenerator.DomainWarpFractalGain,
            DomainWarpFractalLacunarity = _noiseGenerator.DomainWarpFractalLacunarity,
            DomainWarpFractalOctaves = _noiseGenerator.DomainWarpFractalOctaves,
            DomainWarpFractalType = (DomainWarpFractalType)_noiseGenerator.DomainWarpFractalType,
            DomainWarpFrequency = _noiseGenerator.DomainWarpFrequency,
            DomainWarpType = (DomainWarpType)_noiseGenerator.DomainWarpType,
            FractalGain = _noiseGenerator.FractalGain,
            FractalLacunarity = _noiseGenerator.FractalLacunarity,
            FractalOctaves = _noiseGenerator.FractalOctaves,
            FractalPingPongStrength = _noiseGenerator.FractalPingPongStrength,
            FractalType = (FractalType)_noiseGenerator.FractalType,
            FractalWeightedStrength = _noiseGenerator.FractalWeightedStrength,
            Frequency = _noiseGenerator.Frequency,
            NoiseType = (NoiseType)_noiseGenerator.NoiseType,
            Offset = _noiseGenerator.Offset,
            Seed = _noiseGenerator.Seed
        };
    }

    public FastNoiseLiteNode(FastNoiseLiteSettings settings)
    {
        _settings = settings;
        _noiseGenerator = new FastNoiseLite();
        _noiseGenerator.SetCellularDistanceFunction(
            (FastNoiseLite.CellularDistanceFunctionEnum)settings.CellularDistanceFunction);
        _noiseGenerator.SetCellularJitter(settings.CellularJitter);
        _noiseGenerator.SetCellularReturnType((FastNoiseLite.CellularReturnTypeEnum)settings.CellularReturnType);
        _noiseGenerator.SetDomainWarpAmplitude(settings.DomainWarpAmplitude);
        _noiseGenerator.SetDomainWarpEnabled(settings.DomainWarpEnabled);
        _noiseGenerator.SetDomainWarpFractalGain(settings.DomainWarpFractalGain);
        _noiseGenerator.SetDomainWarpFractalLacunarity(settings.DomainWarpFractalLacunarity);
        _noiseGenerator.SetDomainWarpFractalOctaves(settings.DomainWarpFractalOctaves);
        _noiseGenerator.SetDomainWarpFractalType(
            (FastNoiseLite.DomainWarpFractalTypeEnum)settings.DomainWarpFractalType);
        _noiseGenerator.SetDomainWarpFrequency(settings.DomainWarpFrequency);
        _noiseGenerator.SetDomainWarpType((FastNoiseLite.DomainWarpTypeEnum)settings.DomainWarpType);
        _noiseGenerator.SetFractalGain(settings.FractalGain);
        _noiseGenerator.SetFractalLacunarity(settings.FractalLacunarity);
        _noiseGenerator.SetFractalOctaves(settings.FractalOctaves);
        _noiseGenerator.SetFractalPingPongStrength(settings.FractalPingPongStrength);
        _noiseGenerator.SetFractalType((FastNoiseLite.FractalTypeEnum)settings.FractalType);
        _noiseGenerator.SetFractalWeightedStrength(settings.FractalWeightedStrength);
        _noiseGenerator.SetFrequency(settings.Frequency);
        _noiseGenerator.SetNoiseType((FastNoiseLite.NoiseTypeEnum)settings.NoiseType);
        _noiseGenerator.SetOffset(settings.Offset);
        _noiseGenerator.SetSeed(settings.Seed);
    }

    public override double Evaluate(double x, double y)
    {
        return _noiseGenerator.GetNoise2D(x, y);
    }

    public override double Evaluate(double x, double y, double z)
    {
        return _noiseGenerator.GetNoise3D(x, y, z);
    }
}

public class FastNoiseLiteSettings
{
    /// <summary>
    /// Default: <c>CellularDistanceFunction.Euclidean</c>
    /// </summary>
    public CellularDistanceFunction CellularDistanceFunction = CellularDistanceFunction.Euclidean;

    /// <summary>
    /// Default: <c>1.0</c>
    /// </summary>
    public double CellularJitter = 1.0;

    /// <summary>
    /// Default: <c>CellularReturnType.Distance</c>
    /// </summary>
    public CellularReturnType CellularReturnType = CellularReturnType.Distance;

    /// <summary>
    /// Default: <c>30.0</c>
    /// </summary>
    public double DomainWarpAmplitude = 30.0;

    /// <summary>
    /// Default: <c>false</c>
    /// </summary>
    public bool DomainWarpEnabled = false;

    /// <summary>
    /// Default: <c>0.5</c>
    /// </summary>
    public double DomainWarpFractalGain = 0.5;

    /// <summary>
    /// Default: <c>6.0</c>
    /// </summary>
    public double DomainWarpFractalLacunarity = 6.0;

    /// <summary>
    /// Default: <c>5</c>
    /// </summary>
    public int DomainWarpFractalOctaves = 5;

    /// <summary>
    /// Default: <c>DomainWarpFractalType.Progressive</c>
    /// </summary>
    public DomainWarpFractalType DomainWarpFractalType = DomainWarpFractalType.Progressive;

    /// <summary>
    /// Default: <c>0.05</c>
    /// </summary>
    public double DomainWarpFrequency = 0.05;

    /// <summary>
    /// Default: <c>DomainWarpType.Simplex</c>
    /// </summary>
    public DomainWarpType DomainWarpType = DomainWarpType.Simplex;

    /// <summary>
    /// Default: <c>0.5</c>
    /// </summary>
    public double FractalGain = 0.5;

    /// <summary>
    /// Default: <c>2.0</c>
    /// </summary>
    public double FractalLacunarity = 2.0;

    /// <summary>
    /// Default: <c>5</c>
    /// </summary>
    public int FractalOctaves = 5;

    /// <summary>
    /// Default: <c>2.0</c>
    /// </summary>
    public double FractalPingPongStrength = 2.0;

    /// <summary>
    /// Default: <c>FractalType.Fbm</c>
    /// </summary>
    public FractalType FractalType = FractalType.Fbm;

    /// <summary>
    /// Default: <c>0.0</c>
    /// </summary>
    public double FractalWeightedStrength = 0.0;

    /// <summary>
    /// Default: <c>0.01</c>
    /// </summary>
    public double Frequency = 0.01;

    /// <summary>
    /// Default: <c>NoiseType.SimplexSmooth</c>
    /// </summary>
    public NoiseType NoiseType = NoiseType.SimplexSmooth;

    /// <summary>
    /// Default: <c>Vector3.Zero</c>
    /// </summary>
    public Vector3 Offset = Vector3.Zero;

    /// <summary>
    /// Default: <c>0</c>
    /// </summary>
    public int Seed = 0;
}

public enum NoiseType
{
    Simplex,
    SimplexSmooth,
    Cellular,
    Perlin,
    ValueCubic,
    Value
}

public enum FractalType
{
    None,
    Fbm,
    Ridged,
    PingPong
}

public enum CellularDistanceFunction
{
    Euclidean,
    EuclideanSquared,
    Manhattan,
    Hybrid
}

public enum CellularReturnType
{
    CellValue,
    Distance,
    Distance2,
    Distance2Add,
    Distance2Sub,
    Distance2Mul,
    Distance2Div
}

public enum DomainWarpType
{
    Simplex,
    SimplexReduced,
    BasicGrid
}

public enum DomainWarpFractalType
{
    None,
    Progressive,
    Independent
}