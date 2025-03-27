using Godot;
using System;
using System.Collections.Generic;


public class NoiseLayerConfig
{
    public List<FastNoiseLite> Layers { get; } = new List<FastNoiseLite>();

    public NoiseLayerConfig()
    {
        // 默认配置
        // TODO
    }
}
