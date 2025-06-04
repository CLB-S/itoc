using Godot;
using ITOC.Core.Items.Models;

namespace ITOC.Core.Items;

/// <summary>
/// An item that can be used as a tool to perform various actions
/// </summary>
public class ToolItem : Item
{
    /// <summary>
    /// The type of tool this represents
    /// </summary>
    public ToolType ToolType { get; }

    /// <summary>
    /// The efficiency/power level of this tool
    /// </summary>
    public float Efficiency { get; }

    /// <summary>
    /// The current durability of this tool
    /// </summary>
    public int CurrentDurability { get; protected set; }

    /// <summary>
    /// Whether this tool is broken (durability <= 0)
    /// </summary>
    public bool IsBroken => Properties.HasDurability && CurrentDurability <= 0;

    /// <summary>
    /// Whether this tool can be used (not broken)
    /// </summary>
    public override bool CanUse => !IsBroken;

    /// <summary>
    /// Creates a new tool item
    /// </summary>
    /// <param name="id">The identifier for this tool</param>
    /// <param name="name">The name of this tool</param>
    /// <param name="itemModel">The model used to render this item</param>
    /// <param name="toolType">The type of tool</param>
    /// <param name="efficiency">The efficiency of this tool</param>
    /// <param name="maxDurability">The maximum durability of this tool</param>
    /// <param name="description">The description of this tool</param>
    /// <param name="properties">Additional properties for this tool</param>
    public ToolItem(Identifier id, string name, IItemModel itemModel, ToolType toolType, float efficiency, int maxDurability,
                   string description = "", ItemProperties properties = null)
        : base(id, name, itemModel, description, properties ?? ItemProperties.Tool(maxDurability))
    {
        ToolType = toolType;
        Efficiency = efficiency;
        CurrentDurability = maxDurability;
    }

    /// <summary>
    /// Uses this tool on a block (mining, chopping, etc.)
    /// </summary>
    /// <param name="context">The usage context</param>
    /// <returns>The result of using the tool</returns>
    public override ItemUseResult UseOnBlock(ItemUseContext context)
    {
        if (!CanUse || context?.World == null || context.TargetBlock == null)
            return ItemUseResult.Failed;

        try
        {
            // Check if this tool is effective against the target block
            if (!IsEffectiveAgainst(context.TargetBlock))
                return ItemUseResult.NoEffect;

            // Perform the tool action (mine the block)
            var result = PerformToolAction(context);

            // Apply durability damage if the tool has durability
            if (Properties.HasDurability && result != ItemUseResult.Failed)
            {
                return ApplyDurabilityDamage(1);
            }

            return result;
        }
        catch (Exception)
        {
            return ItemUseResult.Failed;
        }
    }

    /// <summary>
    /// Performs the specific action for this tool type
    /// </summary>
    /// <param name="context">The usage context</param>
    /// <returns>The result of the tool action</returns>
    protected virtual ItemUseResult PerformToolAction(ItemUseContext context)
    {
        switch (ToolType)
        {
            case ToolType.Pickaxe:
            case ToolType.Axe:
            case ToolType.Shovel:
                return MineBlock(context);
            case ToolType.Sword:
                return AttackBlock(context);
            case ToolType.Hoe:
                return TillSoil(context);
            default:
                return ItemUseResult.NoEffect;
        }
    }

    /// <summary>
    /// Mines/breaks the target block
    /// </summary>
    /// <param name="context">The usage context</param>
    /// <returns>The mining result</returns>
    protected virtual ItemUseResult MineBlock(ItemUseContext context)
    {
        // Break the block and drop its items
        context.World.SetBlock(context.TargetBlockPosition, Block.Air.Id);

        // TODO: Drop the appropriate items based on the block and tool
        // This would involve a loot table system

        return ItemUseResult.Success;
    }

    /// <summary>
    /// Attacks the target block (for swords or combat tools)
    /// </summary>
    /// <param name="context">The usage context</param>
    /// <returns>The attack result</returns>
    protected virtual ItemUseResult AttackBlock(ItemUseContext context)
    {
        // Swords can break blocks quickly but with high durability cost
        context.World.SetBlock(context.TargetBlockPosition, Block.Air.Id);
        return ItemUseResult.Success;
    }

    /// <summary>
    /// Tills soil blocks (for hoes)
    /// </summary>
    /// <param name="context">The usage context</param>
    /// <returns>The tilling result</returns>
    protected virtual ItemUseResult TillSoil(ItemUseContext context)
    {
        // TODO: Convert dirt blocks to farmland
        // This would require additional block types
        return ItemUseResult.NoEffect;
    }

    /// <summary>
    /// Checks if this tool is effective against the specified block
    /// </summary>
    /// <param name="block">The block to check</param>
    /// <returns>True if the tool is effective</returns>
    protected virtual bool IsEffectiveAgainst(Block block)
    {
        if (block == null || block == Block.Air)
            return false;

        // TODO: Implement block hardness and tool effectiveness system
        // For now, basic effectiveness rules:
        return ToolType switch
        {
            ToolType.Pickaxe => block.Id.Path.Contains("stone") || block.Id.Path.Contains("ore"),
            ToolType.Axe => block.Id.Path.Contains("wood") || block.Id.Path.Contains("log"),
            ToolType.Shovel => block.Id.Path.Contains("dirt") || block.Id.Path.Contains("sand") || block.Id.Path.Contains("gravel"),
            ToolType.Sword => true, // Swords can break any block but inefficiently
            ToolType.Hoe => block.Id.Path.Contains("dirt") || block.Id.Path.Contains("grass"),
            _ => false
        };
    }

    /// <summary>
    /// Applies durability damage to this tool
    /// </summary>
    /// <param name="damage">The amount of damage to apply</param>
    /// <returns>The result after applying damage</returns>
    protected virtual ItemUseResult ApplyDurabilityDamage(int damage)
    {
        if (!Properties.HasDurability)
            return ItemUseResult.Success;

        CurrentDurability = Math.Max(0, CurrentDurability - damage);

        if (CurrentDurability <= 0)
        {
            return ItemUseResult.Broken;
        }
        else if (CurrentDurability <= Properties.MaxDurability * 0.1f) // Tool is about to break
        {
            return ItemUseResult.Damaged;
        }

        return ItemUseResult.Success;
    }

    /// <summary>
    /// Repairs this tool by the specified amount
    /// </summary>
    /// <param name="repairAmount">The amount to repair</param>
    public virtual void Repair(int repairAmount)
    {
        if (Properties.HasDurability)
        {
            CurrentDurability = Math.Min(Properties.MaxDurability, CurrentDurability + repairAmount);
        }
    }

    /// <summary>
    /// Gets the tooltip for this tool
    /// </summary>
    /// <returns>The tooltip text</returns>
    public override string GetTooltip()
    {
        var tooltip = base.GetTooltip();

        tooltip += $"\n[color=yellow]Tool Type:[/color] {ToolType}";
        tooltip += $"\n[color=yellow]Efficiency:[/color] {Efficiency:F1}";

        if (Properties.HasDurability)
        {
            var durabilityPercent = (float)CurrentDurability / Properties.MaxDurability * 100;
            var durabilityColor = durabilityPercent > 50 ? "green" : durabilityPercent > 25 ? "yellow" : "red";
            tooltip += $"\n[color={durabilityColor}]Durability:[/color] {CurrentDurability}/{Properties.MaxDurability}";
        }

        return tooltip;
    }
}

/// <summary>
/// Represents the type of tool
/// </summary>
public enum ToolType
{
    /// <summary>
    /// Used for mining stone and ore blocks
    /// </summary>
    Pickaxe,

    /// <summary>
    /// Used for chopping wood and plant blocks
    /// </summary>
    Axe,

    /// <summary>
    /// Used for digging dirt, sand, and similar blocks
    /// </summary>
    Shovel,

    /// <summary>
    /// Used for combat and can break any block (inefficiently)
    /// </summary>
    Sword,

    /// <summary>
    /// Used for tilling soil and farming
    /// </summary>
    Hoe,

    /// <summary>
    /// Used for shearing sheep and harvesting certain blocks
    /// </summary>
    Shears,

    /// <summary>
    /// Multi-purpose tool (less efficient than specialized tools)
    /// </summary>
    MultiTool
}
