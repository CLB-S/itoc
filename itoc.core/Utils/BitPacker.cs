namespace ITOC.Core.Utils;

using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

/// <summary>
/// Utility class for packing and unpacking values into bit-packed byte arrays.
/// </summary>
public static class BitPacker
{
    #region Single Integer

    /// <summary>
    /// Packs a 16-bit unsigned integer into a byte array.
    /// </summary>
    public static byte[] PackUInt16(ushort value)
    {
        var bytes = new byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(bytes, value);
        return bytes;
    }

    /// <summary>
    /// Packs a 16-bit unsigned integer into a byte span.
    /// </summary>
    /// <param name="value">The value to pack.</param>
    /// <param name="destination">The destination span, must be at least 2 bytes long.</param>
    /// <returns>Number of bytes written (2).</returns>
    public static int PackUInt16(ushort value, Span<byte> destination)
    {
        if (destination.Length < 2)
            throw new ArgumentException("Destination span must be at least 2 bytes long.");

        BinaryPrimitives.WriteUInt16LittleEndian(destination, value);
        return 2;
    }

    /// <summary>
    /// Unpacks a byte array into a 16-bit unsigned integer.
    /// </summary>
    public static ushort UnpackUInt16(byte[] bytes)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));
        if (bytes.Length < 2)
            throw new ArgumentException("Byte array must be at least 2 bytes long.");

        return BinaryPrimitives.ReadUInt16LittleEndian(bytes);
    }

    /// <summary>
    /// Unpacks a byte span into a 16-bit unsigned integer.
    /// </summary>
    public static ushort UnpackUInt16(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 2)
            throw new ArgumentException("Byte span must be at least 2 bytes long.");

        return BinaryPrimitives.ReadUInt16LittleEndian(bytes);
    }

    /// <summary>
    /// Packs a 32-bit integer into a byte array.
    /// </summary>
    public static byte[] PackUInt32(uint value)
    {
        var bytes = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
        return bytes;
    }

    /// <summary>
    /// Packs a 32-bit unsigned integer into a byte span.
    /// </summary>
    /// <param name="value">The value to pack.</param>
    /// <param name="destination">The destination span, must be at least 4 bytes long.</param>
    /// <returns>Number of bytes written (4).</returns>
    public static int PackUInt32(uint value, Span<byte> destination)
    {
        if (destination.Length < 4)
            throw new ArgumentException("Destination span must be at least 4 bytes long.");

        BinaryPrimitives.WriteUInt32LittleEndian(destination, value);
        return 4;
    }

    /// <summary>
    /// Unpacks a byte array into a 32-bit integer.
    /// </summary>
    public static uint UnpackUInt32(byte[] bytes)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));
        if (bytes.Length < 4)
            throw new ArgumentException("Byte array must be at least 4 bytes long.");

        return BinaryPrimitives.ReadUInt32LittleEndian(bytes);
    }

    /// <summary>
    /// Unpacks a byte span into a 32-bit unsigned integer.
    /// </summary>
    public static uint UnpackUInt32(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 4)
            throw new ArgumentException("Byte span must be at least 4 bytes long.");

        return BinaryPrimitives.ReadUInt32LittleEndian(bytes);
    }

    /// <summary>
    /// Packs a 64-bit integer into a byte array.
    /// </summary>
    public static byte[] PackUInt64(ulong value)
    {
        var bytes = new byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(bytes, value);
        return bytes;
    }

    /// <summary>
    /// Packs a 64-bit unsigned integer into a byte span.
    /// </summary>
    /// <param name="value">The value to pack.</param>
    /// <param name="destination">The destination span, must be at least 8 bytes long.</param>
    /// <returns>Number of bytes written (8).</returns>
    public static int PackUInt64(ulong value, Span<byte> destination)
    {
        if (destination.Length < 8)
            throw new ArgumentException("Destination span must be at least 8 bytes long.");

        BinaryPrimitives.WriteUInt64LittleEndian(destination, value);
        return 8;
    }

    /// <summary>
    /// Unpacks a byte array into a 64-bit integer.
    /// </summary>
    public static ulong UnpackUInt64(byte[] bytes)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));
        if (bytes.Length < 8)
            throw new ArgumentException("Byte array must be at least 8 bytes long.");

        return BinaryPrimitives.ReadUInt64LittleEndian(bytes);
    }

    /// <summary>
    /// Unpacks a byte span into a 64-bit unsigned integer.
    /// </summary>
    public static ulong UnpackUInt64(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 8)
            throw new ArgumentException("Byte span must be at least 8 bytes long.");

        return BinaryPrimitives.ReadUInt64LittleEndian(bytes);
    }

    #endregion

    #region Integer Array

    /// <summary>
    /// Packs an array of 16-bit unsigned integers into a byte array.
    /// </summary>
    public static byte[] PackUInt16Array(ushort[] values)
    {
        if (values == null || values.Length == 0)
            return Array.Empty<byte>();

        byte[] result = new byte[values.Length * 2];
        PackUInt16Array(values, result);
        return result;
    }

    /// <summary>
    /// Packs an array of 16-bit unsigned integers into a byte span.
    /// </summary>
    /// <returns>Number of bytes written.</returns>
    public static int PackUInt16Array(ReadOnlySpan<ushort> values, Span<byte> destination)
    {
        if (values.Length == 0)
            return 0;

        if (destination.Length < values.Length * 2)
            throw new ArgumentException("Destination span is too small to hold all values");

        for (int i = 0; i < values.Length; i++)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(i * 2, 2), values[i]);
        }

        return values.Length * 2;
    }

    /// <summary>
    /// Unpacks a byte array into an array of 16-bit unsigned integers.
    /// </summary>
    public static ushort[] UnpackUInt16Array(byte[] bytes, int count)
    {
        if (bytes == null || bytes.Length == 0)
            return Array.Empty<ushort>();

        if (bytes.Length < count * 2)
            throw new ArgumentException("Byte array is too small for the specified count");

        ushort[] result = new ushort[count];
        UnpackUInt16Array(bytes, result);
        return result;
    }

    /// <summary>
    /// Unpacks a byte span into a span of 16-bit unsigned integers.
    /// </summary>
    /// <returns>Number of values unpacked.</returns>
    public static int UnpackUInt16Array(ReadOnlySpan<byte> bytes, Span<ushort> destination)
    {
        if (bytes.Length == 0 || destination.Length == 0)
            return 0;

        int count = Math.Min(bytes.Length / 2, destination.Length);

        for (int i = 0; i < count; i++)
        {
            destination[i] = BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(i * 2, 2));
        }

        return count;
    }

    /// <summary>
    /// Packs an array of 32-bit unsigned integers into a byte array.
    /// </summary>
    public static byte[] PackUInt32Array(uint[] values)
    {
        if (values == null || values.Length == 0)
            return Array.Empty<byte>();

        byte[] result = new byte[values.Length * 4];
        PackUInt32Array(values, result);
        return result;
    }

    /// <summary>
    /// Packs an array of 32-bit unsigned integers into a byte span.
    /// </summary>
    /// <returns>Number of bytes written.</returns>
    public static int PackUInt32Array(ReadOnlySpan<uint> values, Span<byte> destination)
    {
        if (values.Length == 0)
            return 0;

        if (destination.Length < values.Length * 4)
            throw new ArgumentException("Destination span is too small to hold all values");

        for (int i = 0; i < values.Length; i++)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(i * 4, 4), values[i]);
        }

        return values.Length * 4;
    }

    /// <summary>
    /// Unpacks a byte array into an array of 32-bit unsigned integers.
    /// </summary>
    public static uint[] UnpackUInt32Array(byte[] bytes, int count)
    {
        if (bytes == null || bytes.Length == 0)
            return Array.Empty<uint>();

        if (bytes.Length < count * 4)
            throw new ArgumentException("Byte array is too small for the specified count");

        uint[] result = new uint[count];
        UnpackUInt32Array(bytes, result);
        return result;
    }

    /// <summary>
    /// Unpacks a byte span into a span of 32-bit unsigned integers.
    /// </summary>
    /// <returns>Number of values unpacked.</returns>
    public static int UnpackUInt32Array(ReadOnlySpan<byte> bytes, Span<uint> destination)
    {
        if (bytes.Length == 0 || destination.Length == 0)
            return 0;

        int count = Math.Min(bytes.Length / 4, destination.Length);

        for (int i = 0; i < count; i++)
        {
            destination[i] = BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(i * 4, 4));
        }

        return count;
    }

    /// <summary>
    /// Packs an array of 64-bit unsigned integers into a byte array.
    /// </summary>
    public static byte[] PackUInt64Array(ulong[] values)
    {
        if (values == null || values.Length == 0)
            return Array.Empty<byte>();

        byte[] result = new byte[values.Length * 8];
        PackUInt64Array(values, result);
        return result;
    }

    /// <summary>
    /// Packs an array of 64-bit unsigned integers into a byte span.
    /// </summary>
    /// <returns>Number of bytes written.</returns>
    public static int PackUInt64Array(ReadOnlySpan<ulong> values, Span<byte> destination)
    {
        if (values.Length == 0)
            return 0;

        if (destination.Length < values.Length * 8)
            throw new ArgumentException("Destination span is too small to hold all values");

        for (int i = 0; i < values.Length; i++)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(destination.Slice(i * 8, 8), values[i]);
        }

        return values.Length * 8;
    }

    /// <summary>
    /// Unpacks a byte array into an array of 64-bit unsigned integers.
    /// </summary>
    public static ulong[] UnpackUInt64Array(byte[] bytes, int count)
    {
        if (bytes == null || bytes.Length == 0)
            return Array.Empty<ulong>();

        if (bytes.Length < count * 8)
            throw new ArgumentException("Byte array is too small for the specified count");

        ulong[] result = new ulong[count];
        UnpackUInt64Array(bytes, result);
        return result;
    }

    /// <summary>
    /// Unpacks a byte span into a span of 64-bit unsigned integers.
    /// </summary>
    /// <returns>Number of values unpacked.</returns>
    public static int UnpackUInt64Array(ReadOnlySpan<byte> bytes, Span<ulong> destination)
    {
        if (bytes.Length == 0 || destination.Length == 0)
            return 0;

        int count = Math.Min(bytes.Length / 8, destination.Length);

        for (int i = 0; i < count; i++)
        {
            destination[i] = BinaryPrimitives.ReadUInt64LittleEndian(bytes.Slice(i * 8, 8));
        }

        return count;
    }

    /// <summary>
    /// Calculates the number of bytes needed to store a specified number of values with a given bit size.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CalculateRequiredBytes(int valueCount, int bitsPerValue)
    {
        long totalBits = (long)valueCount * bitsPerValue;
        return (int)((totalBits + 7) >> 3); // Equivalent to Math.Ceiling(totalBits / 8.0) but faster
    }

    /// <summary>
    /// Returns the maximum integer value that can be represented with the given number of bits.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong GetMaxValueForBits(int bitCount)
    {
        if (bitCount <= 0 || bitCount > 64)
            throw new ArgumentOutOfRangeException(nameof(bitCount), "Bit count must be between 1 and 64");

        return bitCount == 64 ? ulong.MaxValue : (1UL << bitCount) - 1;
    }

    /// <summary>
    /// Packs an array of integers into a byte array where each integer uses a specific number of bits.
    /// </summary>
    /// <param name="values">Array of integers to pack</param>
    /// <param name="bitsPerValue">Number of bits used for each integer (1-16 for ushort, 1-32 for uint, 1-64 for ulong)</param>
    /// <returns>A byte array containing the packed bits</returns>
    public static byte[] Pack<T>(T[] values, int bitsPerValue) where T : struct
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));
        if (values.Length == 0)
            return Array.Empty<byte>();

        int maxBits = GetMaxBitsForType<T>();
        if (bitsPerValue <= 0 || bitsPerValue > maxBits)
            throw new ArgumentException($"Bits per value must be between 1 and {maxBits}.", nameof(bitsPerValue));

        int byteCount = CalculateRequiredBytes(values.Length, bitsPerValue);
        byte[] result = new byte[byteCount];

        Pack<T>(values.AsSpan(), bitsPerValue, result);
        return result;
    }

    /// <summary>
    /// Packs a span of integers into a byte span where each integer uses a specific number of bits.
    /// </summary>
    /// <returns>Number of bytes written.</returns>
    public static int Pack<T>(ReadOnlySpan<T> values, int bitsPerValue, Span<byte> destination) where T : struct
    {
        if (values.Length == 0)
            return 0;

        int maxBits = GetMaxBitsForType<T>();
        if (bitsPerValue <= 0 || bitsPerValue > maxBits)
            throw new ArgumentException($"Bits per value must be between 1 and {maxBits}.", nameof(bitsPerValue));

        int byteCount = CalculateRequiredBytes(values.Length, bitsPerValue);
        if (destination.Length < byteCount)
            throw new ArgumentException("Destination span is too small for the packed values");

        // Initialize destination to zero to avoid having to clear bits inside WriteBits
        destination.Slice(0, byteCount).Fill(0);

        int bitPosition = 0;
        ulong maxValue = GetMaxValueForBits(bitsPerValue);

        for (int i = 0; i < values.Length; i++)
        {
            ulong numericValue = ConvertToUInt64(values[i]);
            if (numericValue > maxValue)
                throw new ArgumentException($"Value {values[i]} requires more than {bitsPerValue} bits to represent.");

            WriteBits(destination, numericValue, bitPosition, bitsPerValue);
            bitPosition += bitsPerValue;
        }

        return byteCount;
    }

    /// <summary>
    /// Unpacks a byte array into an array of integers, where each integer used a specific number of bits.
    /// </summary>
    /// <param name="bytes">The packed byte array</param>
    /// <param name="count">Number of integers to unpack</param>
    /// <param name="bitsPerValue">Number of bits used for each integer (1-16 for ushort, 1-32 for uint, 1-64 for ulong)</param>
    /// <returns>An array of unpacked integers</returns>
    public static T[] Unpack<T>(byte[] bytes, int count, int bitsPerValue) where T : struct
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));
        if (bytes.Length == 0 || count == 0)
            return Array.Empty<T>();

        int maxBits = GetMaxBitsForType<T>();
        if (bitsPerValue <= 0 || bitsPerValue > maxBits)
            throw new ArgumentException($"Bits per value must be between 1 and {maxBits}.", nameof(bitsPerValue));

        int requiredBytes = CalculateRequiredBytes(count, bitsPerValue);
        if (bytes.Length < requiredBytes)
            throw new ArgumentException("Byte array is too small to contain the specified number of values.");

        T[] result = new T[count];
        Unpack<T>(bytes, bitsPerValue, result);
        return result;
    }

    /// <summary>
    /// Unpacks a byte span into a span of integers, where each integer uses a specific number of bits.
    /// </summary>
    /// <returns>Number of values unpacked.</returns>
    public static int Unpack<T>(ReadOnlySpan<byte> bytes, int bitsPerValue, Span<T> destination) where T : struct
    {
        if (bytes.Length == 0 || destination.Length == 0)
            return 0;

        int maxBits = GetMaxBitsForType<T>();
        if (bitsPerValue <= 0 || bitsPerValue > maxBits)
            throw new ArgumentException($"Bits per value must be between 1 and {maxBits}.", nameof(bitsPerValue));

        int maxCount = (bytes.Length * 8) / bitsPerValue;
        int count = Math.Min(maxCount, destination.Length);

        int bitPosition = 0;

        for (int i = 0; i < count; i++)
        {
            ulong value = ReadBits(bytes, bitPosition, bitsPerValue);
            destination[i] = ConvertFromUInt64<T>(value);
            bitPosition += bitsPerValue;
        }

        return count;
    }

    /// <summary>
    /// Writes bits to a byte span at a specified bit position.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteBits(Span<byte> bytes, ulong value, int bitPosition, int numBits)
    {
        int bytePos = bitPosition / 8;
        int bitPosInByte = bitPosition % 8;

        // Handle the common case where all bits fit in a single byte
        if (bitPosInByte + numBits <= 8)
        {
            byte mask = (byte)((1 << numBits) - 1);
            byte packedValue = (byte)((value & mask) << bitPosInByte);
            bytes[bytePos] |= packedValue;
            return;
        }

        // Handle more complex cases that span multiple bytes
        // First byte
        int bitsInFirstByte = 8 - bitPosInByte;
        byte firstMask = (byte)((1 << bitsInFirstByte) - 1);
        bytes[bytePos] |= (byte)((value & firstMask) << bitPosInByte);

        value >>= bitsInFirstByte;
        numBits -= bitsInFirstByte;
        bytePos++;

        // Middle bytes (full 8 bits each)
        while (numBits >= 8)
        {
            bytes[bytePos] = (byte)(value & 0xFF);
            value >>= 8;
            numBits -= 8;
            bytePos++;
        }

        // Last byte (if any bits remain)
        if (numBits > 0)
        {
            byte lastMask = (byte)((1 << numBits) - 1);
            bytes[bytePos] |= (byte)(value & lastMask);
        }
    }

    /// <summary>
    /// Reads bits from a byte span starting at a specified bit position.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ReadBits(ReadOnlySpan<byte> bytes, int bitPosition, int numBits)
    {
        int bytePos = bitPosition / 8;
        int bitPosInByte = bitPosition % 8;

        // Handle the common case where all bits are within a single byte
        if (bitPosInByte + numBits <= 8)
        {
            byte mask = (byte)((1 << numBits) - 1);
            return (ulong)((bytes[bytePos] >> bitPosInByte) & mask);
        }

        // Handle more complex cases that span multiple bytes
        ulong result = 0;
        int bitsRead = 0;

        // First byte - partial
        int bitsInFirstByte = 8 - bitPosInByte;
        byte firstMask = (byte)((1 << bitsInFirstByte) - 1);
        result = (ulong)(bytes[bytePos] >> bitPosInByte) & firstMask;

        bitsRead += bitsInFirstByte;
        bytePos++;

        // Middle bytes (full 8 bits each)
        while (bitsRead + 8 <= numBits)
        {
            result |= (ulong)bytes[bytePos] << bitsRead;
            bitsRead += 8;
            bytePos++;
        }

        // Last byte - partial (if any bits remain)
        int remainingBits = numBits - bitsRead;
        if (remainingBits > 0)
        {
            byte lastMask = (byte)((1 << remainingBits) - 1);
            result |= (ulong)(bytes[bytePos] & lastMask) << bitsRead;
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetMaxBitsForType<T>() where T : struct
    {
        if (typeof(T) == typeof(ushort))
            return 16;
        if (typeof(T) == typeof(uint))
            return 32;
        if (typeof(T) == typeof(ulong))
            return 64;
        throw new NotSupportedException($"Type {typeof(T).Name} is not supported. Only ushort, uint, and ulong are supported.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ConvertToUInt64<T>(T value) where T : struct
    {
        if (typeof(T) == typeof(ushort))
            return Unsafe.As<T, ushort>(ref value);
        if (typeof(T) == typeof(uint))
            return Unsafe.As<T, uint>(ref value);
        if (typeof(T) == typeof(ulong))
            return Unsafe.As<T, ulong>(ref value);
        throw new NotSupportedException($"Type {typeof(T).Name} is not supported. Only ushort, uint, and ulong are supported.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T ConvertFromUInt64<T>(ulong value) where T : struct
    {
        if (typeof(T) == typeof(ushort))
        {
            var result = (ushort)(value & 0xFFFF);
            return Unsafe.As<ushort, T>(ref result);
        }
        if (typeof(T) == typeof(uint))
        {
            var result = (uint)(value & 0xFFFFFFFF);
            return Unsafe.As<uint, T>(ref result);
        }
        if (typeof(T) == typeof(ulong))
            return Unsafe.As<ulong, T>(ref value);
        throw new NotSupportedException($"Type {typeof(T).Name} is not supported. Only ushort, uint, and ulong are supported.");
    }

    #endregion
}