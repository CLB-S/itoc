using Godot;
using System;
using System.Collections.Generic;

public class BasicBlock : Block
{
    public Texture2D Texture;

    public BasicBlock(int id, string name)
    {
        BlockID = id;
        BlockName = name;
    }

    public override void LoadResources()
    {
        string path = $"res://assets/blocks/{BlockName}.png";
        Texture = ResourceLoader.Load(path) as Texture2D;
    }

    public override Texture2D GetTexture(Direction face = Direction.PositiveX)
    {
        return Texture;
    }
}
