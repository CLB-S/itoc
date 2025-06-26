using Godot;

namespace ITOC.Core;

public abstract class ChunkGeneratorBase
{
    public ChunkManager ChunkManager { get; private set; }

    private int _maxConcurrentChunkGenerationTasks = 1;
    private readonly Queue<Vector2I> _pendingGenerationQueue = new();
    private readonly HashSet<Vector2I> _activeGenerationTasks = new();
    private readonly Dictionary<Vector2I, Action<Vector2I>> _completionCallbacks = new();
    private readonly object _lock = new object();

    /// <summary>
    /// The maximum number of concurrent chunk generation tasks that can be run at the same time.
    /// 0 means no limit.
    /// </summary>
    public int MaxConcurrentChunkGenerationTasks
    {
        get
        {
            lock (_lock)
            {
                return _maxConcurrentChunkGenerationTasks;
            }
        }
        set
        {
            if (value < 0)
                throw new ArgumentException("Max concurrent tasks cannot be negative.");

            lock (_lock)
            {
                _maxConcurrentChunkGenerationTasks = value;
            }
        }
    }

    public EventHandler<Vector2I> OnSurfaceChunksGenerated;

    public ChunkGeneratorBase()
    {
        ChunkManager = new ChunkManager();
        ChunkManager.LinkChunkGenerator(this);
    }

    /// <summary>
    /// All enqueued generation tasks will be processed in order. SO, don't enqueue too many tasks at once,
    /// </summary>
    public void EnqueueSurfaceChunksGeneration(
        Vector2I chunkColumnIndex,
        Action<Vector2I> onComplete = null
    )
    {
        lock (_lock)
        {
            // Don't queue if already active or pending
            if (
                _activeGenerationTasks.Contains(chunkColumnIndex)
                || _pendingGenerationQueue.Contains(chunkColumnIndex)
            )
                return;

            // Don't queue if already generated
            if (ChunkManager.IsSurfaceChunksGeneratedAt(chunkColumnIndex))
            {
                onComplete?.Invoke(chunkColumnIndex);
                return;
            }

            _pendingGenerationQueue.Enqueue(chunkColumnIndex);
            // GD.Print($"Enqueued surface chunk generation for {chunkColumnIndex}");

            if (onComplete != null)
                _completionCallbacks[chunkColumnIndex] = onComplete;
        }

        ProcessGenerationQueue();
    }

    private void ProcessGenerationQueue()
    {
        while (true)
        {
            Vector2I chunkColumnIndex;

            lock (_lock)
            {
                if (
                    _pendingGenerationQueue.Count == 0
                    || (
                        _maxConcurrentChunkGenerationTasks > 0
                        && _activeGenerationTasks.Count >= _maxConcurrentChunkGenerationTasks
                    )
                )
                    break;

                chunkColumnIndex = _pendingGenerationQueue.Dequeue();

                // Double-check if already generated (could have been generated while in queue)
                if (ChunkManager.IsSurfaceChunksGeneratedAt(chunkColumnIndex))
                {
                    // Invoke completion callback if exists
                    if (_completionCallbacks.TryGetValue(chunkColumnIndex, out var callback))
                    {
                        _completionCallbacks.Remove(chunkColumnIndex);
                        callback?.Invoke(chunkColumnIndex);
                    }

                    continue;
                }

                _activeGenerationTasks.Add(chunkColumnIndex);
            }

            GD.Print($"Processing surface chunk generation for {chunkColumnIndex}");
            GenerateSurfaceChunks(chunkColumnIndex);
        }
    }

    protected void NotifySurfaceChunksReady(Vector2I chunkColumnIndex)
    {
        GD.Print($"Surface chunks ready for {chunkColumnIndex}");

        Action<Vector2I> callback = null;

        lock (_lock)
        {
            if (!_activeGenerationTasks.Remove(chunkColumnIndex))
                GD.PrintErr(
                    $"Warning: Chunk {chunkColumnIndex} was not in active tasks when notifying ready"
                );

            // Get completion callback if exists
            if (_completionCallbacks.TryGetValue(chunkColumnIndex, out callback))
                _completionCallbacks.Remove(chunkColumnIndex);
        }

        // Invoke callback outside of lock to prevent potential deadlocks
        callback?.Invoke(chunkColumnIndex);
        OnSurfaceChunksGenerated?.Invoke(this, chunkColumnIndex);

        ProcessGenerationQueue();
    }

    /// <summary>
    /// <see cref="NotifySurfaceChunksReady(Vector2I)"/> must be called when the surface chunks for the given column are ready.
    /// </summary>
    protected abstract void GenerateSurfaceChunks(Vector2I chunkColumnIndex);
}
