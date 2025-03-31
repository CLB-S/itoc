using System;
using System.Collections.Concurrent;
using System.Threading;
using Godot;

public static class NoiseGenerator
{
    private static readonly FastNoiseLite _noise = new();

    static NoiseGenerator()
    {
        _noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
        _noise.Frequency = 0.01f;
        _noise.FractalOctaves = 3;
    }

    public static float GetHeight(float x, float z)
    {
        return 50 + _noise.GetNoise2D(x, z) * 40;
    }
}


public class ChunkGenerationRequest
{
    public ChunkGenerationRequest(Vector3I position, Action<ChunkGenerationResult> callback)
    {
        ChunkPosition = position;
        Callback = callback;
    }

    public Vector3I ChunkPosition { get; }
    public Action<ChunkGenerationResult> Callback { get; }
}

public class ChunkGenerationResult
{
    public ChunkGenerationResult(Vector3I pos, ChunkMesher.MeshData data, uint[] voxels)
    {
        ChunkPosition = pos;
        MeshData = data;
        VoxelData = voxels;
    }

    public Vector3I ChunkPosition { get; }
    public ChunkMesher.MeshData MeshData { get; }
    public uint[] VoxelData { get; }
}


public class ChunkGenerator : IDisposable
{
    private readonly ConcurrentQueue<ChunkGenerationRequest> _queue = new();

    private readonly Thread _workerThread;
    private bool _running = true;

    public ChunkGenerator()
    {
        _workerThread = new Thread(ProcessQueue);
    }

    public void Dispose()
    {
        Stop();
    }

    public void Start()
    {
        _workerThread.Start();
    }

    public void Enqueue(ChunkGenerationRequest request)
    {
        _queue.Enqueue(request);
    }

    private void ProcessQueue()
    {
        while (_running)
            if (_queue.TryDequeue(out var request))
                try
                {
                    var result = GenerateChunkData(request);
                    request.Callback?.Invoke(result);
                }
                catch (Exception e)
                {
                    GD.PrintErr($"Chunk generation failed: {e}");
                }
            else
                Thread.Sleep(10); // 避免忙等待
    }

    private ChunkGenerationResult GenerateChunkData(ChunkGenerationRequest request)
    {
        // GD.Print($"Generating chunk {request.ChunkPosition}");

        // 生成地形数据（示例使用噪声生成）
        var voxels = new uint[ChunkMesher.CS_P3];
        var meshData = new ChunkMesher.MeshData();

        // 使用噪声生成地形（示例）
        for (var x = 0; x < ChunkMesher.CS_P; x++)
            for (var z = 0; z < ChunkMesher.CS_P; z++)
            {
                var height = (int)NoiseGenerator.GetHeight(
                    request.ChunkPosition.X * ChunkMesher.CS + x,
                    request.ChunkPosition.Z * ChunkMesher.CS + z
                );

                for (var y = 0; y < ChunkMesher.CS_P; y++)
                {
                    var actualY = request.ChunkPosition.Y * ChunkMesher.CS + y;
                    if (actualY < height - ChunkMesher.CS)
                    {
                        if (actualY == height - ChunkMesher.CS - 1)
                            voxels[ChunkMesher.GetIndex(x, y, z)] = 4;
                        else if (actualY > height - ChunkMesher.CS - 4)
                            voxels[ChunkMesher.GetIndex(x, y, z)] = 3;
                        else
                            voxels[ChunkMesher.GetIndex(x, y, z)] = 2;
                        ChunkMesher.AddNonOpaqueVoxel(ref meshData.OpaqueMask, x, y, z);
                    }
                }
            }

        // for (int x = 0; x < ChunkMesher.CS_P; x++)
        //     for (int y = 0; y < ChunkMesher.CS_P; y++)
        //         for (int z = 0; z < ChunkMesher.CS_P; z++)
        //         {
        //             if ((new Vector3I(x, y, z) - Vector3I.One * ChunkMesher.CS_P / 2).Length() < 10.0f)
        //             {
        //                 voxels[ChunkMesher.GetZXYIndex(x, y, z)] = 8;
        //                 ChunkMesher.AddNonOpaqueVoxel(ref meshData.OpaqueMask, x, y, z);
        //             }
        //         }

        // for (int x = 0; x < 10; x++)
        //     for (int y = 0; y < 10; y++)
        //         for (int z = 0; z < 10; z++)
        //         {
        //             if (x % 2 == 0 && y % 2 == 0 && z % 2 == 0)
        //             {
        //                 voxels[ChunkMesher.GetZXYIndex(x, y, z)] = 8;
        //                 ChunkMesher.AddNonOpaqueVoxel(ref meshData.OpaqueMask, x, y, z);
        //             }
        //         }

        // var voxelPositions = new Vector3I[]
        // {
        //     new (1,1,1),
        //     new (1,2,1),
        //     new (1,3,1),

        //     new (3,1,1),
        //     new (3,1,2),

        //     new (2,1,5),
        //     new (3,1,5),
        // };

        // foreach (var pos in voxelPositions)
        // {
        //     voxels[ChunkMesher.GetIndex(pos.X, pos.Y, pos.Z)] = 1;
        //     ChunkMesher.AddNonOpaqueVoxel(ref meshData.OpaqueMask, pos.X, pos.Y, pos.Z);
        // }


        // 生成网格数据
        ChunkMesher.MeshVoxels(voxels, meshData);

        return new ChunkGenerationResult(
            request.ChunkPosition,
            meshData,
            voxels
        );
    }

    public void Stop()
    {
        _running = false;
        _workerThread.Join();
    }
}