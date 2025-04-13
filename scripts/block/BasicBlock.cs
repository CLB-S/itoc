using System;
using Godot;

public class BasicBlock : Block
{
    private Material _material;
    private string _materialPath;
    public Texture2D Texture;

    public BasicBlock(string blockId, string blockName) : base(blockId, blockName)
    {
    }

    public BasicBlock(string blockId, string blockName, string materialPath) : base(blockId, blockName)
    {
        _materialPath = materialPath;
    }

    public override void LoadResources()
    {
        if (!String.IsNullOrEmpty(_materialPath))
        {
            var material = ResourceLoader.Load(_materialPath) as Material;
            if (material != null)
            {
                _material = material;
                return;
            }
        }

        var path = $"res://assets/blocks/{BlockId}.png";
        _material = BlockHelper.GetMaterialByTexture(path);
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
