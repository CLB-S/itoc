using Godot;

public partial class Core : Node
{
    public static Core Instance { get; private set; }

    public override void _Ready()
    {
        Instance = this;
        GD.Seed(1212);
        LoadBlocks();
    }

    private void LoadBlocks()
    {
        GD.Print($"Loaded {BlockManager.Instance.GetBlockCount()} blocks");
    }
}