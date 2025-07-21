using Friflo.Engine.ECS;
using Godot;

namespace ITOC.Core.Item;

public class ItemManager
{
    private static ItemManager _instance;
    public static ItemManager Instance => _instance ??= new ItemManager();

    private readonly EntityStore _registeredItems;

    public readonly ComponentIndex<ItemComponent, Identifier> ItemIndex;

    private ItemManager()
    {
        _registeredItems = new EntityStore();
        ItemIndex = _registeredItems.ComponentIndex<ItemComponent, Identifier>();

        // Register default items
        RegisterDefaultItems();
    }

    /// <summary>
    /// Registers a new item. Additional components should be added to the returned entity after registration.
    /// </summary>
    /// <param name="item">The item component to register.</param>
    /// <param name="tags">Optional tags to associate with the item.</param>
    /// <returns>The registered entity.</returns>
    public Friflo.Engine.ECS.Entity RegisterItem(ItemComponent item, in Tags tags = default)
    {
        var existingItems = ItemIndex[item.Id];
        if (existingItems.Count > 0)
        {
            GD.PrintErr($"Item with ID {item.Id} is already registered.");
            return existingItems[0];
        }

        return _registeredItems.CreateEntity(item, tags);
    }

    public void CloneItemTo(in Identifier itemId, EntityStore targetStore)
    {
        var existingItems = ItemIndex[itemId];
        if (existingItems.Count == 0)
        {
            GD.PrintErr($"Item with ID {itemId} does not exist.");
            return;
        }

        var sourceItem = existingItems[0];
        var clonedItem = targetStore.CreateEntity();
        sourceItem.CopyEntity(clonedItem);
    }

    private void RegisterDefaultItems()
    {
        // Example of registering a default item
        var defaultItem = new ItemComponent
        {
            Id = new Identifier("itoc:default_item"),
            Name = "Default Item",
            Description = "This is a default item for testing purposes.",
            RenderingModel = null, // Set to an actual rendering model if available
        };

        RegisterItem(defaultItem);

        var anotherItem = new ItemComponent
        {
            Id = new Identifier("itoc:another_item"),
            Name = "Another Item",
            Description = "This is another default item.",
            RenderingModel = null,
        };

        RegisterItem(anotherItem);

        var another_item = ItemIndex["itoc:another_item"];
        var test = ItemIndex["itoc:test"];
        var default_item = ItemIndex["itoc:default_item"];
    }
}
