using Godot;

namespace WorldGenerator;

public enum PlateType
{
    Continent,
    Oceans,
}

public partial class WorldGenerator
{
    private PlateType RandomPlateType(RandomNumberGenerator rng)
    {
        if (rng.Randf() < 1 - Settings.ContinentRatio)
            return PlateType.Oceans;

        return PlateType.Continent;
    }

    public double MergeNoiseValue(double value)
    {
        if (Settings.PlateMergeRatio > 0)
        {
            var normalized = (value + 1) * 0.5;
            return 2 * Mathf.Floor(normalized / Settings.PlateMergeRatio) * Settings.PlateMergeRatio - 1;
        }

        return value;
    }

    private void InitializeTectonicProperties()
    {
        ReportProgress("Initializing tectonic properties");
        _streamGraph.Clear();
        using var rng = new RandomNumberGenerator();
        foreach (var (i, cellData) in _cellDatas)
        {
            var pos = UniformPosition(_points[i]);
            var mappedX = 2 * Mathf.Pi * pos.X / Settings.Bounds.Size.X;
            var noiseValue = _plateNoise.GetNoise3D(Mathf.Cos(mappedX) * Settings.Bounds.Size.X * 0.5 / Mathf.Pi,
                Mathf.Sin(mappedX) * Settings.Bounds.Size.X * 0.5 / Mathf.Pi, pos.Y);
            var seed = MergeNoiseValue(noiseValue).ToString().Hash();
            rng.Seed = seed;
            var r = rng.Randf() * Settings.MaxTectonicMovement;
            var phi = rng.Randf() * Mathf.Pi * 2;
            cellData.TectonicMovement = new Vector2(Mathf.Cos(phi), Mathf.Sin(phi)) * r;
            cellData.PlateType = RandomPlateType(rng);
            cellData.PlateSeed = seed;

            if (cellData.PlateType == PlateType.Continent && ((Rect2)Settings.Bounds).HasPoint(_points[i]))
                _streamGraph.Add(cellData);
        }
    }
}
