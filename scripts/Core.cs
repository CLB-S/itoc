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
        BlockManager.Instance.RegisterBlock(new BasicBlock(1, "stone"));
        BlockManager.Instance.RegisterBlock(new BasicBlock(2, "debug"));
        BlockManager.Instance.RegisterBlock(new BasicBlock(3, "dirt"));

        GD.Print($"Loaded {BlockManager.Instance.GetBlockCount()} blocks");
    }

}