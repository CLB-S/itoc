using Godot;
using System;
using System.Collections.Generic;

public class BasicBlock : Block
{
    public Texture2D Texture2D;

    public BasicBlock(ushort id, string name)
    {
        BlockID = id;
        BlockName = name;
    }

    public override void LoadResources()
    {
        string path = $"res://assets/blocks/{BlockName}.png";
        Texture2D = ResourceLoader.Load(path) as Texture2D;
    }

    public override Texture2D GetTexture(Direction face = Direction.PositiveX)
    {
        return Texture2D;
    }
}
