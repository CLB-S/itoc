using Godot;

namespace ITOC.Core;

public abstract class ChunkGeneratorBase
{
    protected readonly ChunkManager _chunkManager;

    private int _maxConcurrentChunkGenerationTasks = 1;
    private readonly Queue<Vector2I> _pendingGenerationQueue = new();
    private readonly HashSet<Vector2I> _activeGenerationTasks = new();
    private readonly Dictionary<Vector2I, Action<Vector2I>> _completionCallbacks = new();
    private readonly object _queueLock = new();

    /// <summary>
    /// The maximum number of concurrent chunk generation tasks that can be run at the same time.
    /// 0 means no limit.
    /// </summary>
    public int MaxConcurrentChunkGenerationTasks
    {
        get => _maxConcurrentChunkGenerationTasks;
        set
        {
            if (value < 0)
                throw new ArgumentException("Max concurrent tasks cannot be negative.");
            _maxConcurrentChunkGenerationTasks = value;
        }
    }

    public EventHandler<Vector2I> OnSurfaceChunksGenerated;

    public ChunkGeneratorBase(ChunkManager chunkManager)
    {
        _chunkManager = chunkManager ?? throw new ArgumentNullException(nameof(chunkManager));
        _chunkManager.LinkChunkGenerator(this);
    }

    /// <summary>
    /// All enqueued generation tasks will be processed in order. SO, don't enqueue too many tasks at once,
    /// </summary>
    public void EnqueueSurfaceChunksGeneration(Vector2I chunkColumnIndex, Action<Vector2I> onComplete = null)
    {
        lock (_queueLock)
        {
            // Don't queue if already active or pending
            if (_activeGenerationTasks.Contains(chunkColumnIndex) ||
                _pendingGenerationQueue.Contains(chunkColumnIndex))
                return;

            _pendingGenerationQueue.Enqueue(chunkColumnIndex);

            if (onComplete != null)
                _completionCallbacks[chunkColumnIndex] = onComplete;

            ProcessGenerationQueue();
        }
    }

    private void ProcessGenerationQueue()
    {
        while (_pendingGenerationQueue.Count > 0 &&
               (_maxConcurrentChunkGenerationTasks == 0 ||
                _activeGenerationTasks.Count < _maxConcurrentChunkGenerationTasks))
        {
            var chunkColumnIndex = _pendingGenerationQueue.Dequeue();

            if (_chunkManager.IsSurfaceChunksGeneratedAt(_pendingGenerationQueue.Peek()))
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

            GenerateSurfaceChunks(chunkColumnIndex);
        }
    }

    protected void NotifySurfaceChunksReady(Vector2I chunkColumnIndex)
    {
        lock (_queueLock)
        {
            _activeGenerationTasks.Remove(chunkColumnIndex);

            // Invoke completion callback if exists
            if (_completionCallbacks.TryGetValue(chunkColumnIndex, out var callback))
            {
                _completionCallbacks.Remove(chunkColumnIndex);
                callback?.Invoke(chunkColumnIndex);
            }

            OnSurfaceChunksGenerated?.Invoke(this, chunkColumnIndex);
            ProcessGenerationQueue();
        }
    }

    /// <summary>
    /// <see cref="NotifySurfaceChunksReady(Vector2I)"/> must be called when the surface chunks for the given column are ready.
    /// </summary>
    protected abstract void GenerateSurfaceChunks(Vector2I chunkColumnIndex);
}