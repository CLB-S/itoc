using Godot;
using ITOC.Core.Registry;
using ITOC.Core.BlockModels;

namespace ITOC.Core;

public class BlockManager
{
    private static BlockManager _instance;
    public static BlockManager Instance => _instance ??= new BlockManager();

    // private readonly Dictionary<string, Block> _blocks = new();


    /// <summary>
    /// The block registry
    /// </summary>
    public Registry<Block> BlockRegistry { get; }

    /// <summary>
    /// The tag manager for blocks
    /// </summary>
    public TagManager<Block> BlockTags { get; }

    private BlockManager()
    {
        // Get the block registry from the registry manager
        BlockRegistry = RegistryManager.Instance.GetRegistry<Block>(RegistryManager.Keys.Blocks);

        // Create a tag manager for blocks
        BlockTags = new TagManager<Block>(BlockRegistry);

        // Register default blocks
        RegisterDefaultBlocks();

        // Create default tags
        // CreateDefaultTags();

        TextureManager.Instance.BuildTextureArray();
    }

    private void RegisterDefaultBlocks()
    {
        RegisterBlock(Block.Air);
        RegisterBlock(new CubeBlock("itoc:debug", "Debug Block", new CubeAllModel("res://assets/blocks/debug.png")));
        RegisterBlock(new CubeBlock("itoc:stone", "Stone", new CubeAllModel("res://assets/blocks/stone.png")));
        RegisterBlock(new CubeBlock("itoc:dirt", "Dirt", new CubeAllModel("res://assets/blocks/dirt.png")));
        RegisterBlock(new CubeBlock("itoc:sand", "Sand", new CubeAllModel("res://assets/blocks/sand.png")));
        RegisterBlock(new CubeBlock("itoc:snow", "Snow", new CubeAllModel("res://assets/blocks/snow.png")));

        var waterMaterial = ResourceLoader.Load<Material>("res://assets/materials/water_material.tres");
        RegisterBlock(new CubeBlock("itoc:water", "Water", new CubeAllModel(waterMaterial), BlockProperties.Transparent));
        RegisterBlock(new DirectionalCubeBlock("itoc:grass_block", "Grass Block", new CubeBottomTopModel("res://assets/blocks/grass_block/round.png", "res://assets/blocks/dirt.png", "res://assets/blocks/grass_block/top.png"),
            null, Direction.PositiveY));
    }


    public void RegisterBlock(Block block)
    {
        try
        {
            BlockRegistry.Register(block.Id, block);

            // Also register the block as an item
            // var itemRegistry = RegistryManager.Instance.GetRegistry<IItem>(RegistryManager.Keys.Items);
            // itemRegistry.Register(id, block);
        }
        catch (ArgumentException e)
        {
            throw new ArgumentException($"Failed to register block {block.Id}: {e.Message}", e);
        }
    }

    /// <summary>
    /// Gets a block by ID
    /// </summary>
    /// <param name="blockId">The ID of the block</param>
    /// <returns>The block, or null if not found</returns>
    public Block GetBlock(Identifier blockId)
    {
        return BlockRegistry.Get(blockId); // TODO: Air block and default block.
    }

    public int GetBlockCount()
    {
        return BlockRegistry.Count;
    }
}