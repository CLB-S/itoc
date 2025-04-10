using Godot;
using System;

public class WorldSettings
{
    public ulong Seed = 234;
    public int LoadDistance = 8;

    #region Generation Settings
    public float ContinentRatio = 0.4f;
    public float PlateMergeRatio = 0.13f;
    public float MaxTectonicMovement = 10.0f;
    public float MaxAltitude = 200.0f;

    public Rect2 Bounds = new Rect2(-5000, -5000, 10000, 10000);
    public float MinimumCellDistance = 50;
    public float AltitudePropagationDecrement = 0.8f;
    public float AltitudePropagationSharpness = 0.1f;
    #endregion

    public WorldSettings(ulong seed = 234)
    {
        Seed = seed == 0 ? GD.Randi() : seed;
    }
}
