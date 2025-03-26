using Godot;
using System;
using System.Collections.Generic;

// WARNING: By deepseek. Not revised.

// public class DirectionalBlock : Block
// {
//     private Dictionary<Direction, Material> _materials;

//     public DirectionalBlock(ushort id, string name)
//     {
//         BlockID = id;
//         BlockName = name;
//     }

//     public override void LoadResources()
//     {
//         _materials = new Dictionary<Direction, Material>();

//         foreach (Direction face in Enum.GetValues(typeof(Direction)))
//         {
//             string path = $"res://Assets/Blocks/{BlockID}/textures/{face.ToString().ToLower()}.tres";
//             if (ResourceLoader.Exists(path))
//             {
//                 _materials[face] = ResourceLoader.Load<Material>(path);
//             }
//         }
//     }

//     public override Material GetMaterial(Direction face)
//     {
//         return _materials.TryGetValue(face, out var mat) ? mat : null;
//     }
// }
