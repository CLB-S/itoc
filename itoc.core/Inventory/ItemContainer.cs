using Friflo.Engine.ECS;
using ITOC.Core.Item;

namespace ITOC.Core.Inventory;

public abstract class ItemContainer
{
    private readonly EntityStore _items;
    public readonly ComponentIndex<ItemComponent, Identifier> ItemIndex;

    public event EventHandler Updated;

    public ItemContainer()
    {
        _items = new EntityStore();
        ItemIndex = _items.ComponentIndex<ItemComponent, Identifier>();
    }

    public virtual int NumberOfItem(Identifier itemId)
    {
        var item = ItemIndex[itemId];
        if (item.Count == 0)
            return 0;

        if (!item[0].HasComponent<StackComponent>())
            return item.Count;

        var count = 0;
        foreach (var entity in item)
            count += entity.GetComponent<StackComponent>().Count;

        return count;
    }

    public bool HasItem(Identifier itemId) => ItemIndex[itemId].Count > 0;
}
