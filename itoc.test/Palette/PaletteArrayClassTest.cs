namespace ITOC.Test.Palette;

using System;
using System.Linq;
using ITOC.Core.Utils;
using Xunit;

public class PaletteArrayClassTest
{
    // Custom class for testing that implements IEquatable<T>
    private class TestClass : IEquatable<TestClass>
    {
        public int Id { get; }
        public string Name { get; }

        public TestClass(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public bool Equals(TestClass? other)
        {
            if (other is null) return false;
            return Id == other.Id && Name == other.Name;
        }

        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is TestClass other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Name);
        }

        public static bool operator ==(TestClass left, TestClass right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(TestClass left, TestClass right)
        {
            return !(left == right);
        }
    }

    [Fact]
    public void Constructor_WithClassValues_CreatesPalette()
    {
        // Arrange
        var defaultValue = new TestClass(0, "default");
        var a = new TestClass(1, "a");
        var b = new TestClass(2, "b");
        var c = new TestClass(3, "c");
        var values = new[] { a, b, a, c, b };

        // Act
        var array = new PaletteArray<TestClass>(values, defaultValue);

        // Assert
        Assert.Equal(5, array.Count);
        Assert.Equal(4, array.PaletteSize); // default + a, b, c
        Assert.Equal(a, array[0]);
        Assert.Equal(b, array[1]);
        Assert.Equal(a, array[2]);
        Assert.Equal(c, array[3]);
        Assert.Equal(b, array[4]);
    }

    [Fact]
    public void Set_AndGet_ClassValues()
    {
        // Arrange
        var defaultValue = new TestClass(0, "default");
        var array = new PaletteArray<TestClass>(5, defaultValue);
        var testValue1 = new TestClass(1, "test1");
        var testValue2 = new TestClass(2, "test2");

        // Act
        array[2] = testValue1;
        array[4] = testValue2;

        // Assert
        Assert.Equal(defaultValue, array[0]);
        Assert.Equal(defaultValue, array[1]);
        Assert.Equal(testValue1, array[2]);
        Assert.Equal(defaultValue, array[3]);
        Assert.Equal(testValue2, array[4]);
        Assert.Equal(3, array.PaletteSize); // default, test1, test2
    }

    [Fact]
    public void Set_SameClassMultipleTimes_UsesPaletteCorrectly()
    {
        // Arrange
        var defaultValue = new TestClass(0, "default");
        var array = new PaletteArray<TestClass>(10, defaultValue);
        var testValue = new TestClass(1, "test");

        // Act - set the same value in multiple positions
        array[1] = testValue;
        array[3] = testValue;
        array[5] = testValue;
        array[7] = testValue;

        // Create an equal but not reference-equal instance
        var equalValue = new TestClass(1, "test");
        array[9] = equalValue;

        // Assert
        Assert.Equal(testValue, array[1]);
        Assert.Equal(testValue, array[3]);
        Assert.Equal(testValue, array[5]);
        Assert.Equal(testValue, array[7]);
        Assert.Equal(equalValue, array[9]);

        // Even though we added the same value 5 times, palette should only have 2 entries
        Assert.Equal(2, array.PaletteSize); // default + test
    }

    [Fact]
    public void GetIndex_WithClasses_ReturnsCorrectIndices()
    {
        // Arrange
        var defaultValue = new TestClass(0, "default");
        var array = new PaletteArray<TestClass>(5, defaultValue);
        var testValue1 = new TestClass(1, "test1");
        var testValue2 = new TestClass(2, "test2");
        var testValue3 = new TestClass(3, "test3");

        array[0] = testValue1;
        array[1] = testValue2;
        array[2] = testValue3;

        // Act & Assert
        Assert.Equal(0u, array.GetIndex(defaultValue));
        Assert.Equal(1u, array.GetIndex(testValue1));
        Assert.Equal(2u, array.GetIndex(testValue2));
        Assert.Equal(3u, array.GetIndex(testValue3));

        // Test with equal but not reference-equal instances
        var equalToTestValue1 = new TestClass(1, "test1");
        Assert.Equal(1u, array.GetIndex(equalToTestValue1));

        // Not in palette
        var notInPalette = new TestClass(4, "notInPalette");
        Assert.Equal(0u, array.GetIndex(notInPalette)); // Should return default value index
    }

    [Fact]
    public void Fill_WithClass_UpdatesAllValues()
    {
        // Arrange
        var defaultValue = new TestClass(0, "default");
        var array = new PaletteArray<TestClass>(10, defaultValue);
        var testValue = new TestClass(1, "test");
        array[3] = testValue; // Change one value

        var fillValue = new TestClass(2, "fill");

        // Act
        array.Fill(fillValue);

        // Assert
        foreach (var value in array)
        {
            Assert.Equal(fillValue, value);
        }
    }

    [Fact]
    public void GetUniqueValues_WithClasses_ReturnsAllPaletteEntries()
    {
        // Arrange
        var defaultValue = new TestClass(0, "default");
        var array = new PaletteArray<TestClass>(5, defaultValue);
        var testValue1 = new TestClass(1, "test1");
        var testValue2 = new TestClass(2, "test2");
        var testValue3 = new TestClass(3, "test3");

        array[0] = testValue1;
        array[1] = testValue2;
        array[2] = testValue1; // Duplicate value
        array[3] = testValue3;

        // Act
        var uniqueValues = array.GetUniqueValues().ToList();

        // Assert
        Assert.Equal(4, uniqueValues.Count);
        Assert.Contains(defaultValue, uniqueValues);
        Assert.Contains(testValue1, uniqueValues);
        Assert.Contains(testValue2, uniqueValues);
        Assert.Contains(testValue3, uniqueValues);
    }

    [Fact]
    public void Dispose_WithClasses_PreventsFurtherUsage()
    {
        // Arrange
        var defaultValue = new TestClass(0, "default");
        var array = new PaletteArray<TestClass>(5, defaultValue);
        var testValue = new TestClass(1, "test");
        array[0] = testValue;

        // Act
        array.Dispose();

        // Assert
        Assert.Throws<ObjectDisposedException>(() => array[0] = testValue);
        Assert.Throws<ObjectDisposedException>(() => _ = array[0]);
        Assert.Throws<ObjectDisposedException>(() => array.GetUniqueValues());
    }
}
