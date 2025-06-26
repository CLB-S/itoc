namespace ITOC.Test.Palette;

using System;
using System.Collections.Generic;
using System.Linq;
using ITOC.Core.Utils;
using Xunit;

public class PaletteArrayTest
{
    [Fact]
    public void Constructor_WithSize_CreatesEmptyArray()
    {
        // Arrange & Act
        var array = new PaletteArray<string>(10, "default");

        // Assert
        Assert.Equal(10, array.Count);
        Assert.Equal(1, array.PaletteSize);
        Assert.Equal(4, array.BitsPerValue); // Default is 4 bits

        // All values should be default
        for (var i = 0; i < array.Count; i++)
        {
            Assert.Equal("default", array[i]);
        }
    }

    [Fact]
    public void Constructor_WithValues_CreatesPalette()
    {
        // Arrange
        var values = new[] { "a", "b", "a", "c", "b" };

        // Act
        var array = new PaletteArray<string>(values, "default");

        // Assert
        Assert.Equal(5, array.Count);
        Assert.Equal(4, array.PaletteSize); // default + a, b, c
        Assert.Equal("a", array[0]);
        Assert.Equal("b", array[1]);
        Assert.Equal("a", array[2]);
        Assert.Equal("c", array[3]);
        Assert.Equal("b", array[4]);
    }

    [Fact]
    public void Indexer_GetWithInvalidIndex_ReturnsDefaultValue()
    {
        // Arrange
        var array = new PaletteArray<int>(5, 0);

        // Act & Assert
        Assert.Equal(0, array[-1]); // Out of range - low
        Assert.Equal(0, array[10]); // Out of range - high
    }

    [Fact]
    public void Indexer_SetAndGet_StoresValue()
    {
        // Arrange
        var array = new PaletteArray<string>(5, "default");

        // Act
        array[2] = "test";
        array[4] = "another";

        // Assert
        Assert.Equal("default", array[0]);
        Assert.Equal("default", array[1]);
        Assert.Equal("test", array[2]);
        Assert.Equal("default", array[3]);
        Assert.Equal("another", array[4]);
        Assert.Equal(3, array.PaletteSize); // default, test, another
    }

    [Fact]
    public void Indexer_SetBeyondCapacity_GrowsArray()
    {
        // Arrange
        var array = new PaletteArray<int>(5, 0);

        // Act
        array[10] = 42;

        // Assert
        Assert.True(array.Count >= 11);
        Assert.Equal(42, array[10]);
    }

    [Fact]
    public void Indexer_AddingManyValues_IncreasesBitSize()
    {
        // Arrange
        var array = new PaletteArray<int>(100, 0, 2); // Start with 2-bit storage

        // Act
        for (var i = 0; i < 5; i++) // Add values 0-4
            array[i] = i;

        // Assert
        Assert.Equal(3, array.BitsPerValue); // Should be increased to 3 bits to hold 5 values (0-4)

        // Add more values
        for (var i = 0; i < 10; i++)
            array[10 + i] = 5 + i; // Values 5-14

        // Assert
        Assert.Equal(4, array.BitsPerValue); // Should be increased to 4 bits to hold 15 values (0-14)

        // Check values
        for (var i = 0; i < 5; i++)
            Assert.Equal(i, array[i]);

        for (var i = 0; i < 10; i++)
            Assert.Equal(5 + i, array[10 + i]);
    }

    [Fact]
    public void Fill_SetsAllValuesToSameValue()
    {
        // Arrange
        var array = new PaletteArray<string>(10, "default");
        array[3] = "test"; // Change a value

        // Act
        array.Fill("filled");

        // Assert
        foreach (var value in array)
        {
            Assert.Equal("filled", value);
        }
    }

    [Fact]
    public void GetIndex_ReturnsCorrectIndex()
    {
        // Arrange
        var array = new PaletteArray<string>(5, "default");
        array[0] = "a";
        array[1] = "b";
        array[2] = "c";

        // Act & Assert
        Assert.Equal(0u, array.GetIndex("default"));
        Assert.Equal(1u, array.GetIndex("a"));
        Assert.Equal(2u, array.GetIndex("b"));
        Assert.Equal(3u, array.GetIndex("c"));
        Assert.Equal(0u, array.GetIndex("not-in-palette")); // Should return default value index
    }

    [Fact]
    public void GetValue_ReturnsCorrectValue()
    {
        // Arrange
        var array = new PaletteArray<string>(5, "default");
        array[0] = "a";
        array[1] = "b";
        array[2] = "c";

        // Act & Assert
        Assert.Equal("default", array.GetValue(0));
        Assert.Equal("a", array.GetValue(1));
        Assert.Equal("b", array.GetValue(2));
        Assert.Equal("c", array.GetValue(3));
        Assert.Equal("default", array.GetValue(999)); // Out of range
    }

    [Fact]
    public void GetUniqueValues_ReturnsAllPaletteEntries()
    {
        // Arrange
        var array = new PaletteArray<string>(5, "default");
        array[0] = "a";
        array[1] = "b";
        array[2] = "a"; // Duplicate value
        array[3] = "c";

        // Act
        var uniqueValues = array.GetUniqueValues().ToList();

        // Assert
        Assert.Equal(4, uniqueValues.Count);
        Assert.Contains("default", uniqueValues);
        Assert.Contains("a", uniqueValues);
        Assert.Contains("b", uniqueValues);
        Assert.Contains("c", uniqueValues);
    }

    [Fact]
    public void CopyTo_CopiesToDestinationArray()
    {
        // Arrange
        var array = new PaletteArray<int>(5, 0);
        array[0] = 1;
        array[1] = 2;
        array[2] = 3;
        array[3] = 4;
        array[4] = 5;

        var destination = new int[10];

        // Act
        array.CopyTo(destination, 2);

        // Assert
        Assert.Equal(0, destination[0]);
        Assert.Equal(0, destination[1]);
        Assert.Equal(1, destination[2]);
        Assert.Equal(2, destination[3]);
        Assert.Equal(3, destination[4]);
        Assert.Equal(4, destination[5]);
        Assert.Equal(5, destination[6]);
        Assert.Equal(0, destination[7]);
    }

    [Fact]
    public void ToArray_ReturnsCorrectArray()
    {
        // Arrange
        var array = new PaletteArray<int>(5, 0);
        array[0] = 1;
        array[1] = 2;
        array[2] = 3;
        array[3] = 4;
        array[4] = 5;

        // Act
        var result = array.ToArray();

        // Assert
        Assert.Equal(5, result.Length);
        Assert.Equal(1, result[0]);
        Assert.Equal(2, result[1]);
        Assert.Equal(3, result[2]);
        Assert.Equal(4, result[3]);
        Assert.Equal(5, result[4]);
    }

    [Fact]
    public void Enumeration_IteratesThroughAllElements()
    {
        // Arrange
        var array = new PaletteArray<int>(5, 0);
        array[0] = 1;
        array[1] = 2;
        array[2] = 3;
        array[3] = 4;
        array[4] = 5;

        // Act
        var list = new List<int>();
        foreach (var value in array)
        {
            list.Add(value);
        }

        // Assert
        Assert.Equal(5, list.Count);
        Assert.Equal(1, list[0]);
        Assert.Equal(2, list[1]);
        Assert.Equal(3, list[2]);
        Assert.Equal(4, list[3]);
        Assert.Equal(5, list[4]);
    }

    [Fact]
    public void GetMemoryUsage_ReturnsNonZeroValue()
    {
        // Arrange
        var array = new PaletteArray<string>(1000, "default");
        for (var i = 0; i < 100; i++)
        {
            array[i] = $"value-{i % 10}"; // Only 10 unique values
        }

        // Act
        var memoryUsage = array.GetMemoryUsage();

        // Assert
        Assert.True(memoryUsage > 0);
    }

    [Fact]
    public void Dispose_PreventsFurtherUsage()
    {
        // Arrange
        var array = new PaletteArray<int>(5, 0);
        array[0] = 1;

        // Act
        array.Dispose();

        // Assert
        Assert.Throws<ObjectDisposedException>(() => array[0] = 42);
        Assert.Throws<ObjectDisposedException>(() => _ = array[0]);
        Assert.Throws<ObjectDisposedException>(() => array.GetUniqueValues());
    }
}
