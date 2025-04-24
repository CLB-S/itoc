using Godot;

namespace PatternSystem;

public class FastNoiseLiteNode : PatternTreeNode
{
    private readonly FastNoiseLite _noiseGenerator;
    private FastNoiseLiteSettings _settings;
    public FastNoiseLiteSettings Settings => _settings;

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
    public CellularDistanceFunction CellularDistanceFunction = CellularDistanceFunction.Euclidean;
    public double CellularJitter = 1.0;
    public CellularReturnType CellularReturnType = CellularReturnType.Distance;
    public double DomainWarpAmplitude = 30.0;
    public bool DomainWarpEnabled = false;
    public double DomainWarpFractalGain = 0.5;
    public double DomainWarpFractalLacunarity = 6.0;
    public int DomainWarpFractalOctaves = 5;
    public DomainWarpFractalType DomainWarpFractalType = DomainWarpFractalType.Progressive;
    public double DomainWarpFrequency = 0.05;
    public DomainWarpType DomainWarpType = DomainWarpType.Simplex;
    public double FractalGain = 0.5;
    public double FractalLacunarity = 2.0;
    public int FractalOctaves = 5;
    public double FractalPingPongStrength = 2.0;
    public FractalType FractalType = FractalType.Fbm;
    public double FractalWeightedStrength = 0.0;
    public double Frequency = 0.01;
    public NoiseType NoiseType = NoiseType.SimplexSmooth;
    public Vector3 Offset = Vector3.Zero;
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