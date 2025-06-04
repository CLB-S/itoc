using Godot;

namespace ITOC.Core.Items;

/// <summary>
/// Context information for item usage
/// </summary>
public class ItemUseContext
{
    /// <summary>
    /// The world where the item is being used
    /// </summary>
    public World World { get; set; }

    /// <summary>
    /// The position where the item is being used
    /// </summary>
    public Vector3 Position { get; set; }

    /// <summary>
    /// The direction the player is facing when using the item
    /// </summary>
    public Vector3 Direction { get; set; }

    /// <summary>
    /// The block that is being targeted (if any)
    /// </summary>
    public Block TargetBlock { get; set; }

    /// <summary>
    /// The position of the targeted block
    /// </summary>
    public Vector3 TargetBlockPosition { get; set; }

    /// <summary>
    /// The face of the block that was clicked
    /// </summary>
    public Vector3 ClickedFace { get; set; }

    /// <summary>
    /// Whether the player is sneaking/crouching
    /// </summary>
    public bool IsSneaking { get; set; }

    /// <summary>
    /// Additional context data that can be used by specific items
    /// </summary>
    public Dictionary<string, object> ExtraData { get; set; } = new();

    /// <summary>
    /// Creates a new item use context
    /// </summary>
    /// <param name="world">The world where the item is being used</param>
    /// <param name="position">The position where the item is being used</param>
    public ItemUseContext(World world, Vector3 position)
    {
        World = world ?? throw new ArgumentNullException(nameof(world));
        Position = position;
    }
}

/// <summary>
/// Represents the result of using an item
/// </summary>
public enum ItemUseResult
{
    /// <summary>
    /// The item had no effect
    /// </summary>
    NoEffect,

    /// <summary>
    /// The item was used successfully
    /// </summary>
    Success,

    /// <summary>
    /// The item was consumed during use
    /// </summary>
    Consumed,

    /// <summary>
    /// The item use failed
    /// </summary>
    Failed,

    /// <summary>
    /// The item was damaged during use
    /// </summary>
    Damaged,

    /// <summary>
    /// The item broke during use
    /// </summary>
    Broken
}
