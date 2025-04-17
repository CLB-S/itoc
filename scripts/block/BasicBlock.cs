using System;
using Godot;

public class BasicBlock : Block
{
    private Material _material;
    private string _materialPath;
    private Texture2D _texture;

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
        _texture = ResourceLoader.Load(path) as Texture2D;
        _material = BlockHelper.GetMaterialByTexture(_texture);
    }

    public override Material GetMaterial(Direction face = Direction.PositiveX)
    {
        return _material;
    }

    public override Texture2D GetTexture(Direction face = Direction.PositiveY)
    {
        return _texture;
    }

    public override bool Equals(Block other)
    {
        return other.BlockId == BlockId;
    }
}
