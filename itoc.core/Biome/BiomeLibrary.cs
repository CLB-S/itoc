using Godot;

public class BiomeLibrary
{
    private static BiomeLibrary _instance;
    private readonly Dictionary<string, Biome> _biomes = new();

    public static BiomeLibrary Instance => _instance ??= new BiomeLibrary();

    private BiomeLibrary() => RegisterDefaultBiomes();

    private void RegisterDefaultBiomes()
    {
        RegisterBiome(
            new Biome("mountain", -1e9, 1e9, -1e9, 1e9, 200, 2000, new Color(0.6f, 0.6f, 0.6f))
        );
        RegisterBiome(
            new Biome("hill", -1e9, 1e9, -1e9, 1e9, 100, 200, new Color(0.4f, 0.8f, 0.4f))
        );
        RegisterBiome(
            new Biome("plain", -1e9, 1e9, -1e9, 1e9, 0, 100, new Color(0.5f, 0.8f, 0.3f))
        );
    }

    public void RegisterBiome(Biome biome)
    {
        if (!_biomes.TryAdd(biome.Id, biome))
            throw new ArgumentException($"Biome with id {biome.Id} already exists!");
    }

    public Biome GetBiome(string id)
    {
        if (_biomes.TryGetValue(id, out var biome))
            return biome;

        GD.PrintErr($"Biome with id {id} not found!");
        return null;
    }

    public Biome GetBiomeForConditions(double temperature, double precipitation, double height)
    {
        var matchingBiomes = _biomes
            .Values.Where(b => b.MatchesConditions(temperature, precipitation, height))
            .ToList();

        if (matchingBiomes.Count == 0)
            return FindClosestBiome(temperature, precipitation, height);

        return matchingBiomes[0];
    }

    private Biome FindClosestBiome(double temperature, double precipitation, double height)
    {
        var minDistance = double.MaxValue;
        Biome closestBiome = null;

        foreach (var biome in _biomes.Values)
        {
            var tempDistance = Math.Min(
                Math.Abs(temperature - biome.MinTemperature),
                Math.Abs(temperature - biome.MaxTemperature)
            );
            var precipDistance = Math.Min(
                Math.Abs(precipitation - biome.MinPrecipitation),
                Math.Abs(precipitation - biome.MaxPrecipitation)
            );
            var heightDistance = Math.Min(
                Math.Abs(height - biome.MinHeight),
                Math.Abs(height - biome.MaxHeight)
            );

            // TODO: Adjust weights based on biome characteristics
            var distance = tempDistance * 2.0 + precipDistance * 1.5 + heightDistance * 1.0;

            if (distance < minDistance)
            {
                minDistance = distance;
                closestBiome = biome;
            }
        }

        return closestBiome;
    }
}
