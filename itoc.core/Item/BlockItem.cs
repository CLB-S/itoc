using Godot;
using ITOC.Core.Items.Models;

namespace ITOC.Core.Items;

/// <summary>
/// An item that represents a block that can be placed in the world
/// </summary>
public class BlockItem : Item
{
    /// <summary>
    /// The block that this item represents
    /// </summary>
    public Block Block { get; }

    /// <summary>
    /// Whether this block item can be placed
    /// </summary>
    public override bool CanUse => true;

    /// <summary>
    /// Creates a new block item
    /// </summary>
    /// <param name="block">The block that this item represents</param>
    /// <param name="itemModel">The model used to render this item</param>
    /// <param name="properties">The properties of this item</param>
    public BlockItem(Block block, IItemModel itemModel, ItemProperties properties = null)
        : base(
            new Identifier(block.Id.Namespace, $"{block.Id.Path}_item"),
            block.Name,
            itemModel,
            $"Places a {block.Name} block",
            properties ?? ItemProperties.Block())
    {
        Block = block ?? throw new ArgumentNullException(nameof(block));
    }

    /// <summary>
    /// Creates a new block item with a custom identifier
    /// </summary>
    /// <param name="id">The identifier for this item</param>
    /// <param name="block">The block that this item represents</param>
    /// <param name="properties">The properties of this item</param>
    public BlockItem(Identifier id, Block block, IItemModel itemModel, ItemProperties properties = null)
        : base(id, block.Name, itemModel, $"Places a {block.Name} block", properties ?? ItemProperties.Block())
    {
        Block = block ?? throw new ArgumentNullException(nameof(block));
    }

    /// <summary>
    /// Places the block in the world when used
    /// </summary>
    /// <param name="context">The usage context</param>
    /// <returns>The result of placing the block</returns>
    public override ItemUseResult UseOnWorld(ItemUseContext context)
    {
        if (context?.World == null)
            return ItemUseResult.Failed;

        try
        {
            // Calculate the position to place the block
            var placePosition = CalculatePlacePosition(context);

            // Check if the position is valid for placement
            if (!CanPlaceAt(context.World, placePosition))
                return ItemUseResult.Failed;

            // Place the block
            context.World.SetBlock(placePosition, Block.Id);

            return ItemUseResult.Consumed;
        }
        catch (Exception)
        {
            return ItemUseResult.Failed;
        }
    }

    /// <summary>
    /// Places the block adjacent to the targeted block
    /// </summary>
    /// <param name="context">The usage context</param>
    /// <returns>The result of placing the block</returns>
    public override ItemUseResult UseOnBlock(ItemUseContext context)
    {
        if (context?.World == null || context.TargetBlock == null)
            return ItemUseResult.Failed;

        try
        {
            // Calculate position adjacent to the clicked face
            var placePosition = context.TargetBlockPosition + context.ClickedFace;

            // Check if the position is valid for placement
            if (!CanPlaceAt(context.World, placePosition))
                return ItemUseResult.Failed;

            // Place the block
            context.World.SetBlock(placePosition, Block.Id);

            return ItemUseResult.Consumed;
        }
        catch (Exception)
        {
            return ItemUseResult.Failed;
        }
    }

    /// <summary>
    /// Calculates the position where the block should be placed
    /// </summary>
    /// <param name="context">The usage context</param>
    /// <returns>The position to place the block</returns>
    protected virtual Vector3 CalculatePlacePosition(ItemUseContext context)
    {
        // By default, place at the clicked position
        return context.Position;
    }

    /// <summary>
    /// Checks if a block can be placed at the specified position
    /// </summary>
    /// <param name="world">The world to check</param>
    /// <param name="position">The position to check</param>
    /// <returns>True if the block can be placed</returns>
    protected virtual bool CanPlaceAt(World world, Vector3 position)
    {
        // TODO: Remove to World.

        if (world == null)
            return false;

        // Check if the current block at this position is air or can be replaced
        var currentBlock = world.GetBlock(position);
        return currentBlock == null || currentBlock == Block.Air;
    }

    /// <summary>
    /// Gets the tooltip for this block item
    /// </summary>
    /// <returns>The tooltip text</returns>
    public override string GetTooltip()
    {
        var baseTooltip = base.GetTooltip();
        return baseTooltip + $"\n[color=gray]Right-click to place[/color]";
    }
}
