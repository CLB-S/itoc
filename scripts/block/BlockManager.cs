using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class BlockManager
{
    private static BlockManager _instance;
    private readonly Dictionary<string, Block> _blocks = new();

    public BlockManager()
    {
        RegisterBlock(new BasicBlock("debug", "Debug Block"));
        RegisterBlock(new BasicBlock("stone", "Stone"));
        RegisterBlock(new BasicBlock("dirt", "Dirt"));
        RegisterBlock(new DirectionalBlock("grass_block", "Grass Block", Direction.PositiveY,
            textureBottomPath: "res://assets/blocks/dirt.png"));
    }

    public static BlockManager Instance => _instance ??= new BlockManager();

    public void RegisterBlock(Block block)
    {
        if (_blocks.ContainsKey(block.BlockId))
            throw new ArgumentException($"Block ID {block.BlockId} already exists");

        _blocks[block.BlockId] = block;
        block.LoadResources();
    }

    public Block GetBlock(string blockId)
    {
        return _blocks.TryGetValue(Block.NormalizeBlockId(blockId), out var block) ? block : null;
    }

    public int GetBlockCount()
    {
        return _blocks.Count;
    }
}
