using System;
using System.Collections.Generic;
using Godot;
using Array = Godot.Collections.Array;

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

    private ArrayMesh _arrayMesh;
    private CollisionShape3D _collisionShape;

    // 调试显示
    private ImmediateMesh _debugMesh;
    private MeshInstance3D _debugMeshInstance;

    // 节点引用
    private MeshInstance3D _meshInstance;

    public Chunk(ChunkGenerationResult result)
    {
        Initialize(result.ChunkPosition, result.VoxelData, result.MeshData);
    }

    // 公开属性
    public Vector3 ChunkPosition { get; private set; }
    public ChunkState State { get; private set; } = ChunkState.Unloaded;
    public uint[] VoxelData { get; private set; }
    public ChunkMesher.MeshData MeshData { get; private set; }

    // 初始化区块
    public void Initialize(Vector3 chunkPosition, uint[] voxelData, ChunkMesher.MeshData meshData)
    {
        ChunkPosition = chunkPosition;
        VoxelData = voxelData;
        MeshData = meshData;
        State = ChunkState.DataReady;

        Position = chunkPosition * World.ChunkSize;
        Name = $"Chunk_{chunkPosition.X}_{chunkPosition.Y}_{chunkPosition.Z}";
    }

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
        if (World.Instance.UseDebugMaterial) _meshInstance.MaterialOverride = World.Instance.DebugMaterial;
        AddChild(_meshInstance);

        _collisionShape = new CollisionShape3D();
        AddChild(_collisionShape);

        UpdateMesh();
        UpdateCollision();

        // 调试边框
        if (World.Instance.DebugDrawChunkBounds) DrawDebugBounds();

        State = ChunkState.Loaded;
    }

    // 生成Godot可用的ArrayMesh
    private void UpdateMesh()
    {
        if (MeshData.Quads.Count == 0) return;

        var surfaceArrayDict = new Dictionary<uint, SurfaceArrayData>();

        for (var face = 0; face < 6; face++)
            for (var i = MeshData.FaceVertexBegin[face];
                 i < MeshData.FaceVertexBegin[face] + MeshData.FaceVertexLength[face];
                 i++)
                ParseQuad((Direction)face, MeshData.QuadBlockIDs[i], MeshData.Quads[i], surfaceArrayDict);

        _arrayMesh = new ArrayMesh();
        foreach (var (blockInfo, surfaceArrayData) in surfaceArrayDict)
        {
            var (blockID, dir) = ParseBlockInfo(blockInfo);
            _arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArrayData.GetSurfaceArray());
            _arrayMesh.SurfaceSetMaterial(_arrayMesh.GetSurfaceCount() - 1,
                BlockManager.Instance.GetBlock(blockID).GetMaterial(dir));
        }

        _meshInstance.Mesh = _arrayMesh;
    }

    private (uint, Direction) ParseBlockInfo(uint blockInfo)
    {
        return ((blockInfo << 3) >> 3, (Direction)(blockInfo >> 29));
    }

    private uint GetBlockInfo(uint blockID, Direction dir)
    {
        if (BlockManager.Instance.GetBlock(blockID) is DirectionalBlock)
            return ((uint)dir << 29) | blockID;
        return blockID;
    }

    private void ParseQuad(Direction dir, uint blockID, ulong quad,
        Dictionary<uint, SurfaceArrayData> surfaceArrayDict)
    {
        var blockInfo = GetBlockInfo(blockID, dir);
        if (!surfaceArrayDict.ContainsKey(blockInfo))
            surfaceArrayDict.Add(blockInfo, new SurfaceArrayData());

        var surfaceArrayData = surfaceArrayDict[blockInfo];

        // 解析数据（与C++结构完全一致）
        var x = (uint)(quad & 0x3F); // 6 bits
        var y = (uint)((quad >> 6) & 0x3F); // 6 bits
        var z = (uint)((quad >> 12) & 0x3F); // 6 bits
        var w = (uint)((quad >> 18) & 0x3F); // 6 bits (width)
        var h = (uint)((quad >> 24) & 0x3F); // 6 bits (height)
        // uint blockType = (uint)((quad >> 32) & 0x7);

        // GD.Print($"{dir.Name()}: {x},{y},{z} ({w},{h})");
        // if (dir != Direction.PositiveY && dir != Direction.NegativeY) return;
        // Color color = GetBlockColor(blockType);

        var baseIndex = surfaceArrayData.Vertices.Count;
        var corners = GetQuadCorners(dir, x, y, z, w, h);
        surfaceArrayData.Vertices.AddRange(corners);

        var normal = dir.Norm();
        for (var i = 0; i < 4; i++) surfaceArrayData.Normals.Add(normal);

        var offset = 0.0014f;

        // 标准UV映射
        if (dir == Direction.PositiveZ ||
            dir == Direction.NegativeZ ||
            dir == Direction.NegativeY)
        {
            surfaceArrayData.UVs.Add(new Vector2(offset, h - offset));
            surfaceArrayData.UVs.Add(new Vector2(offset, offset));
            surfaceArrayData.UVs.Add(new Vector2(w - offset, offset));
            surfaceArrayData.UVs.Add(new Vector2(w - offset, h - offset));
        }
        else
        {
            surfaceArrayData.UVs.Add(new Vector2(offset, w - offset));
            surfaceArrayData.UVs.Add(new Vector2(offset, offset));
            surfaceArrayData.UVs.Add(new Vector2(h - offset, offset));
            surfaceArrayData.UVs.Add(new Vector2(h - offset, w - offset));
        }

        // 三角形索引（顺时针顺序）
        surfaceArrayData.Indices.Add(baseIndex + 0);
        surfaceArrayData.Indices.Add(baseIndex + 1);
        surfaceArrayData.Indices.Add(baseIndex + 2);
        surfaceArrayData.Indices.Add(baseIndex + 0);
        surfaceArrayData.Indices.Add(baseIndex + 2);
        surfaceArrayData.Indices.Add(baseIndex + 3);
    }

    private Vector3[] GetQuadCorners(Direction dir, float x, float y, float z, float w, float h)
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
                    new(x, y, z),
                    new(x + w, y, z),
                    new(x + w, y, z + h),
                    new(x, y, z + h)
                };
            case Direction.NegativeY: // Y-
                return new Vector3[]
                {
                    new(x - w, y, z),
                    new(x - w, y, z + h),
                    new(x, y, z + h),
                    new(x, y, z)
                };
            case Direction.PositiveX: // X+
                return new Vector3[]
                {
                    new(x, y - w, z + h),
                    new(x, y, z + h),
                    new(x, y, z),
                    new(x, y - w, z)
                };
            case Direction.NegativeX: // X-
                return new Vector3[]
                {
                    new(x, y, z),
                    new(x, y + w, z),
                    new(x, y + w, z + h),
                    new(x, y, z + h)
                };
            case Direction.PositiveZ: // Z+
                return new Vector3[]
                {
                    new(x - w, y, z),
                    new(x - w, y + h, z),
                    new(x, y + h, z),
                    new(x, y, z)
                };
            case Direction.NegativeZ: // Z-
                return new Vector3[]
                {
                    new(x + w, y, z),
                    new(x + w, y + h, z),
                    new(x, y + h, z),
                    new(x, y, z)
                };
            default:
                throw new ArgumentOutOfRangeException(nameof(dir), dir, null);
        }
    }

    // 设置碰撞体
    private void UpdateCollision()
    {
        if (_arrayMesh == null) return;

        // 创建碰撞形状
        var shape = _arrayMesh.CreateTrimeshShape();
        _collisionShape.Shape = shape;
    }

    // 绘制调试边界框
    private void DrawDebugBounds()
    {
        _debugMesh = new ImmediateMesh();
        _debugMeshInstance = new MeshInstance3D();
        AddChild(_debugMeshInstance);

        _debugMesh.SurfaceBegin(Mesh.PrimitiveType.Lines);

        var min = Vector3.Zero;
        var max = new Vector3(World.ChunkSize, World.ChunkSize, World.ChunkSize);

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

        var material = new StandardMaterial3D
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

    public uint GetBlock(int x, int y, int z)
    {
        // if (x < 0 || x >= World.ChunkSize ||
        //     y < 0 || y >= World.ChunkSize ||
        //     z < 0 || z >= World.ChunkSize)
        //     return 0;

        return VoxelData[ChunkMesher.GetIndex(x + 1, y + 1, z + 1)];
    }

    public void SetBlock(int x, int y, int z, uint block)
    {
        // if (x < 0 || x >= World.ChunkSize ||
        //     y < 0 || y >= World.ChunkSize ||
        //     z < 0 || z >= World.ChunkSize)
        //     return;

        VoxelData[ChunkMesher.GetIndex(x + 1, y + 1, z + 1)] = block;

        bool isOpaque = block == 0 || !BlockManager.Instance.GetBlock(block).IsOpaque;

        if (isOpaque)
            ChunkMesher.AddNonOpaqueVoxel(ref MeshData.OpaqueMask, x + 1, y + 1, z + 1);
        else
            ChunkMesher.AddOpaqueVoxel(ref MeshData.OpaqueMask, x + 1, y + 1, z + 1);
        Update();

        if (x == 0)
        {
            var neighbourChunkPos = (Vector3I)ChunkPosition;
            neighbourChunkPos.X -= 1;
            var neighbourChunk = World.Instance.GetChunk(neighbourChunkPos);
            if (isOpaque)
                ChunkMesher.AddNonOpaqueVoxel(ref neighbourChunk.MeshData.OpaqueMask, ChunkMesher.CS_P - 1, y + 1, z + 1);
            else
                ChunkMesher.AddOpaqueVoxel(ref neighbourChunk.MeshData.OpaqueMask, ChunkMesher.CS_P - 1, y + 1, z + 1);

            neighbourChunk.Update();
        }

        if (x == ChunkMesher.CS - 1)
        {
            var neighbourChunkPos = (Vector3I)ChunkPosition;
            neighbourChunkPos.X += 1;
            var neighbourChunk = World.Instance.GetChunk(neighbourChunkPos);
            if (isOpaque)
                ChunkMesher.AddNonOpaqueVoxel(ref neighbourChunk.MeshData.OpaqueMask, 0, y + 1, z + 1);
            else
                ChunkMesher.AddOpaqueVoxel(ref neighbourChunk.MeshData.OpaqueMask, 0, y + 1, z + 1);

            neighbourChunk.Update();
        }

        if (y == 0)
        {
            var neighbourChunkPos = (Vector3I)ChunkPosition;
            neighbourChunkPos.Y -= 1;
            var neighbourChunk = World.Instance.GetChunk(neighbourChunkPos);
            if (isOpaque)
                ChunkMesher.AddNonOpaqueVoxel(ref neighbourChunk.MeshData.OpaqueMask, x + 1, ChunkMesher.CS_P - 1, z + 1);
            else
                ChunkMesher.AddOpaqueVoxel(ref neighbourChunk.MeshData.OpaqueMask, x + 1, ChunkMesher.CS_P - 1, z + 1);

            neighbourChunk.Update();
        }

        if (y == ChunkMesher.CS - 1)
        {
            var neighbourChunkPos = (Vector3I)ChunkPosition;
            neighbourChunkPos.Y += 1;
            var neighbourChunk = World.Instance.GetChunk(neighbourChunkPos);
            if (isOpaque)
                ChunkMesher.AddNonOpaqueVoxel(ref neighbourChunk.MeshData.OpaqueMask, x + 1, 0, z + 1);
            else
                ChunkMesher.AddOpaqueVoxel(ref neighbourChunk.MeshData.OpaqueMask, x + 1, 0, z + 1);

            neighbourChunk.Update();
        }

        if (z == 0)
        {
            var neighbourChunkPos = (Vector3I)ChunkPosition;
            neighbourChunkPos.Z -= 1;
            var neighbourChunk = World.Instance.GetChunk(neighbourChunkPos);
            if (isOpaque)
                ChunkMesher.AddNonOpaqueVoxel(ref neighbourChunk.MeshData.OpaqueMask, x + 1, y + 1, ChunkMesher.CS_P - 1);
            else
                ChunkMesher.AddOpaqueVoxel(ref neighbourChunk.MeshData.OpaqueMask, x + 1, y + 1, ChunkMesher.CS_P - 1);

            neighbourChunk.Update();
        }

        if (z == ChunkMesher.CS - 1)
        {
            var neighbourChunkPos = (Vector3I)ChunkPosition;
            neighbourChunkPos.Z += 1;
            var neighbourChunk = World.Instance.GetChunk(neighbourChunkPos);
            if (isOpaque)
                ChunkMesher.AddNonOpaqueVoxel(ref neighbourChunk.MeshData.OpaqueMask, x + 1, y + 1, 0);
            else
                ChunkMesher.AddOpaqueVoxel(ref neighbourChunk.MeshData.OpaqueMask, x + 1, y + 1, 0);

            neighbourChunk.Update();
        }

    }


    // 卸载区块
    public void Unload()
    {
        CallDeferred(nameof(DeferredUnload));
    }

    private void FreeUpResources()
    {
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
    }

    private void DeferredUnload()
    {
        FreeUpResources();

        State = ChunkState.Unloaded;
        QueueFree();
    }

    // 更新区块（重新生成网格）
    public void Update()
    {
        if (State != ChunkState.Loaded) return;

        ChunkMesher.MeshVoxels(VoxelData, MeshData);

        UpdateMesh();
        UpdateCollision();
    }

    private class SurfaceArrayData
    {
        public readonly List<int> Indices = new();
        public readonly List<Vector3> Normals = new();
        public readonly List<Vector2> UVs = new();
        public readonly List<Vector3> Vertices = new();

        public Array GetSurfaceArray()
        {
            var surfaceArray = new Array();
            surfaceArray.Resize((int)Mesh.ArrayType.Max);

            surfaceArray[(int)Mesh.ArrayType.Vertex] = Vertices.ToArray();
            surfaceArray[(int)Mesh.ArrayType.TexUV] = UVs.ToArray();
            surfaceArray[(int)Mesh.ArrayType.Normal] = Normals.ToArray();
            surfaceArray[(int)Mesh.ArrayType.Index] = Indices.ToArray();

            return surfaceArray;
        }
    }
}