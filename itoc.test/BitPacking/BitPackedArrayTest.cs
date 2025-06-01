using ITOC.Core.Utils;

namespace ITOC.Test.BitPacking;

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

public class BitPackedArrayTest
{
    [Fact]
    public void TestConstructWithSize()
    {
        // Create an array with 100 elements, 5 bits per value
        var array = new BitPackedArray<uint>(100, 5);

        Assert.Equal(100, array.Count);
        Assert.Equal(5, array.BitsPerValue);
        Assert.Equal(31ul, array.MaxValue); // 2^5 - 1 = 31
        Assert.Equal(BitPacker.CalculateRequiredBytes(100, 5), array.SizeInBytes);
        Assert.Equal(500, array.SizeInBits);

        // Verify all values are initialized to 0
        for (int i = 0; i < array.Count; i++)
        {
            Assert.Equal(0u, array[i]);
        }
    }

    [Fact]
    public void TestConstructWithValues()
    {
        uint[] values = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        int bitsPerValue = 4; // Can represent values up to 15

        var array = new BitPackedArray<uint>(values, bitsPerValue);

        Assert.Equal(values.Length, array.Count);
        Assert.Equal(bitsPerValue, array.BitsPerValue);

        // Verify all values match the original array
        for (int i = 0; i < array.Count; i++)
        {
            Assert.Equal(values[i], array[i]);
        }
    }

    [Fact]
    public void TestIndexerGetSet()
    {
        var array = new BitPackedArray<uint>(10, 5); // 5 bits per value (0-31)

        // Set values
        array[0] = 1;
        array[1] = 5;
        array[2] = 10;
        array[3] = 15;
        array[4] = 20;
        array[5] = 25;
        array[6] = 30;
        array[7] = 31;
        array[8] = 0;
        array[9] = 16;

        // Get values and verify
        Assert.Equal(1u, array[0]);
        Assert.Equal(5u, array[1]);
        Assert.Equal(10u, array[2]);
        Assert.Equal(15u, array[3]);
        Assert.Equal(20u, array[4]);
        Assert.Equal(25u, array[5]);
        Assert.Equal(30u, array[6]);
        Assert.Equal(31u, array[7]);
        Assert.Equal(0u, array[8]);
        Assert.Equal(16u, array[9]);
    }

    [Fact]
    public void TestValueOutOfRange()
    {
        var array = new BitPackedArray<uint>(10, 5); // 5 bits per value (0-31)

        // Setting a value that fits within 5 bits should work
        array[0] = 31; // Maximum value for 5 bits

        // Setting a value that requires more than 5 bits should throw
        Assert.Throws<ArgumentOutOfRangeException>(() => array[0] = 32);
    }

    [Fact]
    public void TestIndexOutOfRange()
    {
        var array = new BitPackedArray<uint>(10, 5);

        // Accessing with valid indices should work
        array[0] = 5;
        array[9] = 10;

        // Accessing with invalid indices should throw
        Assert.Throws<IndexOutOfRangeException>(() => array[-1] = 0);
        Assert.Throws<IndexOutOfRangeException>(() => array[10] = 0);
        Assert.Throws<IndexOutOfRangeException>(() => _ = array[-1]);
        Assert.Throws<IndexOutOfRangeException>(() => _ = array[10]);
    }

    [Fact]
    public void TestFill()
    {
        var array = new BitPackedArray<uint>(100, 5);

        // Fill with a value
        array.Fill(15);

        // Verify all elements are set to the value
        for (int i = 0; i < array.Count; i++)
        {
            Assert.Equal(15u, array[i]);
        }

        // Fill with another value
        array.Fill(7);

        // Verify all elements are set to the new value
        for (int i = 0; i < array.Count; i++)
        {
            Assert.Equal(7u, array[i]);
        }

        // Filling with a value out of range should throw
        Assert.Throws<ArgumentOutOfRangeException>(() => array.Fill(32));
    }

    [Fact]
    public void TestCopyTo()
    {
        uint[] values = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        int bitsPerValue = 4;

        var array = new BitPackedArray<uint>(values, bitsPerValue);

        // Copy to a new array
        uint[] destination = new uint[10];
        array.CopyTo(destination);

        // Verify the copy
        Assert.Equal(values, destination);

        // Copy to an array with an offset
        uint[] destination2 = new uint[15];
        array.CopyTo(destination2, 5);

        // Verify the offset copy
        for (int i = 0; i < values.Length; i++)
        {
            Assert.Equal(values[i], destination2[i + 5]);
        }
    }

    [Fact]
    public void TestGetPackedData()
    {
        uint[] values = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        int bitsPerValue = 4;

        var array = new BitPackedArray<uint>(values, bitsPerValue);

        // Get the packed data
        byte[] packedData = array.GetPackedData();

        // Create a new array from the packed data
        var array2 = new BitPackedArray<uint>(packedData, values.Length, bitsPerValue);

        // Verify the new array has the same values
        for (int i = 0; i < values.Length; i++)
        {
            Assert.Equal(values[i], array2[i]);
        }
    }

    [Fact]
    public void TestToArray()
    {
        uint[] values = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        int bitsPerValue = 4;

        var array = new BitPackedArray<uint>(values, bitsPerValue);

        // Convert to array
        uint[] copy = array.ToArray();

        // Verify the array copy
        Assert.Equal(values, copy);
    }

    [Fact]
    public void TestCreateWithMinimumBits()
    {
        uint[] values = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 15 };

        // Create with minimum bits (4 bits needed for values up to 15)
        var array = BitPackedArray<uint>.CreateWithMinimumBits(values);

        Assert.Equal(4, array.BitsPerValue); // Should choose 4 bits
        Assert.Equal(15ul, array.MaxValue); // Max representable: 2^4 - 1 = 15

        // Verify values
        for (int i = 0; i < values.Length; i++)
        {
            Assert.Equal(values[i], array[i]);
        }
    }

    [Fact]
    public void TestResize()
    {
        uint[] values = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        int bitsPerValue = 4;

        var array = new BitPackedArray<uint>(values, bitsPerValue);

        // Resize to smaller
        var smaller = array.Resize(5);

        Assert.Equal(5, smaller.Count);
        Assert.Equal(bitsPerValue, smaller.BitsPerValue);

        // Verify first 5 values are preserved
        for (int i = 0; i < 5; i++)
        {
            Assert.Equal(values[i], smaller[i]);
        }

        // Resize to larger
        var larger = array.Resize(15);

        Assert.Equal(15, larger.Count);
        Assert.Equal(bitsPerValue, larger.BitsPerValue);

        // Verify original values are preserved
        for (int i = 0; i < values.Length; i++)
        {
            Assert.Equal(values[i], larger[i]);
        }

        // Verify new elements are initialized to 0
        for (int i = values.Length; i < 15; i++)
        {
            Assert.Equal(0u, larger[i]);
        }
    }

    [Fact]
    public void TestEnumeration()
    {
        uint[] values = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        int bitsPerValue = 4;

        var array = new BitPackedArray<uint>(values, bitsPerValue);

        // Enumerate and collect to list
        List<uint> enumerated = array.ToList();

        // Verify enumeration
        Assert.Equal(values, enumerated);
    }

    [Fact]
    public void TestMemoryEfficiency()
    {
        // Create arrays with different bit sizes to verify memory efficiency
        const int count = 1000;

        // Byte array at 8 bits per value
        byte[] regularArray = new byte[count];
        int regularSize = regularArray.Length;

        // BitPackedArray at 4 bits per value
        var bitArray4 = new BitPackedArray<uint>(count, 4);
        int packedSize4 = bitArray4.SizeInBytes;

        // BitPackedArray at 2 bits per value
        var bitArray2 = new BitPackedArray<uint>(count, 2);
        int packedSize2 = bitArray2.SizeInBytes;

        // BitPackedArray at 1 bit per value
        var bitArray1 = new BitPackedArray<uint>(count, 1);
        int packedSize1 = bitArray1.SizeInBytes;

        // Verify sizes
        Assert.Equal(count, regularSize); // Regular array: 1 byte per value
        Assert.Equal(count / 2, packedSize4); // 4-bit array: Half the size (4/8 = 0.5)
        Assert.Equal(count / 4, packedSize2); // 2-bit array: Quarter the size (2/8 = 0.25)
        Assert.Equal(count / 8, packedSize1); // 1-bit array: Eighth the size (1/8 = 0.125)
    }

    [Fact]
    public void TestSpecialBitSizes()
    {
        // Test powers of 2 that align with byte boundaries for Fill optimization
        uint[] sizes = { 8, 16, 32, 64 };

        foreach (uint size in sizes)
        {
            var array = new BitPackedArray<ulong>(100, (int)size);
            array.Fill(42); // This should use the optimized Fill method

            for (int i = 0; i < array.Count; i++)
            {
                Assert.Equal(42u, array[i]);
            }
        }
    }

    [Fact]
    public void TestDispose()
    {
        var array = new BitPackedArray<uint>(10, 4);

        // Use the array
        array[0] = 5;
        Assert.Equal(5u, array[0]);

        // Dispose the array
        array.Dispose();

        // Using the array after disposal should throw
        Assert.Throws<ObjectDisposedException>(() => array[0] = 1);
        Assert.Throws<ObjectDisposedException>(() => _ = array[0]);
        Assert.Throws<ObjectDisposedException>(() => array.Fill(0));
        Assert.Throws<ObjectDisposedException>(() => array.GetPackedData());
        Assert.Throws<ObjectDisposedException>(() => array.ToArray());
    }
}
