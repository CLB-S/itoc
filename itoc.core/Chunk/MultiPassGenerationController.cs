using System.Collections.Concurrent;
using Godot;
using ITOC.Core.Multithreading;

namespace ITOC.Core;

public class MultiPassGenerationController
{
    public int PassCount { get; private set; }
    public int[] PassExpansions { get; private set; } // Pass 0 should always has 0 expansion

    public int MarkersLayerCount { get; private set; }

    /// <summary>
    /// Event triggered when a pass (greater than 0) is accessible.
    /// </summary>
    public event EventHandler<PassEventArgs> PassAccessible;
    public event EventHandler<PassEventArgs> PassFullyCompleted;
    public event EventHandler<Vector2I> AllPassesCompleted;

    private readonly IPass[] _passes;
    private readonly ConcurrentDictionary<Vector2I, int[]> _multiPassMarkers = new();

    // This may should be linked to something like a chunk manager.
    private readonly ConcurrentDictionary<Vector2I, bool> _executedPass0Positions = new();

    public MultiPassGenerationController(params IPass[] passes)
    {
        if (passes == null || passes.Length == 0)
            throw new ArgumentException("Passes cannot be null or empty");

        if (passes[0].Expansion != 0)
            throw new ArgumentException("First pass must have Expansion = 0");

        _passes = passes;

        // Initialize the number of passes and their expansions
        PassCount = passes.Length;
        PassExpansions = new int[PassCount];
        MarkersLayerCount = 0;

        for (var i = 0; i < PassCount; i++)
        {
            if (passes[i].Pass != i)
                throw new ArgumentException(
                    $"Passes must be in order. Pass {i} is not in the correct order."
                );

            PassExpansions[i] = passes[i].Expansion;
            if (PassExpansions[i] < 0)
                throw new ArgumentException("Pass expansion must be non-negative");
            if (PassExpansions[i] > 0)
                MarkersLayerCount += 2;

            passes[i].PassCompleted += (sender, args) =>
            {
                var expansion = PassExpansions[args.Pass];
                if (expansion == 0)
                {
                    PassFullyCompleted?.Invoke(this, args);
                }
                else
                {
                    for (var i = -expansion; i <= expansion; i++)
                    for (var j = -expansion; j <= expansion; j++)
                    {
                        var chunkIndex = args.ChunkColumnPos + new Vector2I(i, j);
                        IncreaseMultiPassCompletionMarker(chunkIndex, args.Pass);
                    }
                }
            };
        }

        PassFullyCompleted += (sender, args) =>
        {
            if (args.Pass == PassCount - 1)
            {
                _multiPassMarkers.TryRemove(args.ChunkColumnPos, out _);
                AllPassesCompleted?.Invoke(this, args.ChunkColumnPos);
                return;
            }

            var expansion = PassExpansions[args.Pass + 1];
            if (expansion == 0)
                PassAccessible?.Invoke(this, new PassEventArgs(args.Pass + 1, args.ChunkColumnPos));
            else
            {
                for (var i = -expansion; i <= expansion; i++)
                for (var j = -expansion; j <= expansion; j++)
                {
                    var chunkIndex = args.ChunkColumnPos + new Vector2I(i, j);
                    IncreaseMultiPassAccessibleMarker(chunkIndex, args.Pass + 1);
                }
            }
        };

        PassAccessible += (sender, args) => _passes[args.Pass].ExecuteAt(args.ChunkColumnPos);
    }

    public int GetTotalExpansionForCompletion()
    {
        var totalExpansion = 0;
        for (var i = 1; i < PassCount; i++)
            totalExpansion += 2 * PassExpansions[i];
        return totalExpansion;
    }

    private void IncreaseMultiPassAccessibleMarker(Vector2I chunkColumnPos, int pass)
    {
        var markers = _multiPassMarkers.GetOrAdd(chunkColumnPos, _ => new int[MarkersLayerCount]);

        var index = 0;
        for (var i = 0; i < pass; i++)
            index += PassExpansions[i] > 0 ? 2 : 0;

        // Use interlocked operations for thread-safe increments
        var currentValue = Interlocked.Increment(ref markers[index]);

        var expansion = 1 + 2 * PassExpansions[pass];
        if (currentValue == expansion * expansion)
            PassAccessible?.Invoke(this, new PassEventArgs(pass, chunkColumnPos));
    }

    private void IncreaseMultiPassCompletionMarker(Vector2I chunkColumnPos, int pass)
    {
        var markers = _multiPassMarkers.GetOrAdd(chunkColumnPos, _ => new int[MarkersLayerCount]);

        var index = -1;
        for (var i = 0; i <= pass; i++)
            index += PassExpansions[i] > 0 ? 2 : 0;

        // Use interlocked operations for thread-safe increments
        var currentValue = Interlocked.Increment(ref markers[index]);

        var expansion = 1 + 2 * PassExpansions[pass];
        if (currentValue == expansion * expansion)
            PassFullyCompleted?.Invoke(this, new PassEventArgs(pass, chunkColumnPos));
    }

    /// <summary>
    /// Generates all required passes for the given position.
    /// This will execute pass 0 at all necessary positions within the total expansion area.
    /// </summary>
    /// <param name="targetPosition">The position to fully generate</param>
    public void GenerateAt(Vector2I targetPosition)
    {
        var totalExpansion = GetTotalExpansionForCompletion();
        var positions = GetRequiredPass0Positions(targetPosition, totalExpansion);

        foreach (var position in positions)
            if (_executedPass0Positions.TryAdd(position, true))
                _passes[0].ExecuteAt(position);
    }

    /// <summary>
    /// Generates all required passes for multiple positions efficiently.
    /// </summary>
    /// <param name="targetPositions">The positions to fully generate</param>
    public void GenerateAt(IEnumerable<Vector2I> targetPositions)
    {
        var totalExpansion = GetTotalExpansionForCompletion();
        var allPositions = new HashSet<Vector2I>();

        foreach (var targetPosition in targetPositions)
        {
            var positions = GetRequiredPass0Positions(targetPosition, totalExpansion);
            foreach (var position in positions)
                allPositions.Add(position);
        }

        foreach (var position in allPositions)
            if (_executedPass0Positions.TryAdd(position, true))
                _passes[0].ExecuteAt(position);
    }

    /// <summary>
    /// Gets all positions where pass 0 needs to be executed to fully generate the target position.
    /// </summary>
    /// <param name="targetPosition">The target position to generate</param>
    /// <param name="totalExpansion">The total expansion required</param>
    /// <returns>Collection of positions where pass 0 should be executed</returns>
    private IEnumerable<Vector2I> GetRequiredPass0Positions(
        Vector2I targetPosition,
        int totalExpansion
    )
    {
        for (var i = -totalExpansion; i <= totalExpansion; i++)
        for (var j = -totalExpansion; j <= totalExpansion; j++)
            yield return targetPosition + new Vector2I(i, j);
    }
}

public interface IPass
{
    int Pass { get; }
    int Expansion { get; }

    GameTask ExecuteAt(Vector2I chunkColumnIndex)
    {
        var task = CreateTaskAt(chunkColumnIndex);
        TaskManager.Instance.EnqueueTask(task);
        return task;
    }

    GameTask CreateTaskAt(Vector2I chunkColumnIndex);

    /// <summary>
    /// Event triggered when the pass is completed successfully.
    /// </summary>
    event EventHandler<PassEventArgs> PassCompleted;
}

public class PassEventArgs : EventArgs
{
    public int Pass { get; }
    public Vector2I ChunkColumnPos { get; }

    public PassEventArgs(int pass, Vector2I chunkIndex)
    {
        Pass = pass;
        ChunkColumnPos = chunkIndex;
    }
}
