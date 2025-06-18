using Godot;
using ITOC.Core.Utils;

namespace ITOC.Core.WorldGeneration.Vanilla;

public class VanillaChunkGenerator : ChunkGeneratorBase
{
    private readonly VanillaWorldGenerator _generator;
    private readonly MultiPassGenerationController _multiPassController;

    public VanillaChunkGenerator(ChunkManager chunkManager, VanillaWorldGenerator generator)
        : base(chunkManager)
    {
        _generator = generator ?? throw new ArgumentNullException(nameof(generator));

        var pass0 = new VanillaChunkColumnGenerationPass0(this, _chunkManager);
        var pass1 = new VanillaChunkColumnGenerationPass1(_chunkManager);

        _multiPassController = new(pass0, pass1);
        _multiPassController.AllPassesCompleted += (_, e) => NotifySurfaceChunksReady(e);
    }

    public ChunkColumn GenerateChunkColumnMetadata(Vector2I chunkColumnIndex)
    {
        // Biome
        var defaultBiome = BiomeLibrary.Instance.GetBiome("plain");
        var size = ChunkColumn.BIOME_MAP_SIZE * ChunkColumn.BIOME_MAP_SIZE;
        var biomes = new PaletteArray<Biome>(size, defaultBiome);

        for (var x = 0; x < ChunkColumn.BIOME_MAP_SIZE; x++)
            for (var z = 0; z < ChunkColumn.BIOME_MAP_SIZE; z++)
            {
                var point = chunkColumnIndex * Chunk.SIZE +
                            new Vector2(x, z) * Chunk.SIZE / (ChunkColumn.BIOME_MAP_SIZE - 1);
                point = _generator.Warp(point);

                var cell = _generator.GetCellDatasNearby(point).First();
                biomes[ChunkColumn.GetBiomeIndex(x, z)] = cell.Biome;
            }

        var chunkColumn = new ChunkColumn(chunkColumnIndex, biomes);

        // Height map
        var getHeight = new Func<double, double, double>((x, y) =>
        {
            var height = _generator.GetRawHeight(x, y, true, true);

            var biomeWeights = chunkColumn.GetBiomeWeights(x, y);
            foreach (var (biome, weight) in biomeWeights)
                // TODO: Use the biome's pattern ?
                height += weight * _generator.PatternLibrary.GetPattern(biome.Id).Evaluate(x, y, _generator.Settings.Seed);

            return height;
        });

        var heightMap = _generator.CalculateChunkHeightMap(chunkColumnIndex, getHeight);
        chunkColumn.SetHeightMap(heightMap);
        return chunkColumn;
    }

    protected override void GenerateSurfaceChunks(Vector2I chunkColumnIndex)
    {
        _multiPassController.GenerateAt(chunkColumnIndex);
    }
}