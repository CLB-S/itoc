namespace ITOC.Core.Utils;

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

/// <summary>
/// A memory-efficient array that stores values using a specified number of bits per value.
/// </summary>
/// <typeparam name="T">The type of values to store. Must be ushort, uint, or ulong.</typeparam>
public sealed class BitPackedArray<T> : IEnumerable<T>, IDisposable where T : struct
{
    private static ArrayPool<byte> _bytePool = ArrayPool<byte>.Shared;

    private readonly byte[] _data;
    private readonly int _dataValidBytes;
    private readonly int _bitsPerValue;
    private readonly int _count;
    private readonly ulong _maxValue;
    private bool _isDisposed;

    /// <summary>
    /// Gets the number of elements in the array.
    /// </summary>
    public int Count => _count;

    /// <summary>
    /// Gets the number of bits used to store each value.
    /// </summary>
    public int BitsPerValue => _bitsPerValue;

    /// <summary>
    /// Gets the maximum value that can be stored in the array based on the bits per value.
    /// </summary>
    public ulong MaxValue => _maxValue;

    /// <summary>
    /// Gets the size of the underlying storage in bytes.
    /// </summary>
    public int SizeInBytes => _dataValidBytes;

    /// <summary>
    /// Gets the total size of the array in bits.
    /// </summary>
    public long SizeInBits => (long)_count * _bitsPerValue;

    /// <summary>
    /// Creates a new bit-packed array with the specified count and bits per value.
    /// </summary>
    /// <param name="count">The number of elements in the array.</param>
    /// <param name="bitsPerValue">The number of bits used to store each value.</param>
    public BitPackedArray(int count, int bitsPerValue)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than or equal to zero.");

        int maxBits = GetMaxBitsForType<T>();
        if (bitsPerValue <= 0 || bitsPerValue > maxBits)
            throw new ArgumentOutOfRangeException(nameof(bitsPerValue), $"Bits per value must be between 1 and {maxBits}.");

        _count = count;
        _bitsPerValue = bitsPerValue;
        _maxValue = _bitsPerValue == 64 ? ulong.MaxValue : (1ul << _bitsPerValue) - 1;

        // Calculate the total number of bytes needed to store the values
        int byteCount = BitPacker.CalculateRequiredBytes(count, bitsPerValue);
        // _data = new byte[byteCount];
        _data = _bytePool.Rent(byteCount);
        Array.Clear(_data, 0, byteCount);
        _dataValidBytes = byteCount;
    }

    /// <summary>
    /// Creates a new bit-packed array with the specified values and bits per value.
    /// </summary>
    /// <param name="values">The values to store in the array.</param>
    /// <param name="bitsPerValue">The number of bits used to store each value.</param>
    public BitPackedArray(T[] values, int bitsPerValue)
    {
        ArgumentNullException.ThrowIfNull(values);

        int maxBits = GetMaxBitsForType<T>();
        if (bitsPerValue <= 0 || bitsPerValue > maxBits)
            throw new ArgumentOutOfRangeException(nameof(bitsPerValue), $"Bits per value must be between 1 and {maxBits}.");

        _count = values.Length;
        _bitsPerValue = bitsPerValue;
        _maxValue = _bitsPerValue == 64 ? ulong.MaxValue : (1ul << _bitsPerValue) - 1;

        // Calculate the total number of bytes needed to store the values
        int byteCount = BitPacker.CalculateRequiredBytes(_count, _bitsPerValue);
        _data = _bytePool.Rent(byteCount);
        Array.Clear(_data, 0, byteCount);
        _dataValidBytes = byteCount;

        // Pack the values into the data array
        BitPacker.Pack<T>(values, _bitsPerValue, _data);
    }

    /// <summary>
    /// Creates a new bit-packed array from packed data.
    /// </summary>
    /// <param name="packedData">The packed data.</param>
    /// <param name="count">The number of values.</param>
    /// <param name="bitsPerValue">The number of bits per value.</param>
    public BitPackedArray(byte[] packedData, int count, int bitsPerValue)
    {
        ArgumentNullException.ThrowIfNull(packedData);
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than or equal to zero.");

        int maxBits = GetMaxBitsForType<T>();
        if (bitsPerValue <= 0 || bitsPerValue > maxBits)
            throw new ArgumentOutOfRangeException(nameof(bitsPerValue), $"Bits per value must be between 1 and {maxBits}.");

        int requiredBytes = BitPacker.CalculateRequiredBytes(count, bitsPerValue);
        if (packedData.Length < requiredBytes)
            throw new ArgumentException($"Packed data too small. Expected at least {requiredBytes} bytes for {count} values with {bitsPerValue} bits per value.");

        _count = count;
        _bitsPerValue = bitsPerValue;
        _maxValue = _bitsPerValue == 64 ? ulong.MaxValue : (1ul << _bitsPerValue) - 1;

        // Copy the packed data
        // _data = new byte[requiredBytes];
        _data = _bytePool.Rent(requiredBytes);
        Array.Clear(_data, 0, requiredBytes);
        _dataValidBytes = requiredBytes;
        Buffer.BlockCopy(packedData, 0, _data, 0, requiredBytes);
    }

    /// <summary>
    /// Gets or sets the value at the specified index.
    /// </summary>
    /// <param name="index">The index of the value.</param>
    /// <returns>The value at the specified index.</returns>
    public T this[int index]
    {
        get
        {
            ObjectDisposedException.ThrowIf(_isDisposed, nameof(BitPackedArray<T>));

            if (index < 0 || index >= _count)
                throw new IndexOutOfRangeException($"Index {index} is out of range [0, {_count - 1}].");

            int bitPosition = index * _bitsPerValue;
            ulong value = ReadBits(_data, bitPosition, _bitsPerValue);
            return ConvertFromUInt64<T>(value);
        }
        set
        {
            ObjectDisposedException.ThrowIf(_isDisposed, nameof(BitPackedArray<T>));

            if (index < 0 || index >= _count)
                throw new IndexOutOfRangeException($"Index {index} is out of range [0, {_count - 1}].");

            // Convert and validate the value
            ulong numericValue = ConvertToUInt64(value);
            if (numericValue > _maxValue)
                throw new ArgumentOutOfRangeException(nameof(value), $"Value {value} requires more than {_bitsPerValue} bits to represent.");

            int bitPosition = index * _bitsPerValue;
            WriteBits(_data, numericValue, bitPosition, _bitsPerValue);
        }
    }

    /// <summary>
    /// Sets all values in the array to the specified value.
    /// </summary>
    /// <param name="value">The value to set.</param>
    public void Fill(T value)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, nameof(BitPackedArray<T>));

        // Convert and validate the value
        ulong numericValue = ConvertToUInt64(value);
        if (numericValue > _maxValue)
            throw new ArgumentOutOfRangeException(nameof(value), $"Value {value} requires more than {_bitsPerValue} bits to represent.");

        // Special case: If the bits per value is a power of 2 that divides byte size, we can optimize
        if (_bitsPerValue == 8 || _bitsPerValue == 16 || _bitsPerValue == 32 || _bitsPerValue == 64)
        {
            FillFast(numericValue);
            return;
        }

        // General case: Set each individual value
        for (int i = 0; i < _count; i++)
        {
            int bitPosition = i * _bitsPerValue;
            WriteBits(_data, numericValue, bitPosition, _bitsPerValue);
        }
    }

    /// <summary>
    /// Optimized fill for specific bit sizes that align with byte boundaries.
    /// </summary>
    private void FillFast(ulong numericValue)
    {
        if (_bitsPerValue == 8)
        {
            byte byteValue = (byte)numericValue;
            Array.Fill(_data, byteValue, 0, _dataValidBytes);
        }
        else if (_bitsPerValue == 16)
        {
            // Fill pattern for 16-bit values
            var pattern = new byte[2];
            System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(pattern, (ushort)numericValue);

            for (int i = 0; i < _dataValidBytes; i += 2)
            {
                int remaining = Math.Min(2, _dataValidBytes - i);
                Buffer.BlockCopy(pattern, 0, _data, i, remaining);
            }
        }
        else if (_bitsPerValue == 32)
        {
            // Fill pattern for 32-bit values
            var pattern = new byte[4];
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(pattern, (uint)numericValue);

            for (int i = 0; i < _dataValidBytes; i += 4)
            {
                int remaining = Math.Min(4, _dataValidBytes - i);
                Buffer.BlockCopy(pattern, 0, _data, i, remaining);
            }
        }
        else if (_bitsPerValue == 64)
        {
            // Fill pattern for 64-bit values
            var pattern = new byte[8];
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(pattern, numericValue);

            for (int i = 0; i < _dataValidBytes; i += 8)
            {
                int remaining = Math.Min(8, _dataValidBytes - i);
                Buffer.BlockCopy(pattern, 0, _data, i, remaining);
            }
        }
    }

    /// <summary>
    /// Copies the values from this array to a destination array.
    /// </summary>
    /// <param name="destinationArray">The destination array.</param>
    /// <param name="startIndex">The index in the destination array at which to start copying.</param>
    public void CopyTo(T[] destinationArray, int startIndex = 0)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, nameof(BitPackedArray<T>));

        ArgumentNullException.ThrowIfNull(destinationArray);
        if (startIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(startIndex), "Start index cannot be negative.");
        if (destinationArray.Length - startIndex < _count)
            throw new ArgumentException("Destination array is too small.");

        BitPacker.Unpack<T>(_data, _bitsPerValue, destinationArray.AsSpan().Slice(startIndex, _count));
    }

    /// <summary>
    /// Gets a copy of the underlying packed data.
    /// </summary>
    /// <returns>A byte array containing the packed data.</returns>
    public byte[] GetPackedData()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, nameof(BitPackedArray<T>));

        var copy = new byte[_dataValidBytes];
        Buffer.BlockCopy(_data, 0, copy, 0, _dataValidBytes);
        return copy;
    }

    /// <summary>
    /// Returns all values in the array as an array of the specified type.
    /// </summary>
    /// <returns>An array containing all values.</returns>
    public T[] ToArray()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, nameof(BitPackedArray<T>));

        var result = new T[_count];
        CopyTo(result);
        return result;
    }

    /// <summary>
    /// Creates a new bit-packed array with values from the specified collection.
    /// </summary>
    /// <param name="values">The values to store.</param>
    /// <param name="bitsPerValue">The number of bits per value.</param>
    /// <returns>A new bit-packed array.</returns>
    public static BitPackedArray<T> Create(IEnumerable<T> values, int bitsPerValue)
    {
        // First, count the elements and validate the max value
        int count = 0;
        ulong maxValue = 0;

        foreach (var value in values)
        {
            count++;
            ulong numericValue = ConvertToUInt64(value);
            maxValue = Math.Max(maxValue, numericValue);
        }

        // Calculate minimum bits required if not specified
        if (bitsPerValue <= 0)
        {
            bitsPerValue = 1;
            while ((1UL << bitsPerValue) - 1 < maxValue && bitsPerValue < 64)
                bitsPerValue++;
        }
        else
        {
            // Validate that all values fit in the specified bits
            ulong maxAllowedValue = bitsPerValue == 64 ? ulong.MaxValue : (1UL << bitsPerValue) - 1;
            if (maxValue > maxAllowedValue)
                throw new ArgumentException($"Some values require more than {bitsPerValue} bits to represent.");
        }

        // Create the array
        var result = new BitPackedArray<T>(count, bitsPerValue);

        // Fill the array
        int index = 0;
        foreach (var value in values)
            result[index++] = value;

        return result;
    }

    /// <summary>
    /// Creates a new bit-packed array with the minimum number of bits required to represent all values.
    /// </summary>
    /// <param name="values">The values to store.</param>
    /// <returns>A new bit-packed array.</returns>
    public static BitPackedArray<T> CreateWithMinimumBits(IEnumerable<T> values)
    {
        return Create(values, 0); // 0 triggers automatic calculation of minimum bits
    }

    /// <summary>
    /// Creates a new bit-packed array from a span of values with the specified bits per value.
    /// </summary>
    /// <param name="values">The values to store.</param>
    /// <param name="bitsPerValue">The number of bits per value.</param>
    /// <returns>A new bit-packed array.</returns>
    public static BitPackedArray<T> Create(ReadOnlySpan<T> values, int bitsPerValue)
    {
        if (values.Length == 0)
            return new BitPackedArray<T>(0, bitsPerValue > 0 ? bitsPerValue : 1);

        // Find max value if we need to calculate minimum bits
        ulong maxValue = 0;
        if (bitsPerValue <= 0)
        {
            for (int i = 0; i < values.Length; i++)
            {
                ulong numericValue = ConvertToUInt64(values[i]);
                maxValue = Math.Max(maxValue, numericValue);
            }

            bitsPerValue = 1;
            while (((1UL << bitsPerValue) - 1) < maxValue && bitsPerValue < 64)
                bitsPerValue++;
        }

        // Create the array and pack the values
        BitPackedArray<T> result = new BitPackedArray<T>(values.Length, bitsPerValue);
        BitPacker.Pack<T>(values, bitsPerValue, result._data);

        return result;
    }

    /// <summary>
    /// Creates a resized copy of this bit-packed array.
    /// </summary>
    /// <param name="newSize">The new size of the array.</param>
    /// <returns>A new bit-packed array with the resized data.</returns>
    public BitPackedArray<T> Resize(int newSize)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, nameof(BitPackedArray<T>));

        if (newSize < 0)
            throw new ArgumentOutOfRangeException(nameof(newSize), "New size cannot be negative.");

        if (newSize == _count)
        {
            // No resize needed, return a clone
            return new BitPackedArray<T>(GetPackedData(), _count, _bitsPerValue);
        }

        var result = new BitPackedArray<T>(newSize, _bitsPerValue);

        if (newSize > 0 && _count > 0)
        {
            // Copy the values to the new array
            int copyCount = Math.Min(newSize, _count);
            int bytesToCopy = BitPacker.CalculateRequiredBytes(copyCount, _bitsPerValue);

            // Direct copy for complete bytes
            int completeBytes = (copyCount * _bitsPerValue) / 8;
            if (completeBytes > 0)
                Buffer.BlockCopy(_data, 0, result._data, 0, completeBytes);

            // Copy the remaining bits
            int remainingBits = (copyCount * _bitsPerValue) % 8;
            if (remainingBits > 0 && completeBytes < _dataValidBytes)
            {
                byte lastByte = _data[completeBytes];
                byte mask = (byte)((1 << remainingBits) - 1);
                result._data[completeBytes] = (byte)(lastByte & mask);
            }
        }

        return result;
    }

    /// <summary>
    /// Estimates the memory usage of the bit-packed array in bytes.
    /// </summary>
    /// <returns>The estimated memory usage in bytes.</returns>
    public int EstimateMemoryUsage()
    {
        // Object overhead + field storage + array overhead + array data
        int objectOverhead = 24; // Approximate .NET object overhead
        int fieldStorage = 5 * sizeof(int) + sizeof(bool); // _data reference, _bitsPerValue, _count, _maxValue, _isDisposed
        int arrayOverhead = 24; // Approximate array overhead
        int arrayData = _data.Length;

        return objectOverhead + fieldStorage + arrayOverhead + arrayData;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the values in the bit-packed array.
    /// </summary>
    public IEnumerator<T> GetEnumerator()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, nameof(BitPackedArray<T>));

        for (int i = 0; i < _count; i++)
            yield return this[i];
    }

    /// <summary>
    /// Returns an enumerator that iterates through the values in the bit-packed array.
    /// </summary>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Disposes the bit-packed array, releasing any resources it holds.
    /// </summary>
    public void Dispose()
    {
        if (!_isDisposed)
        {
            _bytePool.Return(_data);
            _isDisposed = true;
        }
    }

    ~BitPackedArray()
    {
        Dispose();
    }

    /// <summary>
    /// Writes bits to a byte array at a specified bit position.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteBits(byte[] bytes, ulong value, int bitPosition, int numBits)
    {
        int bytePos = bitPosition / 8;
        int bitPosInByte = bitPosition % 8;

        // Handle the common case where all bits fit in a single byte
        if (bitPosInByte + numBits <= 8)
        {
            byte mask = (byte)((1 << numBits) - 1);
            byte packedValue = (byte)((value & mask) << bitPosInByte);
            byte clearMask = (byte)~(mask << bitPosInByte);
            bytes[bytePos] = (byte)((bytes[bytePos] & clearMask) | packedValue);
            return;
        }

        // Handle more complex cases that span multiple bytes
        // First byte
        int bitsInFirstByte = 8 - bitPosInByte;
        byte firstMask = (byte)((1 << bitsInFirstByte) - 1);
        byte clearFirstMask = (byte)~(firstMask << bitPosInByte);
        bytes[bytePos] = (byte)((bytes[bytePos] & clearFirstMask) | ((byte)(value & firstMask) << bitPosInByte));

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
            byte clearLastMask = (byte)~lastMask;
            bytes[bytePos] = (byte)((bytes[bytePos] & clearLastMask) | ((byte)(value & lastMask)));
        }
    }

    /// <summary>
    /// Reads bits from a byte array starting at a specified bit position.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ReadBits(byte[] bytes, int bitPosition, int numBits)
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
        ulong result;
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
    private static int GetMaxBitsForType<TValue>() where TValue : struct
    {
        if (typeof(TValue) == typeof(ushort))
            return 16;
        if (typeof(TValue) == typeof(uint))
            return 32;
        if (typeof(TValue) == typeof(ulong))
            return 64;
        throw new NotSupportedException($"Type {typeof(TValue).Name} is not supported. Only ushort, uint, and ulong are supported.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ConvertToUInt64<TValue>(TValue value) where TValue : struct
    {
        if (typeof(TValue) == typeof(ushort))
            return Unsafe.As<TValue, ushort>(ref value);
        if (typeof(TValue) == typeof(uint))
            return Unsafe.As<TValue, uint>(ref value);
        if (typeof(TValue) == typeof(ulong))
            return Unsafe.As<TValue, ulong>(ref value);
        throw new NotSupportedException($"Type {typeof(TValue).Name} is not supported. Only ushort, uint, and ulong are supported.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TValue ConvertFromUInt64<TValue>(ulong value) where TValue : struct
    {
        if (typeof(TValue) == typeof(ushort))
        {
            var result = (ushort)(value & 0xFFFF);
            return Unsafe.As<ushort, TValue>(ref result);
        }
        if (typeof(TValue) == typeof(uint))
        {
            var result = (uint)(value & 0xFFFFFFFF);
            return Unsafe.As<uint, TValue>(ref result);
        }
        if (typeof(TValue) == typeof(ulong))
            return Unsafe.As<ulong, TValue>(ref value);
        throw new NotSupportedException($"Type {typeof(TValue).Name} is not supported. Only ushort, uint, and ulong are supported.");
    }
}
