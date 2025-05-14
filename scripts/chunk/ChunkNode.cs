// using System;
// using System.Collections.Generic;
// using Godot;

// public partial class ChunkNode : StaticBody3D
// {
//     public enum ChunkState
//     {
//         Unloaded,
//         DataReady,
//         Render,
//         Physics
//     }

//     private Mesh _mesh;
//     private CollisionShape3D _collisionShape3D;

//     // private Shape3D _collisionShape;

//     // private ImmediateMesh _debugMesh;
//     private MeshInstance3D _collisionDebugMeshInstance;
//     private MeshInstance3D _meshInstance;

//     public Chunk ChunkData { get; }
//     public ChunkColumn ChunkColumn { get; private set; }
//     public Vector3I ChunkPosition { get; private set; }

//     public ChunkState State { get; private set; } = ChunkState.Unloaded;

//     public ChunkNode(Chunk result)
//     {
//         ChunkData = result;
//         ChunkColumn = result.GetChunkColumn();
//         ChunkPosition = ChunkData.Position;
//         Position = ChunkPosition * ChunkMesher.CS;
//         Name = $"Chunk_{ChunkPosition.X}_{ChunkPosition.Y}_{ChunkPosition.Z}";

//         State = ChunkState.DataReady;
//     }

//     public override void _PhysicsProcess(double delta)
//     {
//         var distance = ChunkPosition.DistanceTo(Core.Instance.CurrentWorld.PlayerChunk);
//         if (distance > Core.Instance.Settings.PhysicsDistance + 1)
//             UnloadPhysics();
//         else if (distance <= Core.Instance.Settings.PhysicsDistance) LoadPhysics();
//     }

//     public void LoadDeferred()
//     {
//         CallDeferred(nameof(Load));
//     }

//     private void Load()
//     {
//         _meshInstance = new MeshInstance3D();
//         if (Core.Instance.CurrentWorld.UseDebugMaterial) _meshInstance.MaterialOverride = Core.Instance.CurrentWorld.DebugMaterial;
//         AddChild(_meshInstance);

//         _collisionShape3D = new CollisionShape3D();
//         _collisionDebugMeshInstance = new MeshInstance3D();
//         _collisionDebugMeshInstance.MaterialOverride =
//             ResourceLoader.Load<ShaderMaterial>("res://assets/graphics/chunk_debug_shader_material.tres");
//         _collisionDebugMeshInstance.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
//         AddChild(_collisionShape3D);
//         AddChild(_collisionDebugMeshInstance);

//         UpdateMesh();

//         State = ChunkState.Render;

//         // if (_collisionShape != null)
//         // {
//         //     _collisionShape3D.Shape = _collisionShape;
//         //     State = ChunkState.Physics;

//         //     if (Core.Instance.Settings.DrawDebugChunkCollisionShape)
//         //         _collisionDebugMeshInstance.Mesh = _collisionShape3D.Shape.GetDebugMesh();
//         // }

//         // if (World.Instance.DebugDrawChunkBounds) DrawDebugBounds();
//     }

//     private void UnloadPhysics()
//     {
//         if (State != ChunkState.Physics) return;

//         _collisionShape3D.Shape = null;

//         if (Core.Instance.Settings.DrawDebugChunkCollisionShape)
//             _collisionDebugMeshInstance.Mesh = null;

//         State = ChunkState.Render;
//     }

//     private void LoadPhysics()
//     {
//         if (State != ChunkState.Render) return;
//         _collisionShape3D.Shape = _meshInstance.Mesh?.CreateTrimeshShape();
//         if (Core.Instance.Settings.DrawDebugChunkCollisionShape)
//             _collisionDebugMeshInstance.Mesh = _collisionShape3D.Shape?.GetDebugMesh();

//         State = ChunkState.Physics;
//     }

//     /*
//     private void DrawDebugBounds()
//     {
//         _debugMesh = new ImmediateMesh();
//         _debugMeshInstance = new MeshInstance3D();
//         AddChild(_debugMeshInstance);

//         _debugMesh.SurfaceBegin(Mesh.PrimitiveType.Lines);

//         var min = Vector3.Zero;
//         var max = new Vector3(World.ChunkSize, World.ChunkSize, World.ChunkSize);

//         DrawDebugLine(min, new Vector3(max.X, min.Y, min.Z));
//         DrawDebugLine(min, new Vector3(min.X, max.Y, min.Z));
//         DrawDebugLine(min, new Vector3(min.X, min.Y, max.Z));
//         DrawDebugLine(max, new Vector3(max.X, min.Y, min.Z));
//         DrawDebugLine(max, new Vector3(min.X, max.Y, min.Z));
//         DrawDebugLine(max, new Vector3(min.X, min.Y, max.Z));

//         _debugMesh.SurfaceEnd();

//         _debugMeshInstance.Mesh = _debugMesh;
//         _debugMeshInstance.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;

//         var material = new StandardMaterial3D
//         {
//             ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
//             AlbedoColor = new Color(1, 0, 0, 0.5f)
//         };
//         _debugMeshInstance.MaterialOverride = material;
//     }

//     private void DrawDebugLine(Vector3 from, Vector3 to)
//     {
//         _debugMesh.SurfaceSetColor(new Color(1, 0, 0));
//         _debugMesh.SurfaceAddVertex(from);
//         _debugMesh.SurfaceAddVertex(to);
//     }

//     */

//     public Block GetBlock(int x, int y, int z)
//     {
//         return ChunkData.GetBlock(x, y, z);
//     }

//     public void SetBlock(int x, int y, int z, Block block)
//     {
//         ChunkData.SetBlock(x, y, z, block);
//         UpdateMeshIfNeededDeferred();

//         if (x == 0)
//         {
//             var neighbourChunkPos = ChunkPosition;
//             neighbourChunkPos.X -= 1;
//             var neighbourChunk = Core.Instance.CurrentWorld.GetChunk(neighbourChunkPos);
//             neighbourChunk.SetMesherMask(ChunkMesher.CS_P - 1, y + 1, z + 1, block);
//             neighbourChunk.UpdateMeshIfNeededDeferred();
//         }

//         if (x == ChunkMesher.CS - 1)
//         {
//             var neighbourChunkPos = ChunkPosition;
//             neighbourChunkPos.X += 1;
//             var neighbourChunk = Core.Instance.CurrentWorld.GetChunk(neighbourChunkPos);
//             neighbourChunk.ChunkData.SetMesherMask(0, y + 1, z + 1, block);
//             neighbourChunk.UpdateMeshIfNeededDeferred();
//         }

//         if (y == 0)
//         {
//             var neighbourChunkPos = ChunkPosition;
//             neighbourChunkPos.Y -= 1;
//             var neighbourChunk = Core.Instance.CurrentWorld.GetChunk(neighbourChunkPos);
//             neighbourChunk.ChunkData.SetMesherMask(x + 1, ChunkMesher.CS_P - 1, z + 1, block);
//             neighbourChunk.UpdateMeshIfNeededDeferred();
//         }

//         if (y == ChunkMesher.CS - 1)
//         {
//             var neighbourChunkPos = ChunkPosition;
//             neighbourChunkPos.Y += 1;
//             var neighbourChunk = Core.Instance.CurrentWorld.GetChunk(neighbourChunkPos);
//             neighbourChunk.ChunkData.SetMesherMask(x + 1, 0, z + 1, block);
//             neighbourChunk.UpdateMeshIfNeededDeferred();
//         }

//         if (z == 0)
//         {
//             var neighbourChunkPos = ChunkPosition;
//             neighbourChunkPos.Z -= 1;
//             var neighbourChunk = Core.Instance.CurrentWorld.GetChunk(neighbourChunkPos);
//             neighbourChunk.ChunkData.SetMesherMask(x + 1, y + 1, ChunkMesher.CS_P - 1, block);
//             neighbourChunk.UpdateMeshIfNeededDeferred();
//         }

//         if (z == ChunkMesher.CS - 1)
//         {
//             var neighbourChunkPos = ChunkPosition;
//             neighbourChunkPos.Z += 1;
//             var neighbourChunk = Core.Instance.CurrentWorld.GetChunk(neighbourChunkPos);
//             neighbourChunk.ChunkData.SetMesherMask(x + 1, y + 1, 0, block);
//             neighbourChunk.UpdateMeshIfNeededDeferred();
//         }
//     }

//     private void FreeUpResources()
//     {
//         if (_meshInstance != null)
//         {
//             _meshInstance.QueueFree();
//             _meshInstance = null;
//         }

//         if (_collisionShape3D != null)
//         {
//             _collisionShape3D.QueueFree();
//             _collisionShape3D = null;
//         }

//         if (_collisionDebugMeshInstance != null)
//         {
//             _collisionDebugMeshInstance.QueueFree();
//             _collisionDebugMeshInstance = null;
//         }

//         // if (_debugMeshInstance != null)
//         // {
//         //     _debugMeshInstance.QueueFree();
//         //     _debugMeshInstance = null;
//         // }

//         // _arrayMesh?.Dispose();
//         // _arrayMesh = null;
//     }

//     public void UnloadDeferred()
//     {
//         CallDeferred(nameof(Unload));
//     }

//     private void Unload()
//     {
//         FreeUpResources();

//         State = ChunkState.Unloaded;
//         ChunkColumn.Chunks.Remove(ChunkPosition, out _);
//         if (ChunkColumn.Chunks.Count == 0)
//         {
//             Core.Instance.CurrentWorld.ChunkColumns.Remove(ChunkColumn.Position, out _);
//             ChunkColumn = null;
//         }

//         QueueFree();
//     }

//     public void UpdateMeshIfNeededDeferred()
//     {
//         CallDeferred(nameof(UpdateMeshIfNeeded));
//     }

//     public void UpdateMeshIfNeeded()
//     {
//         if (State == ChunkState.Render || State == ChunkState.Physics)
//             UpdateMesh();
//     }

//     public void UpdateMesh()
//     {
//         if (State == ChunkState.Unloaded)
//             throw new InvalidOperationException("Chunk is not loaded.");

//         var meshData = new ChunkMesher.MeshData(ChunkData.OpaqueMask, ChunkData.TransparentMasks);
//         ChunkMesher.MeshChunk(ChunkData, meshData);
//         var mesh = ChunkMesher.GenerateMesh(meshData);
//         _meshInstance.Mesh = mesh;

//         if (State == ChunkState.Physics)
//         {
//             _collisionShape3D.Shape = _meshInstance.Mesh?.CreateTrimeshShape();

//             if (Core.Instance.Settings.DrawDebugChunkCollisionShape)
//                 _collisionDebugMeshInstance.Mesh = _collisionShape3D.Shape?.GetDebugMesh();
//         }

//         // GD.Print($"Updated chunk at {ChunkPosition}");
//     }
// }