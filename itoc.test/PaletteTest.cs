using ITOC.Libs.Palette;

namespace ITOC.Test;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        var palette = new Palette<string>("air");
        var paletteStorage = new PaletteStorage<string>(palette);
        paletteStorage.Set(0, "stone");
        paletteStorage.Set(1, "dirt");

        Assert.Equal("stone", paletteStorage.Get(0));
        Assert.Equal("dirt", paletteStorage.Get(1));
    }
}
