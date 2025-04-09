using Godot;

namespace ChunkGenerator;

public static class TestNoiseGenerator
{
    private static readonly FastNoiseLite _noise = new();

    static TestNoiseGenerator()
    {
        _noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
        _noise.Frequency = 0.01;
        _noise.FractalOctaves = 3;
    }

    public static double GetHeight(float x, float z)
    {
        return 50 + _noise.GetNoise2D(x, z) * 40;
    }
}

public enum GenerationState
{
    NotStarted,
    Initializing,
    HeightMap,
    Meshing,
    Custom,
    Completed,
    Failed
}

public partial class ChunkGenerationRequest
{
    private uint[] _voxels;
    private ChunkMesher.MeshData _meshData;
    private ChunkData _chunkData;
    private Mesh _mesh;
    private Shape3D _shape;

    private void InitializePipeline()
    {
        _generationPipeline.AddLast(new GenerationStep(GenerationState.Initializing, Initialize));
        // _generationPipeline.AddLast(new GenerationStep(GenerationState.Custom, TerrainTest));
        _generationPipeline.AddLast(new GenerationStep(GenerationState.HeightMap, GetHeightmap));
        _generationPipeline.AddLast(new GenerationStep(GenerationState.Meshing, Meshing));
    }

    private void Initialize()
    {
        _voxels = new uint[ChunkMesher.CS_P3];
        _meshData = new ChunkMesher.MeshData();
    }

    // TODO: Optimize this
    private void GetHeightmap()
    {
        var rect = new Rect2(ChunkPosition.X * ChunkMesher.CS, ChunkPosition.Z * ChunkMesher.CS, ChunkMesher.CS_P, ChunkMesher.CS_P);
        var heightMap = _worldGenerator.CalculateHeightMap(ChunkMesher.CS_P, ChunkMesher.CS_P, rect);
        for (var x = 0; x < ChunkMesher.CS_P; x++)
            for (var z = 0; z < ChunkMesher.CS_P; z++)
            {
                var height = (int)heightMap[x, z];

                for (var y = 0; y < ChunkMesher.CS_P; y++)
                {
                    var actualY = ChunkPosition.Y * ChunkMesher.CS + y;
                    if (actualY < height - ChunkMesher.CS)
                    {
                        if (actualY == height - ChunkMesher.CS - 1)
                            _voxels[ChunkMesher.GetIndex(x, y, z)] = 4; // GD.Randi() % 4 + 1;
                        else if (actualY > height - ChunkMesher.CS - 4)
                            _voxels[ChunkMesher.GetIndex(x, y, z)] = 3;
                        else
                            _voxels[ChunkMesher.GetIndex(x, y, z)] = 2;
                        ChunkMesher.AddOpaqueVoxel(ref _meshData.OpaqueMask, x, y, z);
                    }
                }
            }
    }

    private void TerrainTest()
    {
        for (var x = 0; x < ChunkMesher.CS_P; x++)
            for (var z = 0; z < ChunkMesher.CS_P; z++)
            {
                var height = (int)TestNoiseGenerator.GetHeight(
                    ChunkPosition.X * ChunkMesher.CS + x,
                    ChunkPosition.Z * ChunkMesher.CS + z
                );

                for (var y = 0; y < ChunkMesher.CS_P; y++)
                {
                    var actualY = ChunkPosition.Y * ChunkMesher.CS + y;
                    if (actualY < height - ChunkMesher.CS)
                    {
                        if (actualY == height - ChunkMesher.CS - 1)
                            _voxels[ChunkMesher.GetIndex(x, y, z)] = 4; // GD.Randi() % 4 + 1;
                        else if (actualY > height - ChunkMesher.CS - 4)
                            _voxels[ChunkMesher.GetIndex(x, y, z)] = 3;
                        else
                            _voxels[ChunkMesher.GetIndex(x, y, z)] = 2;
                        ChunkMesher.AddOpaqueVoxel(ref _meshData.OpaqueMask, x, y, z);
                    }
                }
            }
    }

    private void Meshing()
    {
        ChunkMesher.MeshVoxels(_voxels, _meshData);

        _chunkData = new ChunkData(ChunkPosition);
        _chunkData.Voxels = _voxels;

        _mesh = ChunkMesher.GenerateMesh(_meshData);
        _shape = _mesh?.CreateTrimeshShape();
    }
}