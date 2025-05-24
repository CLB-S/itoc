namespace ITOC;

public class BlockProperties
{
    public bool IsOpaque { get; set; } = true;


    public static BlockProperties Default => new();
    public static BlockProperties Transparent => new() { IsOpaque = false };

}