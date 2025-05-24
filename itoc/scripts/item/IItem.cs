using ITOC;

public interface IItem
{
    ItemType Type { get; }
    Identifier Id { get; }
    string Name { get; }
    string Description { get; }

    //    Texture2D Icon { get; }
}