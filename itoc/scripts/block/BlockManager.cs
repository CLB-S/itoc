using System;
using System.Collections.Generic;
using ITOC.Models;

namespace ITOC;

public class BlockManager
{
    private static BlockManager _instance;
    private readonly Dictionary<string, Block> _blocks = new();

    public BlockManager()
    {
        RegisterBlock(new Block("itoc:debug", "Debug Block", new CubeAllModel("res://assets/blocks/debug.png")));
        RegisterBlock(new Block("itoc:stone", "Stone", new CubeAllModel("res://assets/blocks/stone.png")));
        RegisterBlock(new Block("itoc:dirt", "Dirt", new CubeAllModel("res://assets/blocks/dirt.png")));
        RegisterBlock(new Block("itoc:sand", "Sand", new CubeAllModel("res://assets/blocks/sand.png")));
        RegisterBlock(new Block("itoc:snow", "Snow", new CubeAllModel("res://assets/blocks/snow.png")));

        var waterMaterial = new MaterialSettings("res://assets/blocks/water_material.tres");
        RegisterBlock(new Block("itoc:water", "Water", new CubeAllModel(waterMaterial), BlockProperties.Transparent));
        RegisterBlock(new DirectionalBlock("itoc:grass_block", "Grass Block", new CubeBottomTopModel("res://assets/blocks/grass_block/round.png", "res://assets/blocks/dirt.png", "res://assets/blocks/grass_block/top.png"),
            null, Direction.PositiveY));
    }

    public static BlockManager Instance => _instance ??= new BlockManager();

    public void RegisterBlock(Block block)
    {
        if (_blocks.ContainsKey(block.Id.ToString()))
            throw new ArgumentException($"Block ID {block.Id} already exists");

        _blocks[block.Id.ToString()] = block;
    }

    public Block GetBlock(string blockId)
    {
        return _blocks.TryGetValue(blockId, out var block) ? block : null;
    }

    public int GetBlockCount()
    {
        return _blocks.Count;
    }
}