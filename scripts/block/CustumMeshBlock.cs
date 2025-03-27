using Godot;
using System;
using System.Collections.Generic;

// WARNING: By deepseek. Not revised.

// public class CustomMeshBlock : Block
// {
//     private Mesh _customMesh;
//     private Material _material;

//     public override string[] ModelTypes => new[] { "custom" };

//     public override void LoadResources()
//     {
//         string meshPath = $"res://Assets/Blocks/{BlockID}/model.glb";
//         if (ResourceLoader.Exists(meshPath))
//         {
//             _customMesh = ResourceLoader.Load<Mesh>(meshPath);
//         }

//         string matPath = $"res://Assets/Blocks/{BlockID}/material.tres";
//         if (ResourceLoader.Exists(matPath))
//         {
//             _material = ResourceLoader.Load<Material>(matPath);
//         }
//     }

//     public override Mesh GetMesh(string modelType) => _customMesh;
//     public override Material GetMaterial(Direction face) => _material;
// }