using ITOC.Core.Utils;

namespace ITOC.Test.BitPacking;

using System;
using Xunit;

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
        var original = ushort.MaxValue;

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
        var original = uint.MaxValue;

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
        ulong original = 0x1234567890ABCDEF;

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
        var original = ulong.MaxValue;

        // Act
        var bytes = BitPacker.PackUInt64(original);
        var unpacked = BitPacker.UnpackUInt64(bytes);

        // Assert
        Assert.Equal(original, unpacked);
    }

    [Fact]
    public void PackUnpack_Span_UInt16_ShouldRetainValue()
    {
        // Arrange
        ushort original = 12345;
        Span<byte> buffer = new byte[2];

        // Act
        BitPacker.PackUInt16(original, buffer);
        var unpacked = BitPacker.UnpackUInt16(buffer);

        // Assert
        Assert.Equal(original, unpacked);
    }

    [Fact]
    public void PackUnpack_Span_UInt32_ShouldRetainValue()
    {
        // Arrange
        uint original = 0x12345678;
        Span<byte> buffer = new byte[4];

        // Act
        BitPacker.PackUInt32(original, buffer);
        var unpacked = BitPacker.UnpackUInt32(buffer);

        // Assert
        Assert.Equal(original, unpacked);
    }

    [Fact]
    public void PackUnpack_Span_UInt64_ShouldRetainValue()
    {
        // Arrange
        ulong original = 0x1234567890ABCDEF;
        Span<byte> buffer = new byte[8];

        // Act
        BitPacker.PackUInt64(original, buffer);
        var unpacked = BitPacker.UnpackUInt64(buffer);

        // Assert
        Assert.Equal(original, unpacked);
    }

    [Fact]
    public void PackUnpack_Array_UInt16_ShouldRetainValues()
    {
        // Arrange
        ushort[] original = { 1, 10, 100, 1000, 10000 };

        // Act
        var bytes = BitPacker.PackUInt16Array(original);
        var unpacked = BitPacker.UnpackUInt16Array(bytes, original.Length);

        // Assert
        Assert.Equal(original, unpacked);
    }

    [Fact]
    public void PackUnpack_Array_UInt32_ShouldRetainValues()
    {
        // Arrange
        uint[] original = { 1, 1000, 10000, 100000, 1000000 };

        // Act
        var bytes = BitPacker.PackUInt32Array(original);
        var unpacked = BitPacker.UnpackUInt32Array(bytes, original.Length);

        // Assert
        Assert.Equal(original, unpacked);
    }

    [Fact]
    public void PackUnpack_Array_UInt64_ShouldRetainValues()
    {
        // Arrange
        ulong[] original = { 1, 1000, 10000, 100000, 1000000000000 };

        // Act
        var bytes = BitPacker.PackUInt64Array(original);
        var unpacked = BitPacker.UnpackUInt64Array(bytes, original.Length);

        // Assert
        Assert.Equal(original, unpacked);
    }

    [Fact]
    public void Pack_Generic_UshortArray_ShouldUseSpecifiedBits()
    {
        // Arrange
        ushort[] original = { 1, 10, 100, 1000, 10000 };
        var bitsPerValue = 14; // Enough to store values up to 16383

        // Act
        var packed = BitPacker.Pack<ushort>(original, bitsPerValue);
        var unpacked = BitPacker.Unpack<ushort>(packed, original.Length, bitsPerValue);

        // Assert
        Assert.Equal(original, unpacked);

        // Verify compressed size is smaller than full size
        Assert.True(packed.Length < original.Length * sizeof(ushort));
        Assert.Equal(
            BitPacker.CalculateRequiredBytes(original.Length, bitsPerValue),
            packed.Length
        );
    }

    [Fact]
    public void Pack_Generic_UIntArray_ShouldUseSpecifiedBits()
    {
        // Arrange
        uint[] original = { 1, 100, 10000, 1000000, 100000000 };
        var bitsPerValue = 27; // Enough to store values up to 134,217,727

        // Act
        var packed = BitPacker.Pack<uint>(original, bitsPerValue);
        var unpacked = BitPacker.Unpack<uint>(packed, original.Length, bitsPerValue);

        // Assert
        Assert.Equal(original, unpacked);

        // Verify compressed size is smaller than full size
        Assert.True(packed.Length < original.Length * sizeof(uint));
        Assert.Equal(
            BitPacker.CalculateRequiredBytes(original.Length, bitsPerValue),
            packed.Length
        );
    }

    [Fact]
    public void Pack_Generic_ULongArray_ShouldUseSpecifiedBits()
    {
        // Arrange
        ulong[] original = { 1, 100000, 10000000000, 1000000000000, 100000000000000 };
        var bitsPerValue = 47; // Enough to store values up to 140,737,488,355,327

        // Act
        var packed = BitPacker.Pack<ulong>(original, bitsPerValue);
        var unpacked = BitPacker.Unpack<ulong>(packed, original.Length, bitsPerValue);

        // Assert
        Assert.Equal(original, unpacked);

        // Verify compressed size is smaller than full size
        Assert.True(packed.Length < original.Length * sizeof(ulong));
        Assert.Equal(
            BitPacker.CalculateRequiredBytes(original.Length, bitsPerValue),
            packed.Length
        );
    }

    [Fact]
    public void CalculateRequiredBytes_ShouldReturnCorrectByteCount()
    {
        // Test various combinations of count and bits per value
        Assert.Equal(1, BitPacker.CalculateRequiredBytes(1, 1)); // 1 bit needs 1 byte
        Assert.Equal(1, BitPacker.CalculateRequiredBytes(8, 1)); // 8 bits needs 1 byte
        Assert.Equal(2, BitPacker.CalculateRequiredBytes(9, 1)); // 9 bits needs 2 bytes
        Assert.Equal(2, BitPacker.CalculateRequiredBytes(1, 16)); // 16 bits needs 2 bytes
        Assert.Equal(4, BitPacker.CalculateRequiredBytes(1, 32)); // 32 bits needs 4 bytes
        Assert.Equal(8, BitPacker.CalculateRequiredBytes(1, 64)); // 64 bits needs 8 bytes

        // More complex cases
        Assert.Equal(13, BitPacker.CalculateRequiredBytes(100, 1)); // 100 bits needs 13 bytes
        Assert.Equal(25, BitPacker.CalculateRequiredBytes(100, 2)); // 200 bits needs 25 bytes
        Assert.Equal(125, BitPacker.CalculateRequiredBytes(100, 10)); // 1000 bits needs 125 bytes
    }

    [Fact]
    public void GetMaxValueForBits_ShouldReturnCorrectMaxValue()
    {
        Assert.Equal(1UL, BitPacker.GetMaxValueForBits(1)); // 2^1 - 1 = 1
        Assert.Equal(3UL, BitPacker.GetMaxValueForBits(2)); // 2^2 - 1 = 3
        Assert.Equal(7UL, BitPacker.GetMaxValueForBits(3)); // 2^3 - 1 = 7
        Assert.Equal(15UL, BitPacker.GetMaxValueForBits(4)); // 2^4 - 1 = 15
        Assert.Equal(255UL, BitPacker.GetMaxValueForBits(8)); // 2^8 - 1 = 255
        Assert.Equal(65535UL, BitPacker.GetMaxValueForBits(16)); // 2^16 - 1 = 65535
        Assert.Equal(4294967295UL, BitPacker.GetMaxValueForBits(32)); // 2^32 - 1 = 4294967295
        Assert.Equal(ulong.MaxValue, BitPacker.GetMaxValueForBits(64)); // 2^64 - 1
    }

    [Fact]
    public void Pack_ValueExceedsBitRange_ThrowsException()
    {
        // Arrange
        uint[] values = { 0, 1, 2, 16, 4 }; // 16 is out of range for 4 bits
        var bitsPerValue = 4; // Can store values 0-15

        // Act & Assert
        Assert.Throws<ArgumentException>(() => BitPacker.Pack<uint>(values, bitsPerValue));
    }
}
