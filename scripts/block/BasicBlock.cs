using Godot;

public class BasicBlock : Block
{
    private Material _material;
    public Texture2D Texture;

    public BasicBlock(string blockId, string blockName) : base(blockId, blockName)
    {
    }

    public override void LoadResources()
    {
        var path = $"res://assets/blocks/{BlockId}.png";
        _material = BlockHelper.GetMaterial(path);
    }

    public override Material GetMaterial(Direction face = Direction.PositiveX)
    {
        return _material;
    }

    public override bool Equals(Block other)
    {
        return other.BlockId == BlockId;
    }

}
