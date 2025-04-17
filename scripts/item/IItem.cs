using Godot;
using System;

public interface IItem
{
    ItemType Type { get; }
    string Id { get; }
    string Name { get; }
    string Description { get; }

    //    Texture2D Icon { get; }
}