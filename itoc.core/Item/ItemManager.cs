using ITOC.Core.Registry;
using ITOC.Core.Items;
using ITOC.Core.Items.Models;

namespace ITOC.Core;

/// <summary>
/// Central manager for all items in the game.
/// Handles item registration, lookup, and provides default items.
/// </summary>
public class ItemManager
{
    private static ItemManager _instance;
    public static ItemManager Instance => _instance ??= new ItemManager();

    /// <summary>
    /// The item registry
    /// </summary>
    public Registry<Item> ItemRegistry { get; }

    /// <summary>
    /// The tag manager for items (used for creative mode tabs, etc.)
    /// </summary>
    public TagManager<Item> ItemTags { get; }

    private ItemManager()
    {
        // Get or create the item registry from the registry manager
        if (RegistryManager.Instance.TryGetRegistry<Item>(RegistryManager.Keys.Items, out var existingRegistry))
        {
            ItemRegistry = existingRegistry;
        }
        else
        {
            ItemRegistry = RegistryManager.Instance.CreateRegistry<Item>(RegistryManager.Keys.Items);
        }

        // Create a tag manager for items
        ItemTags = new TagManager<Item>(ItemRegistry);

        // Register default items
        RegisterDefaultItems();

        // Create default item tags
        CreateDefaultTags();
    }

    /// <summary>
    /// Registers all default items in the game
    /// </summary>
    private void RegisterDefaultItems()
    {
        // Register block items for all registered blocks
        RegisterBlockItems();

        // Register tools
        RegisterTools();

        // Register consumables
        RegisterConsumables();

        // Register special items
        RegisterSpecialItems();
    }

    /// <summary>
    /// Registers block items for all blocks in the block registry
    /// </summary>
    private void RegisterBlockItems()
    {
        var blockManager = BlockManager.Instance;

        foreach (var blockEntry in blockManager.BlockRegistry.Entries)
        {
            var block = blockEntry.Value;

            // Skip air block - it shouldn't have an item
            if (block == Block.Air)
                continue;

            // Create a block item for this block
            if (block is CubeBlock cubeBlock)
            {
                var blockItem = new BlockItem(block, new CubeBlockItemModel(cubeBlock));
                RegisterItem(blockItem);
            }
        }
    }

    /// <summary>
    /// Registers default tool items
    /// </summary>
    private void RegisterTools()
    {
        // Wooden tools
        // RegisterItem(new ToolItem(
        //     new Identifier("itoc", "wooden_pickaxe"),
        //     "Wooden Pickaxe",
        //     ToolType.Pickaxe,
        //     1.0f,
        //     59,
        //     "A basic pickaxe made of wood"
        // ));

    }

    /// <summary>
    /// Registers default consumable items
    /// </summary>
    private void RegisterConsumables()
    {
        // Food items
        // RegisterItem(new ConsumableItem(
        //     new Identifier("itoc", "apple"),
        //     "Apple",
        //     ConsumableType.Food,
        //     new List<ConsumableEffect> { ConsumableEffect.InstantHunger(4) },
        //     1.5f,
        //     "A crisp, red apple that restores hunger"
        // ));

        // Potions
        // RegisterItem(new ConsumableItem(
        //     new Identifier("itoc", "health_potion"),
        //     "Health Potion",
        //     ConsumableType.Potion,
        //     new List<ConsumableEffect> { ConsumableEffect.InstantHealth(20) },
        //     2.0f,
        //     "A magical potion that instantly restores health",
        //     ItemProperties.Rare(ItemRarity.Rare, 16)
        // ));

    }

    /// <summary>
    /// Registers special and unique items
    /// </summary>
    private void RegisterSpecialItems()
    {
        // TODO: Add special items like rare materials, crafting components, etc.

        // Example special item
        // RegisterItem(new Item(
        //     new Identifier("itoc", "diamond"),
        //     "Diamond",
        //     "A precious gemstone used in crafting",
        //     ItemProperties.Rare(ItemRarity.Rare, 64)
        // ) { });
    }

    /// <summary>
    /// Creates default item tags for organizing items (like creative mode tabs)
    /// </summary>
    private void CreateDefaultTags()
    {
        // Building blocks tag
        var buildingBlocksTag = ItemTags.CreateTag("itoc:building_blocks");
        foreach (var item in GetItemsByType<BlockItem>())
            buildingBlocksTag.Add(item.Id);

        // Tools tag
        var toolsTag = ItemTags.CreateTag("itoc:tools");
        foreach (var item in GetItemsByType<ToolItem>())
            toolsTag.Add(item.Id);

        // Food tag
        var foodTag = ItemTags.CreateTag("itoc:food");
        foreach (var item in GetItemsByType<ConsumableItem>())
            if (item.ConsumableType == ConsumableType.Food)
                foodTag.Add(item.Id);

        // Potions tag
        var potionsTag = ItemTags.CreateTag("itoc:potions");
        foreach (var item in GetItemsByType<ConsumableItem>())
            if (item.ConsumableType == ConsumableType.Potion)
                potionsTag.Add(item.Id);

        // Materials tag (for crafting materials)
        var materialsTag = ItemTags.CreateTag("itoc:materials");
        foreach (var item in ItemRegistry.GetEntries())
            if (item.Rarity >= ItemRarity.Rare && !(item is BlockItem) && !(item is ToolItem) && !(item is ConsumableItem))
                materialsTag.Add(item.Id);

        // Combat tag
        var combatTag = ItemTags.CreateTag("itoc:combat");
        foreach (var item in GetItemsByType<ToolItem>())
            if (item.ToolType == ToolType.Sword)
                combatTag.Add(item.Id);
    }

    /// <summary>
    /// Registers an item in the item registry
    /// </summary>
    /// <param name="item">The item to register</param>
    public void RegisterItem(Item item)
    {
        try
        {
            ItemRegistry.Register(item.Id, item);
        }
        catch (ArgumentException e)
        {
            throw new ArgumentException($"Failed to register item {item.Id}: {e.Message}", e);
        }
    }

    /// <summary>
    /// Gets an item by its identifier
    /// </summary>
    /// <param name="itemId">The identifier of the item</param>
    /// <returns>The item, or null if not found</returns>
    public Item GetItem(Identifier itemId)
    {
        return ItemRegistry.Get(itemId);
    }

    /// <summary>
    /// Gets an item by its string identifier
    /// </summary>
    /// <param name="itemId">The string identifier of the item</param>
    /// <returns>The item, or null if not found</returns>
    public Item GetItem(string itemId)
    {
        return GetItem(new Identifier(itemId));
    }

    /// <summary>
    /// Tries to get an item by its identifier
    /// </summary>
    /// <param name="itemId">The identifier of the item</param>
    /// <param name="item">The found item</param>
    /// <returns>True if the item was found</returns>
    public bool TryGetItem(Identifier itemId, out Item item)
    {
        return ItemRegistry.TryGet(itemId, out item);
    }

    /// <summary>
    /// Gets all items of a specific type
    /// </summary>
    /// <typeparam name="T">The type of item to get</typeparam>
    /// <returns>All items of the specified type</returns>
    public IEnumerable<T> GetItemsByType<T>() where T : Item
    {
        return ItemRegistry.GetEntries().OfType<T>();
    }

    /// <summary>
    /// Gets all items in a specific tag
    /// </summary>
    /// <param name="tagId">The identifier of the tag</param>
    /// <returns>All items in the tag</returns>
    public IEnumerable<Item> GetItemsByTag(Identifier tagId)
    {
        if (ItemTags.TryGetTag(tagId, out var tag))
            return tag.GetEntries();

        return Enumerable.Empty<Item>();
    }

    /// <summary>
    /// Gets all items in a specific tag
    /// </summary>
    /// <param name="tagId">The string identifier of the tag</param>
    /// <returns>All items in the tag</returns>
    public IEnumerable<Item> GetItemsByTag(string tagId)
    {
        return GetItemsByTag(new Identifier(tagId));
    }

    /// <summary>
    /// Gets the total number of registered items
    /// </summary>
    /// <returns>The number of items</returns>
    public int GetItemCount()
    {
        return ItemRegistry.Count;
    }

    /// <summary>
    /// Gets all available item tags
    /// </summary>
    /// <returns>All item tags</returns>
    public IEnumerable<RegistryTag<Item>> GetAllTags()
    {
        return ItemTags.GetAllTags();
    }

    /// <summary>
    /// Creates a block item for a specific block
    /// </summary>
    /// <param name="block">The block to create an item for</param>
    /// <returns>The created block item</returns>
    public BlockItem CreateBlockItem(Block block)
    {
        if (block is CubeBlock cubeBlock)
            return new BlockItem(block, new CubeBlockItemModel(cubeBlock));

        throw new NotImplementedException($"Block item creation not implemented for block type {block.GetType().Name}");
    }
}
