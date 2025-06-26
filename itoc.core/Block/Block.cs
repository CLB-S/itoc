using ITOC.Core.Item;

namespace ITOC.Core;

public class Block : IEquatable<Block>, IItem
{
    public static readonly Block Air = new("itoc:air", "Air", BlockProperties.Transparent);

    public Identifier Id { get; }
    public string Name { get; }
    public bool IsOpaque { get; } = true;

    public ItemType Type => ItemType.Block;

    public string Description => "";

    protected Block(Identifier id, string name, BlockProperties properties = null)
    {
        Id = id;
        Name = name;

        properties ??= BlockProperties.Default;
        IsOpaque = properties.IsOpaque;
    }

    public virtual bool Equals(Block other)
    {
        if (other == null)
            return false;
        if (ReferenceEquals(this, other))
            return true;

        return Id.Equals(other.Id);
    }
}
