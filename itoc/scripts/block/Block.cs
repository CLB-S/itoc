using System;
using ITOC.Models;

namespace ITOC;

public class Block : IEquatable<Block>, IItem
{
    public Identifier Id { get; }
    public string Name { get; }
    public bool IsOpaque { get; } = true;
    public CubeModelBase BlockModel { get; }

    public ItemType Type => ItemType.Block;

    public string Description => "";

    public Block(Identifier id, string name, CubeModelBase blockModel, BlockProperties properties = null)
    {
        Id = id;
        Name = name;
        BlockModel = blockModel ?? throw new ArgumentNullException(nameof(blockModel));

        properties ??= BlockProperties.Default;
        IsOpaque = properties.IsOpaque;
    }

    public virtual bool Equals(Block other)
    {
        if (other == null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Id.Equals(other.Id);
    }
}