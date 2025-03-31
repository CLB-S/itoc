using System;
using System.Collections.Generic;

public class BlockManager
{
    private static BlockManager _instance;

    private readonly Dictionary<uint, Block> _blocks = new();
    private readonly Dictionary<string, uint> _nameToId = new();

    public BlockManager()
    {
        RegisterBlock(new BasicBlock(1, "debug"));
        RegisterBlock(new BasicBlock(2, "stone"));
        RegisterBlock(new BasicBlock(3, "dirt"));
        RegisterBlock(new DirectionalBlock(4, "grass_block", Direction.PositiveY,
            textureBottomPath: "res://assets/blocks/dirt.png"));
    }

    public static BlockManager Instance => _instance ??= new BlockManager();

    public void RegisterBlock(Block block)
    {
        if (_blocks.ContainsKey(block.BlockId)) throw new ArgumentException($"Block ID {block.BlockId} already exists");

        _blocks[block.BlockId] = block;
        _nameToId[block.BlockName] = block.BlockId;
        block.LoadResources();
    }

    public Block GetBlock(uint id)
    {
        return _blocks.TryGetValue(id, out var block) ? block : null;
    }

    public Block GetBlock(string name)
    {
        return _nameToId.TryGetValue(name, out var id) ? GetBlock(id) : null;
    }

    public int GetBlockCount()
    {
        return _blocks.Count;
    }
}