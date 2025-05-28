using ITOC.Core.Command;

namespace ITOC.Test.Command;

public class ArgumentTypeTests
{
    [Fact]
    public void IntegerArgumentType_ParsesCorrectly()
    {
        // Arrange
        var type = new IntegerArgumentType();

        // Act & Assert
        Assert.True(type.TryParse("123", out var result));
        Assert.Equal(123, result);

        Assert.False(type.TryParse("abc", out _));
    }

    [Fact]
    public void IntegerArgumentType_RespectsRange()
    {
        // Arrange
        var type = new IntegerArgumentType(10, 20);

        // Act & Assert
        Assert.True(type.TryParse("10", out _));
        Assert.True(type.TryParse("15", out _));
        Assert.True(type.TryParse("20", out _));
        Assert.False(type.TryParse("9", out _));
        Assert.False(type.TryParse("21", out _));
    }

    [Fact]
    public void FloatArgumentType_ParsesCorrectly()
    {
        // Arrange
        var type = new FloatArgumentType();

        // Act & Assert
        Assert.True(type.TryParse("123.45", out var result));
        Assert.Equal(123.45f, result);

        Assert.True(type.TryParse("-123", out var result2));
        Assert.Equal(-123f, result2);

        Assert.False(type.TryParse("abc", out _));
    }

    [Fact]
    public void StringArgumentType_ParsesCorrectly()
    {
        // Arrange
        var type = new StringArgumentType();

        // Act & Assert
        Assert.True(type.TryParse("hello world", out var result));
        Assert.Equal("hello world", result);
    }
}
