using Godot;
using ITOC.Core.Items.Models;

namespace ITOC.Core.Items;

/// <summary>
/// An item that can be consumed to provide effects or benefits
/// </summary>
public class ConsumableItem : Item
{
    /// <summary>
    /// The type of consumable this represents
    /// </summary>
    public ConsumableType ConsumableType { get; }

    /// <summary>
    /// The effects that this consumable provides when consumed
    /// </summary>
    public List<ConsumableEffect> Effects { get; }

    /// <summary>
    /// The time it takes to consume this item (in seconds)
    /// </summary>
    public float ConsumeTime { get; }

    /// <summary>
    /// Whether this consumable can be used
    /// </summary>
    public override bool CanUse => true;

    /// <summary>
    /// Creates a new consumable item
    /// </summary>
    /// <param name="id">The identifier for this consumable</param>
    /// <param name="name">The name of this consumable</param>
    /// <param name="itemModel">The model used to render this consumable</param>
    /// <param name="consumableType">The type of consumable</param>
    /// <param name="effects">The effects this consumable provides</param>
    /// <param name="consumeTime">The time it takes to consume this item</param>
    /// <param name="description">The description of this consumable</param>
    /// <param name="properties">Additional properties for this consumable</param>
    public ConsumableItem(Identifier id, string name, IItemModel itemModel, ConsumableType consumableType,
                         List<ConsumableEffect> effects = null, float consumeTime = 1.0f,
                         string description = "", ItemProperties properties = null)
        : base(id, name, itemModel, description, properties ?? ItemProperties.Consumable)
    {
        ConsumableType = consumableType;
        Effects = effects ?? new List<ConsumableEffect>();
        ConsumeTime = consumeTime;
    }

    /// <summary>
    /// Consumes this item and applies its effects
    /// </summary>
    /// <param name="context">The usage context</param>
    /// <returns>The result of consuming the item</returns>
    public override ItemUseResult Use(ItemUseContext context)
    {
        if (!CanUse)
            return ItemUseResult.Failed;

        try
        {
            // Apply all effects from this consumable
            foreach (var effect in Effects)
            {
                ApplyEffect(effect, context);
            }

            // TODO: Play consumption sound/animation
            // TODO: Start consumption timer if ConsumeTime > 0

            return ItemUseResult.Consumed;
        }
        catch (Exception)
        {
            return ItemUseResult.Failed;
        }
    }

    /// <summary>
    /// Applies a specific effect from this consumable
    /// </summary>
    /// <param name="effect">The effect to apply</param>
    /// <param name="context">The usage context</param>
    protected virtual void ApplyEffect(ConsumableEffect effect, ItemUseContext context)
    {
        switch (effect.Type)
        {
            case EffectType.Health:
                ApplyHealthEffect(effect, context);
                break;
            case EffectType.Hunger:
                ApplyHungerEffect(effect, context);
                break;
            case EffectType.Speed:
                ApplySpeedEffect(effect, context);
                break;
            case EffectType.Strength:
                ApplyStrengthEffect(effect, context);
                break;
            case EffectType.Poison:
                ApplyPoisonEffect(effect, context);
                break;
            case EffectType.Regeneration:
                ApplyRegenerationEffect(effect, context);
                break;
            default:
                GD.PrintErr($"Unknown effect type: {effect.Type}");
                break;
        }
    }

    /// <summary>
    /// Applies a health effect
    /// </summary>
    /// <param name="effect">The health effect</param>
    /// <param name="context">The usage context</param>
    protected virtual void ApplyHealthEffect(ConsumableEffect effect, ItemUseContext context)
    {
        // TODO: Integrate with player health system
        GD.Print($"Applied health effect: {effect.Magnitude} for {effect.Duration} seconds");
    }

    /// <summary>
    /// Applies a hunger effect
    /// </summary>
    /// <param name="effect">The hunger effect</param>
    /// <param name="context">The usage context</param>
    protected virtual void ApplyHungerEffect(ConsumableEffect effect, ItemUseContext context)
    {
        // TODO: Integrate with player hunger system
        GD.Print($"Applied hunger effect: {effect.Magnitude} for {effect.Duration} seconds");
    }

    /// <summary>
    /// Applies a speed effect
    /// </summary>
    /// <param name="effect">The speed effect</param>
    /// <param name="context">The usage context</param>
    protected virtual void ApplySpeedEffect(ConsumableEffect effect, ItemUseContext context)
    {
        // TODO: Integrate with player movement system
        GD.Print($"Applied speed effect: {effect.Magnitude}x for {effect.Duration} seconds");
    }

    /// <summary>
    /// Applies a strength effect
    /// </summary>
    /// <param name="effect">The strength effect</param>
    /// <param name="context">The usage context</param>
    protected virtual void ApplyStrengthEffect(ConsumableEffect effect, ItemUseContext context)
    {
        // TODO: Integrate with player damage system
        GD.Print($"Applied strength effect: +{effect.Magnitude} damage for {effect.Duration} seconds");
    }

    /// <summary>
    /// Applies a poison effect
    /// </summary>
    /// <param name="effect">The poison effect</param>
    /// <param name="context">The usage context</param>
    protected virtual void ApplyPoisonEffect(ConsumableEffect effect, ItemUseContext context)
    {
        // TODO: Integrate with player health/status effect system
        GD.Print($"Applied poison effect: -{effect.Magnitude} health/sec for {effect.Duration} seconds");
    }

    /// <summary>
    /// Applies a regeneration effect
    /// </summary>
    /// <param name="effect">The regeneration effect</param>
    /// <param name="context">The usage context</param>
    protected virtual void ApplyRegenerationEffect(ConsumableEffect effect, ItemUseContext context)
    {
        // TODO: Integrate with player health/status effect system
        GD.Print($"Applied regeneration effect: +{effect.Magnitude} health/sec for {effect.Duration} seconds");
    }

    /// <summary>
    /// Gets the tooltip for this consumable
    /// </summary>
    /// <returns>The tooltip text</returns>
    public override string GetTooltip()
    {
        var tooltip = base.GetTooltip();

        tooltip += $"\n[color=yellow]Type:[/color] {ConsumableType}";

        if (ConsumeTime > 0)
        {
            tooltip += $"\n[color=gray]Consume Time: {ConsumeTime:F1}s[/color]";
        }

        if (Effects.Count > 0)
        {
            tooltip += "\n[color=cyan]Effects:[/color]";
            foreach (var effect in Effects)
            {
                tooltip += $"\n  • {GetEffectDescription(effect)}";
            }
        }

        return tooltip;
    }

    /// <summary>
    /// Gets a description of an effect for display in tooltips
    /// </summary>
    /// <param name="effect">The effect to describe</param>
    /// <returns>A human-readable description of the effect</returns>
    protected virtual string GetEffectDescription(ConsumableEffect effect)
    {
        var sign = effect.Magnitude >= 0 ? "+" : "";
        var duration = effect.Duration > 0 ? $" for {effect.Duration}s" : "";

        return effect.Type switch
        {
            EffectType.Health => $"Health {sign}{effect.Magnitude}",
            EffectType.Hunger => $"Hunger {sign}{effect.Magnitude}",
            EffectType.Speed => $"Speed {effect.Magnitude}x{duration}",
            EffectType.Strength => $"Strength {sign}{effect.Magnitude}{duration}",
            EffectType.Poison => $"Poison {effect.Magnitude}/s{duration}",
            EffectType.Regeneration => $"Regeneration {sign}{effect.Magnitude}/s{duration}",
            _ => $"{effect.Type} {sign}{effect.Magnitude}{duration}"
        };
    }
}

/// <summary>
/// Represents the type of consumable item
/// </summary>
public enum ConsumableType
{
    /// <summary>
    /// Food items that restore hunger
    /// </summary>
    Food,

    /// <summary>
    /// Potions that provide magical effects
    /// </summary>
    Potion,

    /// <summary>
    /// Medicine that heals or cures
    /// </summary>
    Medicine,

    /// <summary>
    /// Magical scrolls with one-time effects
    /// </summary>
    Scroll,

    /// <summary>
    /// Other consumable items
    /// </summary>
    Other
}

/// <summary>
/// Represents an effect that a consumable item can provide
/// </summary>
public class ConsumableEffect
{
    /// <summary>
    /// The type of effect
    /// </summary>
    public EffectType Type { get; set; }

    /// <summary>
    /// The magnitude/strength of the effect
    /// </summary>
    public float Magnitude { get; set; }

    /// <summary>
    /// The duration of the effect in seconds (0 for instant effects)
    /// </summary>
    public float Duration { get; set; }

    /// <summary>
    /// Creates a new consumable effect
    /// </summary>
    /// <param name="type">The type of effect</param>
    /// <param name="magnitude">The magnitude of the effect</param>
    /// <param name="duration">The duration of the effect</param>
    public ConsumableEffect(EffectType type, float magnitude, float duration = 0f)
    {
        Type = type;
        Magnitude = magnitude;
        Duration = duration;
    }

    /// <summary>
    /// Creates an instant healing effect
    /// </summary>
    /// <param name="amount">The amount of health to restore</param>
    /// <returns>A health effect</returns>
    public static ConsumableEffect InstantHealth(float amount) => new(EffectType.Health, amount, 0f);

    /// <summary>
    /// Creates an instant hunger restoration effect
    /// </summary>
    /// <param name="amount">The amount of hunger to restore</param>
    /// <returns>A hunger effect</returns>
    public static ConsumableEffect InstantHunger(float amount) => new(EffectType.Hunger, amount, 0f);

    /// <summary>
    /// Creates a speed boost effect
    /// </summary>
    /// <param name="multiplier">The speed multiplier</param>
    /// <param name="duration">The duration of the effect</param>
    /// <returns>A speed effect</returns>
    public static ConsumableEffect SpeedBoost(float multiplier, float duration) => new(EffectType.Speed, multiplier, duration);

    /// <summary>
    /// Creates a strength boost effect
    /// </summary>
    /// <param name="amount">The amount of extra damage</param>
    /// <param name="duration">The duration of the effect</param>
    /// <returns>A strength effect</returns>
    public static ConsumableEffect StrengthBoost(float amount, float duration) => new(EffectType.Strength, amount, duration);

    /// <summary>
    /// Creates a poison effect
    /// </summary>
    /// <param name="damagePerSecond">The damage per second</param>
    /// <param name="duration">The duration of the effect</param>
    /// <returns>A poison effect</returns>
    public static ConsumableEffect Poison(float damagePerSecond, float duration) => new(EffectType.Poison, damagePerSecond, duration);

    /// <summary>
    /// Creates a regeneration effect
    /// </summary>
    /// <param name="healingPerSecond">The healing per second</param>
    /// <param name="duration">The duration of the effect</param>
    /// <returns>A regeneration effect</returns>
    public static ConsumableEffect Regeneration(float healingPerSecond, float duration) => new(EffectType.Regeneration, healingPerSecond, duration);
}

/// <summary>
/// Represents the type of effect that can be applied
/// </summary>
public enum EffectType
{
    /// <summary>
    /// Affects player health
    /// </summary>
    Health,

    /// <summary>
    /// Affects player hunger
    /// </summary>
    Hunger,

    /// <summary>
    /// Affects player movement speed
    /// </summary>
    Speed,

    /// <summary>
    /// Affects player damage/strength
    /// </summary>
    Strength,

    /// <summary>
    /// Applies poison damage over time
    /// </summary>
    Poison,

    /// <summary>
    /// Applies healing over time
    /// </summary>
    Regeneration,

    /// <summary>
    /// Affects player jumping ability
    /// </summary>
    JumpBoost,

    /// <summary>
    /// Provides night vision
    /// </summary>
    NightVision,

    /// <summary>
    /// Provides underwater breathing
    /// </summary>
    WaterBreathing,

    /// <summary>
    /// Provides fire resistance
    /// </summary>
    FireResistance
}
