using Godot;
using System;
using System.Collections.Generic;

public partial class Chunk : StaticBody3D
{
    // 区块状态枚举
    public enum ChunkState
    {
        Unloaded,
        DataReady,
        MeshReady,
        Loaded
    }

    // 公开属性
    public Vector3 ChunkPosition { get; private set; }
    public ChunkState State { get; private set; } = ChunkState.Unloaded;
    public int[] VoxelData { get; private set; }
    public ChunkMesher.MeshData MeshData { get; private set; }

    // 节点引用
    private MeshInstance3D _meshInstance;
    private CollisionShape3D _collisionShape;
    private ArrayMesh _arrayMesh;

    // 调试显示
    private ImmediateMesh _debugMesh;
    private MeshInstance3D _debugMeshInstance;

    public Chunk(ChunkGenerationResult result)
    {
        Initialize(result.ChunkPosition, result.VoxelData, result.MeshData);
    }

    // 初始化区块
    public void Initialize(Vector3 chunkPosition, int[] voxelData, ChunkMesher.MeshData meshData)
    {
        ChunkPosition = chunkPosition;
        VoxelData = voxelData;
        MeshData = meshData;
        State = ChunkState.DataReady;

        Position = chunkPosition * World.ChunkSize;
        Name = $"Chunk_{chunkPosition.X}_{chunkPosition.Y}_{chunkPosition.Z}";
    }

    // 主线程加载区块
    public void Load()
    {
        if (State == ChunkState.Loaded) return;

        CallDeferred(nameof(DeferredLoad));
    }

    private void DeferredLoad()
    {
        if (State == ChunkState.Loaded) return;

        // 创建网格实例
        _meshInstance = new MeshInstance3D();

        if (World.Instance.UseDebugMaterial)
        {
            _meshInstance.MaterialOverride = World.Instance.DebugMaterial;
        }

        AddChild(_meshInstance);

        // 生成Godot网格
        GenerateGodotMesh();

        // 设置碰撞体
        SetupCollision();

        // 调试边框
        if (World.Instance.DebugDrawChunkBounds)
        {
            DrawDebugBounds();
        }

        State = ChunkState.Loaded;
    }

    private class SurfaceArrayData
    {
        public List<Vector3> Vertices = new List<Vector3>();
        public List<Vector2> UVs = new List<Vector2>();
        public List<Vector3> Normals = new List<Vector3>();
        public List<int> Indices = new List<int>();

        public Godot.Collections.Array GetSurfaceArray()
        {
            var surfaceArray = new Godot.Collections.Array();
            surfaceArray.Resize((int)Mesh.ArrayType.Max);

            surfaceArray[(int)Mesh.ArrayType.Vertex] = Vertices.ToArray();
            surfaceArray[(int)Mesh.ArrayType.TexUV] = UVs.ToArray();
            surfaceArray[(int)Mesh.ArrayType.Normal] = Normals.ToArray();
            surfaceArray[(int)Mesh.ArrayType.Index] = Indices.ToArray();

            return surfaceArray;
        }
    }

    // 生成Godot可用的ArrayMesh
    private void GenerateGodotMesh()
    {
        if (MeshData.Quads.Count == 0) return;

        var surfaceArrayDict = new Dictionary<int, SurfaceArrayData>();

        for (int face = 0; face < 6; face++)
        {
            for (int i = MeshData.FaceVertexBegin[face]; i < MeshData.FaceVertexBegin[face] + MeshData.FaceVertexLength[face]; i++)
            {
                ParseQuad((Direction)face, MeshData.QuadBlockIDs[i], MeshData.Quads[i], surfaceArrayDict);

            }
        }

        _arrayMesh = new ArrayMesh();
        foreach (var (blockID, surfaceArrayData) in surfaceArrayDict)
        {
            _arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArrayData.GetSurfaceArray());
            _arrayMesh.SurfaceSetMaterial(_arrayMesh.GetSurfaceCount() - 1,
                BlockManager.Instance.GetBlock(blockID).GetMaterial());
        }

        _meshInstance.Mesh = _arrayMesh;
    }

    private void ParseQuad(Direction dir, int blockID, ulong quad, Dictionary<int, SurfaceArrayData> surfaceArrayDict)
    {
        if (!surfaceArrayDict.ContainsKey(blockID))
            surfaceArrayDict.Add(blockID, new SurfaceArrayData());

        var surfaceArrayData = surfaceArrayDict[blockID];

        // 解析数据（与C++结构完全一致）
        uint x = (uint)(quad & 0x3F) + 1;        // 6 bits
        uint y = (uint)((quad >> 6) & 0x3F) + 1; // 6 bits
        uint z = (uint)((quad >> 12) & 0x3F) + 1;// 6 bits
        uint w = (uint)((quad >> 18) & 0x3F);// 6 bits (width)
        uint h = (uint)((quad >> 24) & 0x3F);// 6 bits (height)
        // uint blockType = (uint)((quad >> 32) & 0x7);

        // GD.Print($"{dir.Name()}: {x},{y},{z} ({w},{h})");
        // if (dir != Direction.PositiveY && dir != Direction.NegativeY) return;
        // Color color = GetBlockColor(blockType);

        var baseIndex = surfaceArrayData.Vertices.Count;
        var corners = GetQuadCorners(dir, x, y, z, w, h);
        surfaceArrayData.Vertices.AddRange(corners);

        var normal = dir.Norm();
        for (int i = 0; i < 4; i++) surfaceArrayData.Normals.Add(normal);

        // 标准UV映射
        if (dir == Direction.PositiveZ ||
            dir == Direction.NegativeZ ||
            dir == Direction.NegativeY)
        {
            surfaceArrayData.UVs.Add(new Vector2(0, h));
            surfaceArrayData.UVs.Add(new Vector2(0, 0));
            surfaceArrayData.UVs.Add(new Vector2(w, 0));
            surfaceArrayData.UVs.Add(new Vector2(w, h));
        }
        else
        {
            surfaceArrayData.UVs.Add(new Vector2(0, w));
            surfaceArrayData.UVs.Add(new Vector2(0, 0));
            surfaceArrayData.UVs.Add(new Vector2(h, 0));
            surfaceArrayData.UVs.Add(new Vector2(h, w));
        }

        // 三角形索引（顺时针顺序）
        surfaceArrayData.Indices.Add(baseIndex + 0);
        surfaceArrayData.Indices.Add(baseIndex + 1);
        surfaceArrayData.Indices.Add(baseIndex + 2);
        surfaceArrayData.Indices.Add(baseIndex + 0);
        surfaceArrayData.Indices.Add(baseIndex + 2);
        surfaceArrayData.Indices.Add(baseIndex + 3);
    }

    private Vector3[] GetQuadCorners(Direction dir, uint x, uint y, uint z, uint w, uint h)
    {

        // 0 PositiveY wDir = 0 hDir = 2
        // 1 NegativeY wDir = 0 hDir = 2
        // 2 PositiveX wDir = 1 hDir = 2
        // 3 NegativeX wDir = 1 hDir = 2
        // 4 PositiveZ wDir = 0 hDir = 1
        // 5 NegativeZ wDir = 0 hDir = 1

        switch (dir)
        {
            case Direction.PositiveY: // Y+
                return new Vector3[]
                {
                new Vector3(x, y, z),
                new Vector3(x + w, y, z),
                new Vector3(x + w, y, z + h),
                new Vector3(x, y, z + h),
                };
            case Direction.NegativeY: // Y-
                return new Vector3[]
                {
                new Vector3(x - w, y, z),
                new Vector3(x - w, y, z + h),
                new Vector3(x, y, z + h),
                new Vector3(x, y, z),
                };
            case Direction.PositiveX: // X+
                return new Vector3[]
                {
                new Vector3(x, y - w, z + h),
                new Vector3(x, y, z + h),
                new Vector3(x, y, z),
                new Vector3(x, y - w, z),
                };
            case Direction.NegativeX: // X-
                return new Vector3[]
                {
                new Vector3(x, y, z),
                new Vector3(x, y + w, z),
                new Vector3(x, y + w, z + h),
                new Vector3(x, y, z + h),
                };
            case Direction.PositiveZ: // Z+
                return new Vector3[]
                {
                new Vector3(x - w, y, z),
                new Vector3(x - w, y + h, z),
                new Vector3(x, y + h, z),
                new Vector3(x, y, z),
                };
            case Direction.NegativeZ: // Z-
                return new Vector3[]
                {
                new Vector3(x + w, y, z),
                new Vector3(x + w, y + h, z),
                new Vector3(x, y + h, z),
                new Vector3(x, y, z),
                };
            default:
                throw new ArgumentOutOfRangeException(nameof(dir), dir, null);
        }
    }

    // 设置碰撞体
    private void SetupCollision()
    {
        if (_arrayMesh == null) return;

        // 创建碰撞形状
        _collisionShape = new CollisionShape3D();
        var shape = _arrayMesh.CreateTrimeshShape();
        _collisionShape.Shape = shape;
        AddChild(_collisionShape);
    }

    // 获取方块颜色（示例）
    private Color GetBlockColor(uint type)
    {
        return type switch
        {
            1 => new Color(0.4f, 0.3f, 0.2f), // 泥土
            2 => new Color(0.2f, 0.2f, 0.2f), // 石头
            3 => new Color(0.1f, 0.5f, 0.1f), // 草
            _ => new Color(0.8f, 0.8f, 0.8f)  // 默认
        };
    }

    // 绘制调试边界框
    private void DrawDebugBounds()
    {
        _debugMesh = new ImmediateMesh();
        _debugMeshInstance = new MeshInstance3D();
        AddChild(_debugMeshInstance);

        _debugMesh.SurfaceBegin(Mesh.PrimitiveType.Lines);

        Vector3 min = Vector3.Zero;
        Vector3 max = new Vector3(World.ChunkSize, World.ChunkSize, World.ChunkSize);

        // 边框线
        DrawDebugLine(min, new Vector3(max.X, min.Y, min.Z));
        DrawDebugLine(min, new Vector3(min.X, max.Y, min.Z));
        DrawDebugLine(min, new Vector3(min.X, min.Y, max.Z));
        DrawDebugLine(max, new Vector3(max.X, min.Y, min.Z));
        DrawDebugLine(max, new Vector3(min.X, max.Y, min.Z));
        DrawDebugLine(max, new Vector3(min.X, min.Y, max.Z));

        _debugMesh.SurfaceEnd();

        _debugMeshInstance.Mesh = _debugMesh;
        _debugMeshInstance.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;

        var material = new StandardMaterial3D()
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = new Color(1, 0, 0, 0.5f)
        };
        _debugMeshInstance.MaterialOverride = material;
    }

    private void DrawDebugLine(Vector3 from, Vector3 to)
    {
        _debugMesh.SurfaceSetColor(new Color(1, 0, 0));
        _debugMesh.SurfaceAddVertex(from);
        _debugMesh.SurfaceAddVertex(to);
    }

    // 获取局部坐标的方块
    public int GetBlock(int x, int y, int z)
    {
        if (x < 0 || x >= World.ChunkSize ||
            y < 0 || y >= World.ChunkSize ||
            z < 0 || z >= World.ChunkSize)
            return 0;

        return VoxelData[ChunkMesher.GetIndex(x, y, z)];
    }

    // 卸载区块
    public void Unload()
    {
        CallDeferred(nameof(DeferredUnload));
    }

    private void DeferredUnload()
    {
        // 释放资源
        if (_meshInstance != null)
        {
            _meshInstance.QueueFree();
            _meshInstance = null;
        }

        if (_collisionShape != null)
        {
            _collisionShape.QueueFree();
            _collisionShape = null;
        }

        if (_debugMeshInstance != null)
        {
            _debugMeshInstance.QueueFree();
            _debugMeshInstance = null;
        }

        _arrayMesh?.Dispose();
        _arrayMesh = null;

        State = ChunkState.Unloaded;
        QueueFree();
    }

    // 更新区块（重新生成网格）
    public void UpdateChunk()
    {
        if (State != ChunkState.Loaded) return;

        Unload();

        // 重新生成网格数据
        var newMeshData = new ChunkMesher.MeshData();
        ChunkMesher.MeshVoxels(VoxelData, newMeshData);
        MeshData = newMeshData;

        Load();
    }
}
