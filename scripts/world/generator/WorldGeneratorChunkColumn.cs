using System;
using System.Linq;
using Godot;
using Palette;

namespace WorldGenerator;

public partial class WorldGenerator
{
    public double[,] CalculateChunkHeightMap(Vector2I chunkColumnPos, Func<double, double, double> getHeight)
    {
        if (State != GenerationState.Completed)
            throw new InvalidOperationException("World generation is not completed yet.");

        var rect = new Rect2I(chunkColumnPos * ChunkMesher.CS, ChunkMesher.CS, ChunkMesher.CS);
        return HeightMapUtils.ConstructChunkHeightMap(rect, getHeight, 2);
    }

    public ChunkColumn GenerateChunkColumn(Vector2I chunkColumnPos)
    {
        // Biome
        var biomePalette = new Palette<Biome>(BiomeLibrary.Instance.GetBiome("plain"));
        var biomePaletteStorage = new PaletteStorage<Biome>(biomePalette);

        for (var x = 0; x < ChunkColumn.BIOME_MAP_SIZE; x++)
            for (var z = 0; z < ChunkColumn.BIOME_MAP_SIZE; z++)
            {
                var point = chunkColumnPos * ChunkMesher.CS +
                            new Vector2(x, z) * ChunkMesher.CS / (ChunkColumn.BIOME_MAP_SIZE - 1);
                point = Warp(point, _domainWarpPattern);

                var cell = GetCellDatasNearby(point).First();
                biomePaletteStorage.Set(ChunkColumn.GetBiomeIndex(x, z), cell.Biome);
            }

        var chunkColumn = new ChunkColumn(chunkColumnPos, biomePaletteStorage);

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

        var heightMap = CalculateChunkHeightMap(chunkColumnPos, getHeight);
        chunkColumn.SetHeightMap(heightMap);
        return chunkColumn;
    }
}