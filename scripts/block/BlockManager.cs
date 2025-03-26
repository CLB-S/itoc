using Godot;
using System;
using System.Collections.Generic;

public class BlockManager
{
    private static BlockManager _instance;
    public static BlockManager Instance => _instance ??= new BlockManager();

    private readonly Dictionary<ushort, Block> _blocks = new();
    private readonly Dictionary<string, ushort> _nameToId = new();

    public void RegisterBlock(Block block)
    {
        if (_blocks.ContainsKey(block.BlockID))
        {
            throw new ArgumentException($"Block ID {block.BlockID} already exists");
        }

        _blocks[block.BlockID] = block;
        _nameToId[block.BlockName] = block.BlockID;
        block.LoadResources();
    }

    public Block GetBlock(ushort id) => _blocks.TryGetValue(id, out var block) ? block : null;
    public Block GetBlock(string name) =>
        _nameToId.TryGetValue(name, out var id) ? GetBlock(id) : null;

    public int GetBlockCount() => _blocks.Count;
}
