namespace ITOC.Core.Utils;

using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// A memory-efficient array that stores values using a palette system.
/// Values are stored as indices to a palette, and the indices are packed using a BitPackedArray.
/// </summary>
/// <typeparam name="T">The type of values to store.</typeparam>
public sealed class PaletteArray<T> : IEnumerable<T>, IDisposable
    where T : IEquatable<T>
{
    private BitPackedArray<uint> _indices;
    private readonly List<T> _palette = new();
    private readonly Dictionary<T, uint> _valueToIndex = new(EqualityComparer<T>.Default);
    private readonly T _defaultValue;
    private readonly int _initialBits;
    private bool _isDisposed;

    /// <summary>
    /// Gets the number of elements in the array.
    /// </summary>
    public int Count => _indices?.Count ?? 0;

    /// <summary>
    /// Gets the number of unique values in the palette.
    /// </summary>
    public int PaletteSize => _palette.Count;

    /// <summary>
    /// Gets the number of bits used to store each index.
    /// </summary>
    public int BitsPerValue => _indices?.BitsPerValue ?? _initialBits;

    /// <summary>
    /// Gets the default value that is returned for out-of-range indices.
    /// </summary>
    public T DefaultValue => _defaultValue;

    /// <summary>
    /// Creates a new PaletteArray with the specified capacity and default value.
    /// </summary>
    /// <param name="size">The capacity of the array.</param>
    /// <param name="defaultValue">The default value to use.</param>
    /// <param name="initialBits">The initial number of bits per value.</param>
    public PaletteArray(int size, T defaultValue, int initialBits = 4)
    {
        if (size < 0)
            throw new ArgumentOutOfRangeException(nameof(size), "Size must be non-negative.");

        if (initialBits <= 0 || initialBits > 32)
            throw new ArgumentOutOfRangeException(
                nameof(initialBits),
                "Bits per value must be between 1 and 32."
            );

        _defaultValue = defaultValue;
        _initialBits = initialBits;

        // Add default value to palette
        _palette.Add(defaultValue);
        _valueToIndex[defaultValue] = 0;

        // Create the bit-packed array for indices
        if (size > 0)
            _indices = new BitPackedArray<uint>(size, initialBits);
    }

    /// <summary>
    /// Creates a new PaletteArray with the specified values.
    /// </summary>
    /// <param name="values">The values to store in the array.</param>
    /// <param name="defaultValue">The default value to use.</param>
    /// <param name="initialBits">The initial number of bits per value.</param>
    public PaletteArray(IEnumerable<T> values, T defaultValue, int initialBits = 4)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(defaultValue);

        if (initialBits <= 0 || initialBits > 32)
            throw new ArgumentOutOfRangeException(
                nameof(initialBits),
                "Bits per value must be between 1 and 32."
            );

        _defaultValue = defaultValue;
        _initialBits = initialBits;

        // Add default value to palette
        _palette.Add(defaultValue);
        _valueToIndex[defaultValue] = 0;

        // First pass: count items and build palette
        var valuesList = new List<T>(values);

        foreach (var value in valuesList)
        {
            var v = value ?? _defaultValue;
            if (!_valueToIndex.ContainsKey(v))
            {
                var index = (uint)_palette.Count;
                _palette.Add(v);
                _valueToIndex[v] = index;
            }
        }

        // Calculate required bits
        var requiredBits = CalculateRequiredBits(_palette.Count);

        // Create the bit-packed array for indices
        if (valuesList.Count > 0)
        {
            _indices = new BitPackedArray<uint>(valuesList.Count, requiredBits);

            // Second pass: populate the bit-packed array
            for (var i = 0; i < valuesList.Count; i++)
                _indices[i] = _valueToIndex[valuesList[i] ?? _defaultValue];
        }
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
            ObjectDisposedException.ThrowIf(_isDisposed, typeof(PaletteArray<T>));

            if (_indices == null || index < 0 || index >= _indices.Count)
                return _defaultValue;

            var paletteIndex = _indices[index];
            return paletteIndex < _palette.Count ? _palette[(int)paletteIndex] : _defaultValue;
        }
        set
        {
            ObjectDisposedException.ThrowIf(_isDisposed, typeof(PaletteArray<T>));

            EnsureCapacity(index);

            if (!_valueToIndex.TryGetValue(value, out var paletteIndex))
            {
                // Add the new value to the palette
                paletteIndex = (uint)_palette.Count;
                _palette.Add(value);
                _valueToIndex[value] = paletteIndex;

                // Check if we need more bits
                var requiredBits = CalculateRequiredBits(_palette.Count);
                if (requiredBits > _indices.BitsPerValue)
                    ResizeIndices(requiredBits);
            }

            _indices[index] = paletteIndex;
        }
    }

    /// <summary>
    /// Ensures that the array has at least the specified capacity.
    /// </summary>
    /// <param name="index">The index that needs to be accommodated.</param>
    private void EnsureCapacity(int index)
    {
        if (_indices == null)
        {
            // Create array with initial size
            var initialSize = Math.Max(index + 1, 16);
            _indices = new BitPackedArray<uint>(initialSize, _initialBits);
        }
        else if (index >= _indices.Count)
        {
            // Need to increase size
            var newSize = Math.Max(index + 1, _indices.Count * 2);
            ResizeArray(newSize);
        }
    }

    /// <summary>
    /// Resizes the array to the specified size.
    /// </summary>
    /// <param name="newSize">The new size of the array.</param>
    private void ResizeArray(int newSize)
    {
        var newIndices = new BitPackedArray<uint>(newSize, _indices.BitsPerValue);

        // Copy existing values
        for (var i = 0; i < _indices.Count; i++)
            newIndices[i] = _indices[i];

        // Replace the old array
        var oldIndices = _indices;
        _indices = newIndices;
        oldIndices?.Dispose();
    }

    /// <summary>
    /// Resizes the array to accommodate more bits per value.
    /// </summary>
    /// <param name="newBitsPerValue">The new number of bits per value.</param>
    private void ResizeIndices(int newBitsPerValue)
    {
        var newIndices = new BitPackedArray<uint>(_indices.Count, newBitsPerValue);

        // Copy existing values
        for (var i = 0; i < _indices.Count; i++)
        {
            newIndices[i] = _indices[i];
        }

        // Replace the old array
        var oldIndices = _indices;
        _indices = newIndices;
        oldIndices.Dispose();
    }

    /// <summary>
    /// Calculates the number of bits required to represent the specified number of values.
    /// </summary>
    /// <param name="paletteSize">The size of the palette.</param>
    /// <returns>The number of bits required.</returns>
    private int CalculateRequiredBits(int paletteSize)
    {
        if (paletteSize <= 1)
            return _initialBits;

        var bits = 1;
        while ((1U << bits) < paletteSize)
        {
            bits++;
        }
        return Math.Max(bits, _initialBits);
    }

    /// <summary>
    /// Fills the array with the specified value.
    /// </summary>
    /// <param name="value">The value to fill the array with.</param>
    public void Fill(T value)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, typeof(PaletteArray<T>));

        if (_indices == null)
            return;

        if (!_valueToIndex.TryGetValue(value, out var paletteIndex))
        {
            // Add the new value to the palette
            paletteIndex = (uint)_palette.Count;
            _palette.Add(value);
            _valueToIndex[value] = paletteIndex;

            // Check if we need more bits
            var requiredBits = CalculateRequiredBits(_palette.Count);
            if (requiredBits > _indices.BitsPerValue)
                ResizeIndices(requiredBits);
        }

        // Fill the array with the palette index
        for (var i = 0; i < _indices.Count; i++)
            _indices[i] = paletteIndex;
    }

    /// <summary>
    /// Gets the palette index for a value.
    /// </summary>
    /// <param name="value">The value to get the index for.</param>
    /// <returns>The palette index for the value, or 0 if not found.</returns>
    public uint GetIndex(T value)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, typeof(PaletteArray<T>));

        if (_valueToIndex.TryGetValue(value, out var index))
            return index;

        return 0; // Default value index
    }

    /// <summary>
    /// Gets the value for a palette index.
    /// </summary>
    /// <param name="index">The palette index.</param>
    /// <returns>The value for the index, or the default value if out of range.</returns>
    public T GetValue(uint index)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, typeof(PaletteArray<T>));

        if (index < _palette.Count)
            return _palette[(int)index];

        return _defaultValue;
    }

    /// <summary>
    /// Gets all unique values in the array.
    /// </summary>
    /// <returns>An enumerable of unique values.</returns>
    public IEnumerable<T> GetUniqueValues()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, typeof(PaletteArray<T>));

        return _palette;
    }

    /// <summary>
    /// Copies the values from this array to a destination array.
    /// </summary>
    /// <param name="destinationArray">The destination array.</param>
    /// <param name="startIndex">The index in the destination array at which to start copying.</param>
    public void CopyTo(T[] destinationArray, int startIndex = 0)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, typeof(PaletteArray<T>));

        ArgumentNullException.ThrowIfNull(destinationArray);

        if (startIndex < 0)
            throw new ArgumentOutOfRangeException(
                nameof(startIndex),
                "Start index must be non-negative."
            );

        if (_indices == null)
            return;

        if (destinationArray.Length - startIndex < _indices.Count)
            throw new ArgumentException("Destination array is too small.");

        for (var i = 0; i < _indices.Count; i++)
        {
            var paletteIndex = _indices[i];
            destinationArray[startIndex + i] =
                paletteIndex < _palette.Count ? _palette[(int)paletteIndex] : _defaultValue;
        }
    }

    /// <summary>
    /// Returns all values in the array as an array.
    /// </summary>
    /// <returns>An array containing all values.</returns>
    public T[] ToArray()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, typeof(PaletteArray<T>));

        if (_indices == null)
            return Array.Empty<T>();

        var result = new T[_indices.Count];
        CopyTo(result);
        return result;
    }

    /// <summary>
    /// Gets an enumerator that iterates through the array.
    /// </summary>
    /// <returns>An enumerator for the array.</returns>
    public IEnumerator<T> GetEnumerator()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, typeof(PaletteArray<T>));

        if (_indices == null)
            yield break;

        for (var i = 0; i < _indices.Count; i++)
        {
            var paletteIndex = _indices[i];
            yield return paletteIndex < _palette.Count
                ? _palette[(int)paletteIndex]
                : _defaultValue;
        }
    }

    /// <summary>
    /// Gets an enumerator that iterates through the array.
    /// </summary>
    /// <returns>An enumerator for the array.</returns>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Gets the memory usage of this palette array in bytes.
    /// </summary>
    /// <returns>The memory usage in bytes.</returns>
    public int GetMemoryUsage()
    {
        if (_isDisposed || _indices == null)
            return 0;

        // Memory for BitPackedArray + palette entries + dictionary overhead (rough estimate)
        return _indices.EstimateMemoryUsage()
            + (_palette.Count * (typeof(T).IsValueType ? 16 : 32));
    }

    /// <summary>
    /// Disposes the array and releases any resources.
    /// </summary>
    public void Dispose()
    {
        if (!_isDisposed)
        {
            _indices?.Dispose();
            _indices = null;
            _palette.Clear();
            _valueToIndex.Clear();
            _isDisposed = true;
        }
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Finalizer for the array.
    /// </summary>
    ~PaletteArray()
    {
        Dispose();
    }
}
