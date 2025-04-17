using System;
using System.Text.RegularExpressions;
using Godot;

public abstract class Block : IEquatable<Block>, IItem
{
    /// <summary>
    /// The namespaced ID of the block (e.g. "stone")
    /// </summary>
    public string BlockId { get; }
    public string BlockName { get; }
    public Color Color { get; set; } = Colors.White;
    public float Hardness { get; set; } = 1.0f;
    public bool IsSolid { get; set; } = true;
    public bool IsOpaque { get; set; } = true;
    public bool IsLightSource { get; set; } = false;
    public float LightStrength { get; set; } = 0;


    public ItemType Type => ItemType.Block;
    public string Id => BlockId;
    public string Name => BlockName;
    public string Description => String.Empty;


    private static readonly Regex _blockIdRegex = new Regex(@"^[a-z0-9_]+$", RegexOptions.Compiled);

    protected Block(string blockId, string blockName)
    {
        if (!_blockIdRegex.IsMatch(blockId))
            throw new ArgumentException($"Invalid block ID format: {blockId}. Must be in format 'block_id' using lowercase letters, numbers and underscores");

        BlockId = blockId;
        BlockName = blockName;
    }

    public static bool IsTransparent(string blockId)
    {
        return String.IsNullOrEmpty(blockId) || !BlockManager.Instance.GetBlock(blockId).IsOpaque;
    }

    public abstract void LoadResources();

    public abstract Material GetMaterial(Direction face = Direction.PositiveY);
    public abstract Texture2D GetTexture(Direction face = Direction.PositiveY);

    public abstract bool Equals(Block other);

    // public virtual Mesh GetMesh(string modelType = "cube") => null;

}
