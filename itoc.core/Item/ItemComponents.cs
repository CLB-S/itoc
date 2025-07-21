using Friflo.Engine.ECS;

namespace ITOC.Core.Item;

public struct ItemComponent : IIndexedComponent<Identifier>
{
    public Identifier Id;
    public string Name;
    public string Description;
    public IRenderingModel RenderingModel;

    public Identifier GetIndexedValue() => Id;
}

public struct DurabilityComponent : IComponent
{
    public int CurrentDurability;
    public int MaxDurability;

    /// <summary>
    /// If true, the item won't be destroyed when its durability reaches zero.
    /// </summary>
    public bool Unbreakable;
}

/// <summary>
/// Represents that the item can be stacked.
/// </summary>
public struct StackComponent : IComponent
{
    public int Count;
    public int MaxCount;
}

// Who used What at Where?
public struct UseComponent : IComponent
{
    public Action OnUse;
}

public struct ContinuousUseComponent : IComponent
{
    public float TotalUseTime;
    public float UseTimer;
    public Action OnUse;
}

public struct UsageCooldownComponent : IComponent
{
    public float CooldownTime;
    public float RemainingTime;
}

public struct PlacableComponent : IComponent
{
    public Block Block;
}
