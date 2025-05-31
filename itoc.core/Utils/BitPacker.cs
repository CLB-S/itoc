namespace ITOC.Core.Utils;

public static class BitPacker
{
    #region Single Interger

    /// <summary>
    /// Packs a 16-bit unsigned integer into a byte array.
    /// </summary>
    public static byte[] PackUInt16(ushort value)
    {
        var bytes = new byte[2];
        bytes[0] = (byte)(value & 0xFF);
        bytes[1] = (byte)((value >> 8) & 0xFF);
        return bytes;
    }

    /// <summary>
    /// Unpacks a byte array into a 16-bit unsigned integer.
    /// </summary>
    public static ushort UnpackUInt16(byte[] bytes)
    {
        if (bytes.Length != 2)
            throw new ArgumentException("Byte array must be exactly 2 bytes long.");

        return (ushort)(bytes[0] | (bytes[1] << 8));
    }

    /// <summary>
    /// Packs a 32-bit integer into a byte array.
    /// </summary>
    public static byte[] PackUInt32(uint value)
    {
        var bytes = new byte[4];
        bytes[0] = (byte)(value & 0xFFu);
        bytes[1] = (byte)((value >> 8) & 0xFFu);
        bytes[2] = (byte)((value >> 16) & 0xFFu);
        bytes[3] = (byte)((value >> 24) & 0xFFu);
        return bytes;
    }

    /// <summary>
    /// Unpacks a byte array into a 32-bit integer.
    /// </summary>
    public static uint UnpackUInt32(byte[] bytes)
    {
        if (bytes.Length != 4)
            throw new ArgumentException("Byte array must be exactly 4 bytes long.");

        return bytes[0] | (uint)(bytes[1] << 8) | (uint)(bytes[2] << 16) | (uint)(bytes[3] << 24);
    }

    /// <summary>
    /// Packs a 64-bit integer into a byte array.
    /// </summary>
    public static byte[] PackUInt64(ulong value)
    {
        var bytes = new byte[8];
        bytes[0] = (byte)(value & 0xFFu);
        bytes[1] = (byte)((value >> 8) & 0xFFu);
        bytes[2] = (byte)((value >> 16) & 0xFFu);
        bytes[3] = (byte)((value >> 24) & 0xFFu);
        bytes[4] = (byte)((value >> 32) & 0xFFu);
        bytes[5] = (byte)((value >> 40) & 0xFFu);
        bytes[6] = (byte)((value >> 48) & 0xFFu);
        bytes[7] = (byte)((value >> 56) & 0xFFu);
        return bytes;
    }

    /// <summary>
    /// Unpacks a byte array into a 64-bit integer.
    /// </summary>
    public static ulong UnpackUInt64(byte[] bytes)
    {
        if (bytes.Length != 8)
            throw new ArgumentException("Byte array must be exactly 8 bytes long.");

        return bytes[0] | ((ulong)bytes[1] << 8) | ((ulong)bytes[2] << 16) |
               ((ulong)bytes[3] << 24) | ((ulong)bytes[4] << 32) | ((ulong)bytes[5] << 40) |
               ((ulong)bytes[6] << 48) | ((ulong)bytes[7] << 56);
    }

    #endregion

    #region Interger Array

    public static byte[] PackUInt16Array(ushort[] values)
    {
        if (values == null || values.Length == 0)
            return [];

        byte[] result = new byte[values.Length * 2];
        Buffer.BlockCopy(values, 0, result, 0, result.Length);

        return result;
    }

    public static byte[] PackUInt32Array(uint[] values)
    {
        if (values == null || values.Length == 0)
            return Array.Empty<byte>();

        byte[] result = new byte[values.Length * 4];
        Buffer.BlockCopy(values, 0, result, 0, result.Length);

        return result;
    }

    public static byte[] PackUInt64Array(ulong[] values)
    {
        if (values == null || values.Length == 0)
            return Array.Empty<byte>();

        byte[] result = new byte[values.Length * 8];
        Buffer.BlockCopy(values, 0, result, 0, result.Length);

        return result;
    }

    /// <summary>
    /// Calculates the number of bytes needed to store a specified number of values with a given bit size.
    /// </summary>
    public static int CalculateRequiredBytes(int valueCount, int bitsPerValue)
    {
        long totalBits = (long)valueCount * bitsPerValue;
        return (int)Math.Ceiling(totalBits / 8.0);
    }

    /// <summary>
    /// Packs an array of integers into a byte array where each integer uses a specific number of bits.
    /// </summary>
    /// <param name="values">Array of integers to pack</param>
    /// <param name="bitsPerValue">Number of bits used for each integer (1-32 for uint/ushort, 1-64 for ulong)</param>
    /// <returns>A byte array containing the packed bits</returns>
    public static byte[] Pack<T>(T[] values, int bitsPerValue) where T : struct
    {
        if (values == null || values.Length == 0)
            return [];

        int maxBits = GetMaxBitsForType<T>();
        if (bitsPerValue <= 0 || bitsPerValue > maxBits)
            throw new ArgumentException($"Bits per value must be between 1 and {maxBits}.", nameof(bitsPerValue));

        int byteCount = CalculateRequiredBytes(values.Length, bitsPerValue);
        byte[] result = new byte[byteCount];

        int bitPosition = 0;

        foreach (var value in values)
        {
            ulong numericValue = ConvertToUInt64(value);
            if ((numericValue >> bitsPerValue) > 0)
                throw new ArgumentException($"Value {value} requires more than {bitsPerValue} bits to represent.");

            WriteBits(result, numericValue, bitPosition, bitsPerValue);
            bitPosition += bitsPerValue;
        }

        return result;
    }

    /// <summary>
    /// Unpacks a byte array into an array of integers, where each integer used a specific number of bits.
    /// </summary>
    /// <param name="bytes">The packed byte array</param>
    /// <param name="count">Number of integers to unpack</param>
    /// <param name="bitsPerValue">Number of bits used for each integer (1-32 for uint/ushort, 1-64 for ulong)</param>
    /// <returns>An array of unpacked integers</returns>
    public static T[] Unpack<T>(byte[] bytes, int count, int bitsPerValue) where T : struct
    {
        if (bytes == null || bytes.Length == 0)
            return Array.Empty<T>();

        int maxBits = GetMaxBitsForType<T>();
        if (bitsPerValue <= 0 || bitsPerValue > maxBits)
            throw new ArgumentException($"Bits per value must be between 1 and {maxBits}.", nameof(bitsPerValue));

        long totalBitsRequired = (long)count * bitsPerValue;
        if (bytes.Length < Math.Ceiling(totalBitsRequired / 8.0))
            throw new ArgumentException("Byte array is too small to contain the specified number of values.");

        T[] result = new T[count];
        int bitPosition = 0;

        for (int i = 0; i < count; i++)
        {
            ulong value = ReadBits(bytes, bitPosition, bitsPerValue);
            result[i] = ConvertFromUInt64<T>(value);
            bitPosition += bitsPerValue;
        }

        return result;
    }

    /// <summary>
    /// Writes bits to a byte array at a specified bit position.
    /// </summary>
    private static void WriteBits(byte[] bytes, ulong value, int bitPosition, int numBits)
    {
        int bytePos = bitPosition / 8;
        int bitPosInByte = bitPosition % 8;

        ulong mask = (numBits == 64) ? ulong.MaxValue : (1UL << numBits) - 1;
        value &= mask;

        while (numBits > 0)
        {
            int bitsToWrite = Math.Min(8 - bitPosInByte, numBits);
            mask = (bitsToWrite == 64) ? ulong.MaxValue : (1UL << bitsToWrite) - 1;

            byte currentByteMask = (byte)(mask << bitPosInByte);
            byte currentByteValue = (byte)((value & mask) << bitPosInByte);

            bytes[bytePos] = (byte)((bytes[bytePos] & ~currentByteMask) | currentByteValue);

            value >>= bitsToWrite;
            numBits -= bitsToWrite;
            bytePos++;
            bitPosInByte = 0;
        }
    }

    /// <summary>
    /// Reads bits from a byte array starting at a specified bit position.
    /// </summary>
    private static ulong ReadBits(byte[] bytes, int bitPosition, int numBits)
    {
        int bytePos = bitPosition / 8;
        int bitPosInByte = bitPosition % 8;

        ulong result = 0;
        int bitsRead = 0;

        while (bitsRead < numBits)
        {
            int bitsToRead = Math.Min(8 - bitPosInByte, numBits - bitsRead);
            ulong mask = (bitsToRead == 64) ? ulong.MaxValue : (1UL << bitsToRead) - 1;

            ulong bits = (ulong)(bytes[bytePos] >> bitPosInByte) & mask;
            result |= bits << bitsRead;

            bitsRead += bitsToRead;
            bytePos++;
            bitPosInByte = 0;
        }

        return result;
    }

    private static int GetMaxBitsForType<T>()
    {
        if (typeof(T) == typeof(ushort))
            return 16;
        if (typeof(T) == typeof(uint))
            return 32;
        if (typeof(T) == typeof(ulong))
            return 64;
        throw new NotSupportedException($"Type {typeof(T).Name} is not supported.");
    }

    private static ulong ConvertToUInt64<T>(T value) where T : struct
    {
        if (value is ushort us) return us;
        if (value is uint ui) return ui;
        if (value is ulong ul) return ul;
        throw new NotSupportedException($"Type {typeof(T).Name} is not supported.");
    }

    private static T ConvertFromUInt64<T>(ulong value) where T : struct
    {
        if (typeof(T) == typeof(ushort)) return (T)(object)(ushort)value;
        if (typeof(T) == typeof(uint)) return (T)(object)(uint)value;
        if (typeof(T) == typeof(ulong)) return (T)(object)value;
        throw new NotSupportedException($"Type {typeof(T).Name} is not supported.");
    }

    #endregion
}