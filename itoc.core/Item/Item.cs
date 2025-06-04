using ITOC.Core.Items.Models;

namespace ITOC.Core.Items;

/// <summary>
/// Base class for all items in the game.
/// Items represent objects that can be held in inventories, used by players, and have various behaviors.
/// </summary>
public abstract class Item : IEquatable<Item>
{
    /// <summary>
    /// The unique identifier for this item
    /// </summary>
    public Identifier Id { get; }

    /// <summary>
    /// The display name of this item
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The model used to render this item in the inventory and world
    /// </summary>
    public IItemModel ItemModel { get; }

    /// <summary>
    /// The description of this item
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// The properties of this item
    /// </summary>
    public ItemProperties Properties { get; }

    /// <summary>
    /// The maximum stack size for this item
    /// </summary>
    public int MaxStackSize => Properties.MaxStackSize;

    /// <summary>
    /// Whether this item is stackable
    /// </summary>
    public bool IsStackable => MaxStackSize > 1;

    /// <summary>
    /// The rarity tier of this item
    /// </summary>
    public ItemRarity Rarity => Properties.Rarity;

    /// <summary>
    /// Whether this item can be used
    /// </summary>
    public virtual bool CanUse => true;

    /// <summary>
    /// Creates a new item with the specified properties
    /// </summary>
    /// <param name="id">The unique identifier for this item</param>
    /// <param name="name">The display name of this item</param>
    /// <param name="description">The description of this item</param>
    /// <param name="properties">The properties of this item</param>
    protected Item(Identifier id, string name, IItemModel itemModel, string description = "", ItemProperties properties = null)
    {
        Id = id;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        ItemModel = itemModel ?? throw new ArgumentNullException(nameof(itemModel));
        Description = description ?? "";
        Properties = properties ?? ItemProperties.Default;
    }

    /// <summary>
    /// Called when this item is used by a player
    /// </summary>
    /// <param name="context">The usage context containing player, world, and other relevant information</param>
    /// <returns>The result of using this item</returns>
    public virtual ItemUseResult Use(ItemUseContext context)
    {
        return ItemUseResult.NoEffect;
    }

    /// <summary>
    /// Called when this item is right-clicked in the world
    /// </summary>
    /// <param name="context">The usage context containing player, world, position, and other relevant information</param>
    /// <returns>The result of using this item in the world</returns>
    public virtual ItemUseResult UseOnWorld(ItemUseContext context)
    {
        return ItemUseResult.NoEffect;
    }

    /// <summary>
    /// Called when this item is used on a block
    /// </summary>
    /// <param name="context">The usage context containing the target block and other relevant information</param>
    /// <returns>The result of using this item on the block</returns>
    public virtual ItemUseResult UseOnBlock(ItemUseContext context)
    {
        return ItemUseResult.NoEffect;
    }

    /// <summary>
    /// Gets the tooltip text for this item
    /// </summary>
    /// <returns>The tooltip text to display for this item</returns>
    public virtual string GetTooltip()
    {
        var tooltip = $"[b]{Name}[/b]";

        if (!string.IsNullOrEmpty(Description))
            tooltip += $"\n{Description}";

        if (Rarity != ItemRarity.Common)
            tooltip += $"\n[color={GetRarityColor()}]{Rarity}[/color]";

        return tooltip;
    }

    /// <summary>
    /// Gets the color associated with this item's rarity
    /// </summary>
    /// <returns>The rarity color</returns>
    protected virtual string GetRarityColor()
    {
        return Rarity switch
        {
            ItemRarity.Common => "white",
            ItemRarity.Uncommon => "green",
            ItemRarity.Rare => "blue",
            ItemRarity.Epic => "purple",
            ItemRarity.Legendary => "orange",
            ItemRarity.Mythic => "red",
            _ => "white"
        };
    }

    /// <summary>
    /// Determines if this item can be combined with another item for stacking
    /// </summary>
    /// <param name="other">The other item to check</param>
    /// <returns>True if the items can be stacked together</returns>
    public virtual bool CanStackWith(Item other)
    {
        if (other == null || !IsStackable || !other.IsStackable)
            return false;

        return Equals(other);
    }

    /// <summary>
    /// Checks if this item is equal to another item
    /// </summary>
    /// <param name="other">The other item to compare</param>
    /// <returns>True if the items are equal</returns>
    public virtual bool Equals(Item other)
    {
        if (other == null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Id.Equals(other.Id);
    }

    /// <summary>
    /// Checks if this object is equal to another object
    /// </summary>
    /// <param name="obj">The other object to compare</param>
    /// <returns>True if the objects are equal</returns>
    public override bool Equals(object obj)
    {
        return Equals(obj as Item);
    }

    /// <summary>
    /// Gets the hash code for this item
    /// </summary>
    /// <returns>The hash code</returns>
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    /// <summary>
    /// Gets the string representation of this item
    /// </summary>
    /// <returns>The string representation</returns>
    public override string ToString()
    {
        return $"Item[{Id}]: {Name}";
    }
}
