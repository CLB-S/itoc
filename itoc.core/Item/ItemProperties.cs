namespace ITOC.Core.Items;

/// <summary>
/// Defines the properties and characteristics of an item
/// </summary>
public class ItemProperties
{
    /// <summary>
    /// The maximum number of this item that can be stacked in a single slot
    /// </summary>
    public int MaxStackSize { get; set; } = 64;

    /// <summary>
    /// The rarity tier of this item
    /// </summary>
    public ItemRarity Rarity { get; set; } = ItemRarity.Common;

    /// <summary>
    /// Whether this item can be consumed (used up when used)
    /// </summary>
    public bool IsConsumable { get; set; } = false;

    /// <summary>
    /// Whether this item has durability and can break
    /// </summary>
    public bool HasDurability { get; set; } = false;

    /// <summary>
    /// The maximum durability of this item (if HasDurability is true)
    /// </summary>
    public int MaxDurability { get; set; } = 0;

    /// <summary>
    /// Whether this item can be crafted
    /// </summary>
    public bool IsCraftable { get; set; } = true;

    /// <summary>
    /// Whether this item can be enchanted
    /// </summary>
    public bool IsEnchantable { get; set; } = false;

    /// <summary>
    /// Whether this item should glow in the dark or have special visual effects
    /// </summary>
    public bool HasGlow { get; set; } = false;

    /// <summary>
    /// The fuel value of this item (for use in furnaces, etc.)
    /// </summary>
    public int FuelValue { get; set; } = 0;

    /// <summary>
    /// Whether this item can be used as fuel
    /// </summary>
    public bool IsFuel => FuelValue > 0;

    /// <summary>
    /// Custom properties that can be used by specific item types
    /// </summary>
    public Dictionary<string, object> CustomProperties { get; set; } = new();

    /// <summary>
    /// Default item properties
    /// </summary>
    public static ItemProperties Default => new();

    /// <summary>
    /// Creates properties for a single-use consumable item
    /// </summary>
    public static ItemProperties Consumable => new()
    {
        MaxStackSize = 64,
        IsConsumable = true,
        Rarity = ItemRarity.Common
    };

    /// <summary>
    /// Creates properties for a tool with durability
    /// </summary>
    /// <param name="maxDurability">The maximum durability of the tool</param>
    /// <param name="rarity">The rarity of the tool</param>
    /// <returns>Properties for a tool item</returns>
    public static ItemProperties Tool(int maxDurability, ItemRarity rarity = ItemRarity.Common) => new()
    {
        MaxStackSize = 1,
        HasDurability = true,
        MaxDurability = maxDurability,
        IsEnchantable = true,
        Rarity = rarity
    };

    /// <summary>
    /// Creates properties for a block item
    /// </summary>
    /// <param name="stackSize">The maximum stack size</param>
    /// <returns>Properties for a block item</returns>
    public static ItemProperties Block(int stackSize = 64) => new()
    {
        MaxStackSize = stackSize,
        Rarity = ItemRarity.Common
    };

    /// <summary>
    /// Creates properties for a rare or special item
    /// </summary>
    /// <param name="rarity">The rarity of the item</param>
    /// <param name="stackSize">The maximum stack size</param>
    /// <returns>Properties for a rare item</returns>
    public static ItemProperties Rare(ItemRarity rarity, int stackSize = 64) => new()
    {
        MaxStackSize = stackSize,
        Rarity = rarity,
        HasGlow = rarity >= ItemRarity.Epic
    };

    /// <summary>
    /// Creates properties for a fuel item
    /// </summary>
    /// <param name="fuelValue">The fuel value</param>
    /// <param name="stackSize">The maximum stack size</param>
    /// <returns>Properties for a fuel item</returns>
    public static ItemProperties Fuel(int fuelValue, int stackSize = 64) => new()
    {
        MaxStackSize = stackSize,
        FuelValue = fuelValue,
        Rarity = ItemRarity.Common
    };
}

/// <summary>
/// Represents the rarity tier of an item, affecting its visual appearance and perceived value
/// </summary>
public enum ItemRarity
{
    /// <summary>
    /// Common items (white text)
    /// </summary>
    Common = 0,

    /// <summary>
    /// Uncommon items (green text)
    /// </summary>
    Uncommon = 1,

    /// <summary>
    /// Rare items (blue text)
    /// </summary>
    Rare = 2,

    /// <summary>
    /// Epic items (purple text)
    /// </summary>
    Epic = 3,

    /// <summary>
    /// Legendary items (orange text)
    /// </summary>
    Legendary = 4,

    /// <summary>
    /// Mythic items (red text, very rare)
    /// </summary>
    Mythic = 5
}
