using Godot;
using System;

public partial class Core : Node
{
    public static Core Instance { get; private set; }

    public override void _Ready()
    {
        Instance = this;

        LoadBlocks();
    }

    private void LoadBlocks()
    {
        BlockManager.Instance.RegisterBlock(new BasicBlock(1, "debug"));
        BlockManager.Instance.RegisterBlock(new BasicBlock(2, "stone"));
        BlockManager.Instance.RegisterBlock(new BasicBlock(3, "dirt"));
        BlockManager.Instance.RegisterBlock(new DirectionalBlock(4, "grass_block", Direction.PositiveY, textureBottomPath: "res://assets/blocks/dirt.png"));

        GD.Print($"Loaded {BlockManager.Instance.GetBlockCount()} blocks");
    }

}