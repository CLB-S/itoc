using ITOC.Libs.Palette;

namespace ITOC.Test;

public class PaletteTest : IDisposable
{
    private Palette<string> _palette;

    public PaletteTest()
    {
        _palette = new Palette<string>("default", 4);
    }

    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act using constructor
        var palette = new Palette<int>(0, 4);

        // Assert
        Assert.Equal(0, palette.DefaultValue);
        Assert.Equal(4, palette.BitsPerEntry);
        Assert.Equal((1UL << 4) - 1, palette.Mask); // Should be 15 (binary: 1111)
        Assert.True(palette.IsSingleEntry);
    }

    [Fact]
    public void GetId_ReturnsZeroForDefaultValue()
    {
        // Act
        var id = _palette.GetId("default");

        // Assert
        Assert.Equal(0, id);
    }

    [Fact]
    public void GetId_ReturnsConsistentIdsForSameValues()
    {
        // Act
        var id1 = _palette.GetId("stone");
        var id2 = _palette.GetId("dirt");
        var id3 = _palette.GetId("stone"); // Should return same ID as id1

        // Assert
        Assert.Equal(1, id1);
        Assert.Equal(2, id2);
        Assert.Equal(id1, id3); // Getting the same value should return the same ID
    }

    [Fact]
    public void GetId_SetsSingleEntryModeToFalseWhenAddingNewEntry()
    {
        // Arrange
        Assert.True(_palette.IsSingleEntry); // Starts as true

        // Act
        _palette.GetId("newValue"); // Adding a non-default value

        // Assert
        Assert.False(_palette.IsSingleEntry); // Should be false now
    }

    [Fact]
    public void GetId_IncreasesBitsWhenNeeded()
    {
        // Arrange
        var bitsTracker = new BitsTracker(_palette);

        // Fill the palette to capacity for 4 bits (16 entries)
        for (int i = 0; i < 15; i++)
        {
            _palette.GetId($"value{i}");
        }

        Assert.Equal(4, _palette.BitsPerEntry); // Should still be 4 bits

        // Act - add the 17th entry (including default value) which should trigger a bit increase
        _palette.GetId("triggerBitIncrease");

        // Assert
        Assert.Equal(5, _palette.BitsPerEntry); // Should now be 5 bits
        Assert.Equal(1, bitsTracker.BitsIncreaseCount);
    }

    [Fact]
    public void GetValue_ReturnsCorrectValue()
    {
        // Arrange
        var id1 = _palette.GetId("stone");
        var id2 = _palette.GetId("dirt");

        // Act & Assert
        Assert.Equal("default", _palette.GetValue(0));
        Assert.Equal("stone", _palette.GetValue(1));
        Assert.Equal("dirt", _palette.GetValue(2));
    }

    [Fact]
    public void GetValue_ReturnsDefaultValueForInvalidIds()
    {
        // Act & Assert
        Assert.Equal("default", _palette.GetValue(-1)); // Negative ID
        Assert.Equal("default", _palette.GetValue(999)); // Non-existent ID
    }

    [Fact]
    public void ForceNormalMode_ChangesStateCorrectly()
    {
        // Arrange
        var palette = new Palette<string>("default", 4);
        Assert.True(palette.IsSingleEntry);

        var singleEntryModeChanges = new SingleEntryModeTracker(palette);

        // Act
        palette.ForceNormalMode();

        // Assert
        Assert.False(palette.IsSingleEntry);
        Assert.Equal(1, singleEntryModeChanges.StateChangeCount);

        // Act again - should not trigger events since already in normal mode
        palette.ForceNormalMode();

        // Assert
        Assert.False(palette.IsSingleEntry);
        Assert.Equal(1, singleEntryModeChanges.StateChangeCount); // Count should not increase
    }

    [Fact]
    public void Dispose_FreesResources()
    {
        // Arrange
        var palette = new Palette<string>("default", 4);

        // Act
        palette.Dispose();

        // Assert - this is mostly testing that no exception is thrown
        // We could use reflection to check if _lock is disposed, but that's testing implementation details
    }

    public void Dispose()
    {
        _palette.Dispose();
    }

    // Helper classes for event tracking
    private class BitsTracker
    {
        public int BitsIncreaseCount { get; private set; }
        public int OldBits { get; private set; }
        public int NewBits { get; private set; }

        public BitsTracker(Palette<string> palette)
        {
            palette.OnBitsIncreased += (oldBits, newBits) =>
            {
                BitsIncreaseCount++;
                OldBits = oldBits;
                NewBits = newBits;
            };
        }
    }

    private class SingleEntryModeTracker
    {
        public int StateChangeCount { get; private set; }
        public bool LastState { get; private set; }

        public SingleEntryModeTracker(Palette<string> palette)
        {
            palette.OnSingleEntryStateChanged += (newState) =>
            {
                StateChangeCount++;
                LastState = newState;
            };
        }
    }
}
