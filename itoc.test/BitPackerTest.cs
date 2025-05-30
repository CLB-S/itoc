using ITOC.Core.Utils;

namespace ITOC.Test;

public class BitPackerTest
{
    [Fact]
    public void PackUnpack_UInt16_ShouldRetainValue()
    {
        // Arrange
        ushort original = 12345;

        // Act
        var bytes = BitPacker.PackUInt16(original);
        var unpacked = BitPacker.UnpackUInt16(bytes);

        // Assert
        Assert.Equal(original, unpacked);
    }

    [Fact]
    public void PackUnpack_UInt16_MaxValue_ShouldRetainValue()
    {
        // Arrange
        ushort original = ushort.MaxValue;

        // Act
        var bytes = BitPacker.PackUInt16(original);
        var unpacked = BitPacker.UnpackUInt16(bytes);

        // Assert
        Assert.Equal(original, unpacked);
    }

    [Fact]
    public void PackUnpack_UInt32_ShouldRetainValue()
    {
        // Arrange
        uint original = 123456789;

        // Act
        var bytes = BitPacker.PackUInt32(original);
        var unpacked = BitPacker.UnpackUInt32(bytes);

        // Assert
        Assert.Equal(original, unpacked);
    }

    [Fact]
    public void PackUnpack_UInt32_MaxValue_ShouldRetainValue()
    {
        // Arrange
        uint original = uint.MaxValue;

        // Act
        var bytes = BitPacker.PackUInt32(original);
        var unpacked = BitPacker.UnpackUInt32(bytes);

        // Assert
        Assert.Equal(original, unpacked);
    }

    [Fact]
    public void PackUnpack_UInt64_ShouldRetainValue()
    {
        // Arrange
        ulong original = 1234567890123456789;

        // Act
        var bytes = BitPacker.PackUInt64(original);
        var unpacked = BitPacker.UnpackUInt64(bytes);

        // Assert
        Assert.Equal(original, unpacked);
    }

    [Fact]
    public void PackUnpack_UInt64_MaxValue_ShouldRetainValue()
    {
        // Arrange
        ulong original = ulong.MaxValue;

        // Act
        var bytes = BitPacker.PackUInt64(original);
        var unpacked = BitPacker.UnpackUInt64(bytes);

        // Assert
        Assert.Equal(original, unpacked);
    }

    [Fact]
    public void UnpackUInt16_InvalidLength_ShouldThrowException()
    {
        // Arrange
        var bytes = new byte[] { 1, 2, 3 }; // Wrong length

        // Act & Assert
        Assert.Throws<ArgumentException>(() => BitPacker.UnpackUInt16(bytes));
    }

    [Fact]
    public void UnpackUInt32_InvalidLength_ShouldThrowException()
    {
        // Arrange
        var bytes = new byte[] { 1, 2, 3 }; // Wrong length

        // Act & Assert
        Assert.Throws<ArgumentException>(() => BitPacker.UnpackUInt32(bytes));
    }

    [Fact]
    public void UnpackUInt64_InvalidLength_ShouldThrowException()
    {
        // Arrange
        var bytes = new byte[] { 1, 2, 3 }; // Wrong length

        // Act & Assert
        Assert.Throws<ArgumentException>(() => BitPacker.UnpackUInt64(bytes));
    }

    [Fact]
    public void CalculateRequiredBytes_ShouldReturnCorrectValue()
    {
        // Test cases with different bit sizes and value counts
        Assert.Equal(1, BitPacker.CalculateRequiredBytes(8, 1)); // 8 bits = 1 byte
        Assert.Equal(1, BitPacker.CalculateRequiredBytes(4, 2)); // 8 bits = 1 byte
        Assert.Equal(2, BitPacker.CalculateRequiredBytes(9, 1)); // 9 bits = 2 bytes (round up)
        Assert.Equal(4, BitPacker.CalculateRequiredBytes(1, 32)); // 32 bits = 4 bytes
        Assert.Equal(3, BitPacker.CalculateRequiredBytes(6, 4)); // 24 bits = 3 bytes
    }

    [Fact]
    public void Pack_Unpack_UInt16Array_ShouldRetainValues()
    {
        // Arrange
        ushort[] original = { 1, 5, 10, 15, 20, 31 };
        int bitsPerValue = 5; // 5 bits can represent values 0-31

        // Act
        var packed = BitPacker.Pack(original, bitsPerValue);
        var unpacked = BitPacker.Unpack<ushort>(packed, original.Length, bitsPerValue);

        // Assert
        Assert.Equal(original.Length, unpacked.Length);
        for (int i = 0; i < original.Length; i++)
        {
            Assert.Equal(original[i], unpacked[i]);
        }
    }

    [Fact]
    public void Pack_Unpack_UInt32Array_ShouldRetainValues()
    {
        // Arrange
        uint[] original = { 100, 500, 1000, 1500, 2000, 2500 };
        int bitsPerValue = 12; // 12 bits can represent values 0-4095

        // Act
        var packed = BitPacker.Pack(original, bitsPerValue);
        var unpacked = BitPacker.Unpack<uint>(packed, original.Length, bitsPerValue);

        // Assert
        Assert.Equal(original.Length, unpacked.Length);
        for (int i = 0; i < original.Length; i++)
        {
            Assert.Equal(original[i], unpacked[i]);
        }
    }

    [Fact]
    public void Pack_Unpack_UInt64Array_ShouldRetainValues()
    {
        // Arrange
        ulong[] original = { 1000, 5000, 10000, 15000, 20000, 30000 };
        int bitsPerValue = 16; // 16 bits can represent values 0-65535

        // Act
        var packed = BitPacker.Pack(original, bitsPerValue);
        var unpacked = BitPacker.Unpack<ulong>(packed, original.Length, bitsPerValue);

        // Assert
        Assert.Equal(original.Length, unpacked.Length);
        for (int i = 0; i < original.Length; i++)
        {
            Assert.Equal(original[i], unpacked[i]);
        }
    }

    [Fact]
    public void Pack_Unpack_BitPacking_ShouldOptimizeStorage()
    {
        // Arrange - Create an array with values that fit in 3 bits
        ushort[] original = new ushort[16]; // 16 values * 3 bits = 48 bits = 6 bytes
        for (int i = 0; i < original.Length; i++)
        {
            original[i] = (ushort)(i % 8); // Values 0-7 (3 bits)
        }

        // Act
        var packed = BitPacker.Pack(original, 3);

        // Assert
        // Verify packed size is optimized (6 bytes for 16 values using 3 bits each)
        Assert.Equal(6, packed.Length);

        // Verify we can unpack correctly
        var unpacked = BitPacker.Unpack<ushort>(packed, original.Length, 3);
        for (int i = 0; i < original.Length; i++)
        {
            Assert.Equal(original[i], unpacked[i]);
        }
    }

    [Fact]
    public void Pack_EmptyArray_ShouldReturnEmptyArray()
    {
        // Arrange
        ushort[] empty = Array.Empty<ushort>();

        // Act
        var result = BitPacker.Pack(empty, 8);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Pack_ExceedsBitLimit_ShouldThrowArgumentException()
    {
        // Arrange
        ushort[] values = { 0, 16, 0 }; // 16 exceeds 4-bit limit

        // Act & Assert
        Assert.Throws<ArgumentException>(() => BitPacker.Pack(values, 4));
    }

    [Fact]
    public void Pack_InvalidBitsPerValue_ShouldThrowArgumentException()
    {
        // Arrange
        ushort[] values = { 1, 2, 3 };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => BitPacker.Pack(values, 0));
        Assert.Throws<ArgumentException>(() => BitPacker.Pack(values, 17)); // ushort max is 16 bits
    }

    [Fact]
    public void Unpack_BytesTooSmall_ShouldThrowArgumentException()
    {
        // Arrange
        byte[] tooSmall = { 1, 2 }; // Too small for 5 values at 4 bits each

        // Act & Assert
        Assert.Throws<ArgumentException>(() => BitPacker.Unpack<ushort>(tooSmall, 5, 4));
    }
}