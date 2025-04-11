using System;
using System.Collections.Generic;
using Godot;

public partial class Chunk : StaticBody3D
{
    public enum ChunkState
    {
        Unloaded,
        DataReady,
        MeshReady,
        Loaded
    }

    private Mesh _mesh;
    private CollisionShape3D _collisionShape3D;
    private Shape3D _collisionShape;

    // private ImmediateMesh _debugMesh;
    // private MeshInstance3D _debugMeshInstance;

    private MeshInstance3D _meshInstance;

    public ChunkData ChunkData { get; private set; }
    public Vector3I ChunkPosition { get; private set; }

    public ChunkState State { get; private set; } = ChunkState.Unloaded;

    public Chunk(ChunkGenerator.ChunkGenerationResult result)
    {
        _mesh = result.Mesh;
        _collisionShape = result.CollisionShape;
        ChunkData = result.ChunkData;
        ChunkPosition = ChunkData.GetPosition();
        Position = ChunkPosition * World.ChunkSize;
        Name = $"Chunk_{ChunkPosition.X}_{ChunkPosition.Y}_{ChunkPosition.Z}";

        // TODO: State
    }

    public void Load()
    {
        if (State == ChunkState.Loaded) return;

        CallDeferred(nameof(DeferredLoad));
    }

    private void DeferredLoad()
    {
        if (State == ChunkState.Loaded) return;

        _meshInstance = new MeshInstance3D();
        _meshInstance.Mesh = _mesh;
        if (World.Instance.UseDebugMaterial) _meshInstance.MaterialOverride = World.Instance.DebugMaterial;
        AddChild(_meshInstance);

        _collisionShape3D = new CollisionShape3D();
        if (_collisionShape != null)
            _collisionShape3D.Shape = _collisionShape;
        AddChild(_collisionShape3D);


        // if (World.Instance.DebugDrawChunkBounds) DrawDebugBounds();

        State = ChunkState.Loaded;
    }


    /*
    private void DrawDebugBounds()
    {
        _debugMesh = new ImmediateMesh();
        _debugMeshInstance = new MeshInstance3D();
        AddChild(_debugMeshInstance);

        _debugMesh.SurfaceBegin(Mesh.PrimitiveType.Lines);

        var min = Vector3.Zero;
        var max = new Vector3(World.ChunkSize, World.ChunkSize, World.ChunkSize);

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

    */

    public string GetBlock(int x, int y, int z)
    {
        // if (x < 0 || x >= World.ChunkSize ||
        //     y < 0 || y >= World.ChunkSize ||
        //     z < 0 || z >= World.ChunkSize)
        //     return 0;

        return ChunkData.GetBlock(x + 1, y + 1, z + 1);
    }

    public void SetBlock(int x, int y, int z, string block)
    {
        // if (x < 0 || x >= World.ChunkSize ||
        //     y < 0 || y >= World.ChunkSize ||
        //     z < 0 || z >= World.ChunkSize)
        //     return;

        // GD.Print($"Setting block {block} at {x}, {y}, {z}");

        ChunkData.SetBlock(x + 1, y + 1, z + 1, block);
        Update();

        var neighbourChunkPos = ChunkPosition;
        if (x == 0)
        {
            neighbourChunkPos.X -= 1;
            var neighbourChunk = World.Instance.GetChunk(neighbourChunkPos);
            neighbourChunk.ChunkData.SetBlock(ChunkMesher.CS_P - 1, y + 1, z + 1, block);
            neighbourChunk.Update();
        }

        if (x == ChunkMesher.CS - 1)
        {
            neighbourChunkPos.X += 1;
            var neighbourChunk = World.Instance.GetChunk(neighbourChunkPos);
            neighbourChunk.ChunkData.SetBlock(0, y + 1, z + 1, block);
            neighbourChunk.Update();
        }

        if (y == 0)
        {
            neighbourChunkPos.Y -= 1;
            var neighbourChunk = World.Instance.GetChunk(neighbourChunkPos);
            neighbourChunk.ChunkData.SetBlock(x + 1, ChunkMesher.CS_P - 1, z + 1, block);
            neighbourChunk.Update();
        }

        if (y == ChunkMesher.CS - 1)
        {
            neighbourChunkPos.Y += 1;
            var neighbourChunk = World.Instance.GetChunk(neighbourChunkPos);
            neighbourChunk.ChunkData.SetBlock(x + 1, 0, z + 1, block);
            neighbourChunk.Update();
        }

        if (z == 0)
        {
            neighbourChunkPos.Z -= 1;
            var neighbourChunk = World.Instance.GetChunk(neighbourChunkPos);
            neighbourChunk.ChunkData.SetBlock(x + 1, y + 1, ChunkMesher.CS_P - 1, block);
            neighbourChunk.Update();
        }

        if (z == ChunkMesher.CS - 1)
        {
            neighbourChunkPos.Z += 1;
            var neighbourChunk = World.Instance.GetChunk(neighbourChunkPos);
            neighbourChunk.ChunkData.SetBlock(x + 1, y + 1, 0, block);
            neighbourChunk.Update();
        }

    }


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

        if (_collisionShape3D != null)
        {
            _collisionShape3D.QueueFree();
            _collisionShape3D = null;
        }

        // if (_debugMeshInstance != null)
        // {
        //     _debugMeshInstance.QueueFree();
        //     _debugMeshInstance = null;
        // }

        // _arrayMesh?.Dispose();
        // _arrayMesh = null;
    }

    private void DeferredUnload()
    {
        FreeUpResources();

        State = ChunkState.Unloaded;
        QueueFree();
    }

    public void Update()
    {
        if (State != ChunkState.Loaded) return;

        using var meshData = new ChunkMesher.MeshData(ChunkData.OpaqueMask);
        ChunkMesher.MeshChunk(ChunkData, meshData);
        var mesh = ChunkMesher.GenerateMesh(meshData);
        _meshInstance.Mesh = mesh;
        _collisionShape3D.Shape = mesh?.CreateTrimeshShape();

        // GD.Print($"Updated chunk at {ChunkPosition}");
    }
}