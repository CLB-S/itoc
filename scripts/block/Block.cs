using System;
using System.Text.RegularExpressions;
using Godot;

public abstract class Block
{
    /// <summary>
    /// The namespaced ID of the block (e.g. "itoc:stone")
    /// </summary>
    public string BlockId { get; }
    public string BlockName { get; }
    public string Namespace => BlockId.Split(':')[0];
    public string NakeId => BlockId.Split(':')[1];

    private const string DEFAULT_NAMESPACE = "itoc";
    private static readonly Regex BlockIdRegex = new Regex(@"^[a-z0-9_]+:[a-z0-9_]+$", RegexOptions.Compiled);

    protected Block(string blockId, string blockName)
    {
        BlockId = NormalizeBlockId(blockId);
        BlockName = blockName;
    }

    public static string NormalizeBlockId(string blockId)
    {
        if (string.IsNullOrWhiteSpace(blockId))
            throw new ArgumentException("Block ID cannot be null or whitespace");

        // If no namespace specified, add default namespace
        if (!blockId.Contains(':'))
            return $"{DEFAULT_NAMESPACE}:{blockId}";

        if (!BlockIdRegex.IsMatch(blockId))
            throw new ArgumentException($"Invalid block ID format: {blockId}. Must be in format 'namespace:block_id' using lowercase letters, numbers and underscores");

        return blockId;
    }

    public static (string blockNamespace, string nakeId) SplitBlockId(string blockId)
    {
        var parts = blockId.Split(':');
        return (parts[0], parts[1]);
    }

    public static bool IsTransparent(string blockId)
    {
        return blockId == "itoc:air" || blockId == "air" || String.IsNullOrEmpty(blockId) || !BlockManager.Instance.GetBlock(blockId).IsOpaque;
    }

    public Color Color { get; set; } = Colors.White;
    public float Hardness { get; set; } = 1.0f;
    public bool IsSolid { get; set; } = true;
    public bool IsOpaque { get; set; } = true;
    public bool IsLightSource { get; set; } = false;

    public float LightStrength { get; set; } = 0;
    // public virtual string[] ModelTypes => new[] { "cube" };

    public abstract void LoadResources();

    public abstract Material GetMaterial(Direction face = Direction.PositiveY);
    // public virtual Mesh GetMesh(string modelType = "cube") => null;
}
