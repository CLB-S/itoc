using Godot;
using System;
using System.Collections.Generic;

public class BasicBlock : Block
{
    public Texture2D Texture;
    private Material _material;

    public BasicBlock(int id, string name)
    {
        BlockID = id;
        BlockName = name;
    }

    public override void LoadResources()
    {
        string path = $"res://assets/blocks/{BlockName}.png";
        _material = BlockHelper.GetMaterial(path);
    }

    public override Material GetMaterial(Direction face = Direction.PositiveX) => _material;
}
