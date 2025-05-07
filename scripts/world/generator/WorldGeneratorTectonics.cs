using System.Threading.Tasks;
using Godot;

namespace WorldGenerator;

public enum PlateType
{
    Continent,
    Oceans
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

    protected void InitializeTectonicProperties()
    {
        ReportProgress("Initializing tectonic properties");
        _streamGraph.Clear();

        // Use Parallel.ForEach to process cells in parallel
        Parallel.ForEach(_cellDatas, cellDataPair =>
        {
            var i = cellDataPair.Key;
            var cellData = cellDataPair.Value;

            // Create a thread-local RNG instance
            using var rng = new RandomNumberGenerator();

            var pos = _points[i];
            var noiseValue = _platePattern.EvaluateSeamlessX(pos, Settings.Bounds);
            var seed = MergeNoiseValue(noiseValue).ToString().Hash();
            rng.Seed = seed;
            var r = rng.Randf() * Settings.MaxTectonicMovement;
            var phi = rng.Randf() * Mathf.Pi * 2;
            cellData.TectonicMovement = new Vector2(Mathf.Cos(phi), Mathf.Sin(phi)) * r;
            cellData.PlateType = RandomPlateType(rng);
            cellData.PlateSeed = seed;
        });

        // After parallel processing, add continental cells to the stream graph
        foreach (var (i, cellData) in _cellDatas)
            if (cellData.PlateType == PlateType.Continent)
                _streamGraph.Add(cellData);
    }
}