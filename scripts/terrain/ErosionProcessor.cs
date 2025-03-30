public class ErosionProcessor
{
    private readonly int _size;
    private readonly float _talusThreshold;

    public ErosionProcessor(int chunkSize)
    {
        _size = chunkSize;
        _talusThreshold = 16f / chunkSize; // 文献推荐的T=16/N
    }

    public float[,] ApplyErosion(float[,] heightmap)
    {
        var eroded = (float[,])heightmap.Clone();

        // 文献建议50次迭代
        for (var i = 0; i < 50; i++) IterateErosion(eroded);

        return eroded;
    }

    private void IterateErosion(float[,] heightmap)
    {
        for (var x = 0; x < _size; x++)
        for (var z = 0; z < _size; z++)
            ProcessCell(heightmap, x, z);
    }

    private void ProcessCell(float[,] heightmap, int x, int z)
    {
        var currentHeight = heightmap[x, z];
        float maxDelta = 0;
        int lowestX = x, lowestZ = z;

        // 检查4邻域（Von Neumann）
        CheckNeighbor(heightmap, x, z, x + 1, z, ref maxDelta, ref lowestX, ref lowestZ);
        CheckNeighbor(heightmap, x, z, x - 1, z, ref maxDelta, ref lowestX, ref lowestZ);
        CheckNeighbor(heightmap, x, z, x, z + 1, ref maxDelta, ref lowestX, ref lowestZ);
        CheckNeighbor(heightmap, x, z, x, z - 1, ref maxDelta, ref lowestX, ref lowestZ);

        if (maxDelta > 0 && maxDelta <= _talusThreshold)
        {
            var delta = maxDelta * 0.5f;
            heightmap[x, z] -= delta;
            heightmap[lowestX, lowestZ] += delta;
        }
    }

    private void CheckNeighbor(float[,] map, int x, int z,
        int nx, int nz,
        ref float maxDelta,
        ref int lowestX, ref int lowestZ)
    {
        if (nx < 0 || nx >= _size || nz < 0 || nz >= _size) return;

        var delta = map[x, z] - map[nx, nz];
        if (delta > maxDelta)
        {
            maxDelta = delta;
            lowestX = nx;
            lowestZ = nz;
        }
    }
}