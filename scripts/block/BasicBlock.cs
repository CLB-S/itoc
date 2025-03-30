using Godot;

public class BasicBlock : Block
{
    private Material _material;
    public Texture2D Texture;

    public BasicBlock(uint id, string name)
    {
        BlockID = id;
        BlockName = name;
    }

    public override void LoadResources()
    {
        var path = $"res://assets/blocks/{BlockName}.png";
        _material = BlockHelper.GetMaterial(path);
    }

    public override Material GetMaterial(Direction face = Direction.PositiveX)
    {
        return _material;
    }
}