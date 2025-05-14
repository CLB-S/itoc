using System;
using System.Collections.Generic;
using Godot;

namespace ITOC.ChunkGeneration;

public class ChunkMultiPassGenerator
{
    public int PassCount = 2;
    public int[] PassExtends = [0, 1]; // Pass 0 always has 0 extend

    public int MarkersLayerCount { get; private set; }

    /// <summary>
    /// Event triggered when a pass (greater than 0) is accessible.
    /// </summary>
    public event EventHandler<PassEventArgs> PassAccessible;
    public event EventHandler<PassEventArgs> PassFullyCompleted;
    public event EventHandler<Vector2I> AllPassesCompleted;

    private readonly IPass[] _passes;
    private readonly Dictionary<Vector2I, int[]> _multiPassMarkers = new();
    private readonly object _lock = new object();

    public ChunkMultiPassGenerator(bool runNextPassAuto = true, params IPass[] passes)
    {
        if (passes == null || passes.Length == 0)
            throw new ArgumentException("Passes cannot be null or empty");

        _passes = passes;

        // Initialize the number of passes and their extents
        PassCount = passes.Length;
        PassExtends = new int[PassCount];
        MarkersLayerCount = 0;

        for (var i = 0; i < PassCount; i++)
        {
            if (passes[i].Pass != i)
                throw new ArgumentException($"Passes must be in order. Pass {i} is not in the correct order.");

            PassExtends[i] = passes[i].Extend;
            if (PassExtends[i] < 0)
                throw new ArgumentException("Pass extend must be non-negative");
            if (PassExtends[i] > 0)
                MarkersLayerCount += 2;

            passes[i].PassCompleted += (sender, args) =>
            {
                var extend = PassExtends[args.Pass];
                if (extend == 0)
                {
                    PassFullyCompleted?.Invoke(this, args);
                }
                else
                {
                    for (var i = -extend; i <= extend; i++)
                        for (var j = -extend; j <= extend; j++)
                        {
                            var chunkPos = args.ChunkColumnPos + new Vector2I(i, j);
                            IncreaseMultiPassCompletionMarker(chunkPos, args.Pass);
                        }
                }
            };
        }

        PassFullyCompleted += (sender, args) =>
        {
            if (args.Pass == PassCount - 1)
            {
                lock (_lock)
                    _multiPassMarkers.Remove(args.ChunkColumnPos, out _);

                AllPassesCompleted?.Invoke(this, args.ChunkColumnPos);
                return;
            }

            var extend = PassExtends[args.Pass + 1];

            lock (_lock)
            {
                for (var i = -extend; i <= extend; i++)
                    for (var j = -extend; j <= extend; j++)
                    {
                        var chunkPos = args.ChunkColumnPos + new Vector2I(i, j);
                        IncreaseMultiPassAccessibleMarker(chunkPos, args.Pass + 1);
                    }
            }
        };

        if (runNextPassAuto)
            PassAccessible += (sender, args) =>
                _passes[args.Pass].ExecuteAt(args.ChunkColumnPos);
    }

    private void IncreaseMultiPassAccessibleMarker(Vector2I chunkColumnPos, int pass)
    {
        lock (_lock)
        {
            if (!_multiPassMarkers.ContainsKey(chunkColumnPos))
                _multiPassMarkers[chunkColumnPos] = new int[MarkersLayerCount];
            var markers = _multiPassMarkers[chunkColumnPos];

            var index = 0;
            for (var i = 0; i < pass; i++)
                index += PassExtends[i] > 0 ? 2 : 0;

            markers[index]++;

            var extend = 1 + 2 * PassExtends[pass];
            if (markers[index] == extend * extend)
                PassAccessible?.Invoke(this, new PassEventArgs(pass, chunkColumnPos));
        }
    }

    private void IncreaseMultiPassCompletionMarker(Vector2I chunkColumnPos, int pass)
    {
        lock (_lock)
        {
            if (!_multiPassMarkers.ContainsKey(chunkColumnPos))
                _multiPassMarkers[chunkColumnPos] = new int[MarkersLayerCount];
            var markers = _multiPassMarkers[chunkColumnPos];

            var index = -1;
            for (var i = 0; i <= pass; i++)
                index += PassExtends[i] > 0 ? 2 : 0;

            markers[index]++;

            var extend = 1 + 2 * PassExtends[pass];
            if (markers[index] == extend * extend)
                PassFullyCompleted?.Invoke(this, new PassEventArgs(pass, chunkColumnPos));
        }
    }
}

public interface IPass
{
    int Pass { get; }
    int Extend { get; }

    void ExecuteAt(Vector2I chunkColumnPos);

    /// <summary>
    /// Event triggered when the pass is completed.
    /// </summary>
    event EventHandler<PassEventArgs> PassCompleted;
}

public class PassEventArgs : EventArgs
{
    public int Pass { get; }
    public Vector2I ChunkColumnPos { get; }

    public PassEventArgs(int pass, Vector2I chunkPos)
    {
        Pass = pass;
        ChunkColumnPos = chunkPos;
    }
}
