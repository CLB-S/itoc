using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Godot;

namespace ITOC.Multithreading;

/// <summary>
/// A thread-safe, high-performance task management system for handling multithreaded operations in a game environment.
/// This class manages worker threads and task scheduling with support for priorities, cancellation, and pausing.
/// </summary>
public class TaskManager : IDisposable
{
    #region Singleton

    private static TaskManager _instance;
    private static readonly object _instanceLock = new();

    public static TaskManager Instance
    {
        get
        {
            if (_instance == null)
                lock (_instanceLock)
                    _instance ??= new TaskManager();
            return _instance;
        }
    }

    #endregion

    #region Fields

    // Configuration
    private TaskManagerConfig _config;
    private int _maxWorkerThreads;
    private bool _autoStart;

    // Worker threads
    private readonly List<WorkerThread> _workers = new();

    // Task queues - one per priority level
    private readonly ConcurrentDictionary<TaskPriority, ConcurrentQueue<GameTask>> _taskQueues = new();

    // Task counters for statistics
    private long _totalTasksCreated;
    private long _totalTasksCompleted;
    private long _totalTasksCancelled;
    private long _totalTasksFailed;

    // Synchronization objects
    private readonly ManualResetEventSlim _newTaskEvent = new(false);
    private readonly SemaphoreSlim _pauseSemaphore = new(1, 1);

    // State management
    private volatile bool _isDisposed;
    private volatile bool _isPaused;
    private volatile bool _isShuttingDown;

    // Diagnostics
    private readonly Stopwatch _uptime = new();

    // Task dependency tracking
    private readonly ConcurrentDictionary<Guid, List<DependentTask>> _dependentTasks = new();

    #endregion

    #region Properties

    /// <summary>
    /// Gets the number of active worker threads.
    /// </summary>
    public int WorkerCount => _workers.Count;

    /// <summary>
    /// Gets the total number of pending tasks across all priorities.
    /// </summary>
    public int PendingTaskCount => _taskQueues.Values.Sum(q => q.Count);

    /// <summary>
    /// Gets the number of tasks currently being processed.
    /// </summary>
    public int ActiveTaskCount => _workers.Count(w => w.IsProcessingTask);

    /// <summary>
    /// Gets the total number of tasks that have been created.
    /// </summary>
    public long TotalTasksCreated => Interlocked.Read(ref _totalTasksCreated);

    /// <summary>
    /// Gets the total number of tasks that have been completed successfully.
    /// </summary>
    public long TotalTasksCompleted => Interlocked.Read(ref _totalTasksCompleted);

    /// <summary>
    /// Gets the total number of tasks that have been cancelled.
    /// </summary>
    public long TotalTasksCancelled => Interlocked.Read(ref _totalTasksCancelled);

    /// <summary>
    /// Gets the total number of tasks that have failed with exceptions.
    /// </summary>
    public long TotalTasksFailed => Interlocked.Read(ref _totalTasksFailed);

    /// <summary>
    /// Gets the uptime of the task manager in milliseconds.
    /// </summary>
    public long UptimeMs => _uptime.ElapsedMilliseconds;

    /// <summary>
    /// Gets or sets whether the TaskManager is paused. When paused, tasks won't be processed
    /// but can still be queued.
    /// </summary>
    public bool IsPaused
    {
        get => _isPaused;
        set
        {
            if (_isPaused == value) return;

            if (value)
            {
                _pauseSemaphore.Wait();
                _isPaused = true;
            }
            else
            {
                _isPaused = false;
                _pauseSemaphore.Release();
                _newTaskEvent.Set(); // Wake up workers
            }
        }
    }

    /// <summary>
    /// Gets whether the TaskManager is currently shutting down.
    /// </summary>
    public bool IsShuttingDown => _isShuttingDown;

    /// <summary>
    /// Gets the current configuration of the TaskManager.
    /// </summary>
    public TaskManagerConfig Config => _config;

    #endregion

    #region Constructors and Initialization

    private TaskManager()
    {
        // Initialize queues for each priority level
        foreach (TaskPriority priority in Enum.GetValues(typeof(TaskPriority)))
            _taskQueues[priority] = new ConcurrentQueue<GameTask>();
    }

    /// <summary>
    /// Initializes the TaskManager with the specified configuration.
    /// </summary>
    /// <param name="config">The TaskManagerConfig object with configuration settings</param>
    public void Initialize(TaskManagerConfig config)
    {
        if (_workers.Count > 0)
        {
            GD.PrintErr("TaskManager is already initialized.");
            return;
        }

        _config = config ?? new TaskManagerConfig();

        _maxWorkerThreads = _config.MaxWorkerThreads > 0
            ? _config.MaxWorkerThreads
            : Math.Max(1, System.Environment.ProcessorCount - 1);

        _autoStart = _config.AutoStart;

        CreateWorkerThreads();

        if (_autoStart)
            Start();

        GD.Print($"TaskManager initialized with {_maxWorkerThreads} worker threads.");
    }

    /// <summary>
    /// Initializes the TaskManager with the specified configuration.
    /// </summary>
    /// <param name="maxWorkerThreads">Maximum number of worker threads to use. If 0 or negative, will use Environment.ProcessorCount - 1.</param>
    /// <param name="autoStart">Whether to automatically start the worker threads after initialization.</param>
    public void Initialize(int maxWorkerThreads = 0, bool autoStart = true)
    {
        var config = new TaskManagerConfig
        {
            MaxWorkerThreads = maxWorkerThreads,
            AutoStart = autoStart
        };

        Initialize(config);
    }

    private void CreateWorkerThreads()
    {
        for (int i = 0; i < _maxWorkerThreads; i++)
        {
            var worker = new WorkerThread(this, i, _config.VerboseLogging);
            _workers.Add(worker);
        }
    }

    #endregion

    #region Task Management

    /// <summary>
    /// Enqueues a task for execution with the specified priority.
    /// </summary>
    /// <param name="task">The task to enqueue.</param>
    /// <returns>Returns the enqueued task for chaining or tracking.</returns>
    public GameTask EnqueueTask(GameTask task)
    {
        ThrowIfDisposed();

        if (_isShuttingDown)
        {
            task.Cancel();
            return task;
        }

        if (task.State == TaskState.Created)
        {
            // Handle dependent tasks differently
            if (task is DependentTask dependentTask)
                return EnqueueDependentTask(dependentTask);

            task.State = TaskState.Queued;
            _taskQueues[task.Priority].Enqueue(task);
            Interlocked.Increment(ref _totalTasksCreated);

            // Signal that a new task is available
            _newTaskEvent.Set();

            if (_config.VerboseLogging)
                GD.Print($"Task '{task.Name}' enqueued with priority {task.Priority}");
        }
        else
        {
            GD.PrintErr($"Cannot enqueue task '{task.Name}' with state {task.State}");
        }

        return task;
    }

    /// <summary>
    /// Enqueues a task with dependencies. The task will only be executed when all dependencies have completed.
    /// </summary>
    /// <param name="task">The dependent task to enqueue.</param>
    /// <returns>Returns the enqueued task for chaining or tracking.</returns>
    private GameTask EnqueueDependentTask(DependentTask task)
    {
        if (task == null) throw new ArgumentNullException(nameof(task));

        // Check if all dependencies are already completed
        if (task.AreDependenciesFulfilled)
        {
            // All dependencies are fulfilled, enqueue as normal
            task.State = TaskState.Queued;
            _taskQueues[task.Priority].Enqueue(task);
            Interlocked.Increment(ref _totalTasksCreated);
            _newTaskEvent.Set();

            if (_config.VerboseLogging)
                GD.Print($"Dependent task '{task.Name}' enqueued with {task.Dependencies.Count} already completed dependencies.");

            return task;
        }

        // Store the task in waiting state (still considered as "Queued")
        task.State = TaskState.Queued;
        Interlocked.Increment(ref _totalTasksCreated);

        // For each dependency, register this task as dependent
        foreach (var dependency in task.Dependencies)
        {
            if (dependency.State != TaskState.Completed)
            {
                // Ensure the dependency is at least queued
                if (dependency.State == TaskState.Created)
                    EnqueueTask(dependency);

                // Register this dependent task for the dependency
                _dependentTasks.AddOrUpdate(
                    dependency.Id,
                    [task],
                    (_, list) =>
                    {
                        lock (list)
                        {
                            if (!list.Contains(task))
                                list.Add(task);
                            return list;
                        }
                    });
            }
        }

        if (_config.VerboseLogging)
            GD.Print($"Dependent task '{task.Name}' registered with {task.Dependencies.Count} dependencies.");

        return task;
    }

    /// <summary>
    /// Attempts to dequeue the next task to be processed, considering priorities.
    /// </summary>
    /// <param name="task">The dequeued task, or null if no tasks are available.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>True if a task was dequeued, false otherwise.</returns>
    internal bool TryDequeueNextTask(out GameTask task, CancellationToken cancellationToken)
    {
        task = null;

        if (_isPaused || _isShuttingDown)
            return false;

        // Try to get a task from the highest priority queue first
        foreach (TaskPriority priority in Enum.GetValues(typeof(TaskPriority)).Cast<TaskPriority>().OrderByDescending(p => p))
        {
            if (_taskQueues.TryGetValue(priority, out var queue) && queue.TryDequeue(out task))
                return true;
        }

        // Reset the event since we've processed all available tasks
        _newTaskEvent.Reset();

        // Wait for new tasks to arrive
        try
        {
            // Wait until a new task is available or cancellation is requested
            _newTaskEvent.Wait(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Worker is being shut down
            return false;
        }

        // Recursively try again after being signaled
        return TryDequeueNextTask(out task, cancellationToken);
    }

    /// <summary>
    /// Notifies the TaskManager that a task has completed.
    /// </summary>
    /// <param name="task">The completed task.</param>
    /// <param name="successful">Whether the task completed successfully.</param>
    internal void NotifyTaskCompletion(GameTask task, bool successful)
    {
        if (successful)
        {
            Interlocked.Increment(ref _totalTasksCompleted);

            // Check if there are any dependent tasks waiting on this task
            CheckAndEnqueueDependentTasks(task);
        }
        else if (task.IsCancelled)
            Interlocked.Increment(ref _totalTasksCancelled);
        else
            Interlocked.Increment(ref _totalTasksFailed);
    }

    /// <summary>
    /// Checks if any dependent tasks are waiting for the completed task and enqueues them if all their dependencies are fulfilled.
    /// </summary>
    /// <param name="completedTask">The task that just completed.</param>
    private void CheckAndEnqueueDependentTasks(GameTask completedTask)
    {
        if (!_dependentTasks.TryRemove(completedTask.Id, out var dependentTaskList))
            return;

        foreach (var dependentTask in dependentTaskList)
        {
            // Check if all dependencies are now fulfilled
            if (dependentTask.AreDependenciesFulfilled)
            {
                // All dependencies are fulfilled, enqueue the task
                _taskQueues[dependentTask.Priority].Enqueue(dependentTask);
                _newTaskEvent.Set();

                if (_config.VerboseLogging)
                    GD.Print($"Dependent task '{dependentTask.Name}' is now ready for execution as all dependencies have completed.");
            }
        }
    }

    /// <summary>
    /// Cancels all pending tasks and optionally waits for active tasks to complete.
    /// </summary>
    /// <param name="waitForActiveTasks">Whether to wait for currently executing tasks to complete.</param>
    public void CancelAllTasks(bool waitForActiveTasks = false)
    {
        ThrowIfDisposed();

        // Cancel all pending tasks
        foreach (var queue in _taskQueues.Values)
        {
            while (queue.TryDequeue(out var task))
            {
                task.Cancel();
                NotifyTaskCompletion(task, false);
            }
        }

        if (waitForActiveTasks)
        {
            // Wait for all workers to finish their current tasks
            while (_workers.Any(w => w.IsProcessingTask))
            {
                Thread.Sleep(10);
            }
        }

        if (_config.VerboseLogging)
            GD.Print("All pending tasks cancelled.");
    }

    #endregion

    #region Lifecycle Management

    /// <summary>
    /// Starts the TaskManager if it is not already running.
    /// </summary>
    public void Start()
    {
        ThrowIfDisposed();

        if (_isShuttingDown)
        {
            GD.PrintErr("Cannot start TaskManager while it is shutting down.");
            return;
        }

        foreach (var worker in _workers)
            worker.Start();

        IsPaused = false;
        _uptime.Start();

        if (_config.VerboseLogging)
            GD.Print("TaskManager started.");
    }

    /// <summary>
    /// Pauses task processing. Tasks can still be enqueued but won't be processed until resumed.
    /// </summary>
    public void Pause()
    {
        ThrowIfDisposed();
        IsPaused = true;

        if (_config.VerboseLogging)
            GD.Print("TaskManager paused.");
    }

    /// <summary>
    /// Resumes task processing if the TaskManager is paused.
    /// </summary>
    public void Resume()
    {
        ThrowIfDisposed();
        IsPaused = false;

        if (_config.VerboseLogging)
            GD.Print("TaskManager resumed.");
    }

    /// <summary>
    /// Initiates a graceful shutdown of the TaskManager.
    /// </summary>
    /// <param name="cancelPendingTasks">Whether to cancel all pending tasks.</param>
    /// <param name="waitForWorkers">Whether to wait for worker threads to exit.</param>
    public void Shutdown(bool cancelPendingTasks = true, bool waitForWorkers = true)
    {
        if (_isDisposed || _isShuttingDown) return;

        _isShuttingDown = true;
        _uptime.Stop();

        if (cancelPendingTasks)
            CancelAllTasks(false);

        foreach (var worker in _workers)
            worker.RequestStop();

        if (waitForWorkers)
        {
            foreach (var worker in _workers)
                worker.Join();
        }

        if (_config.VerboseLogging)
            GD.Print("TaskManager shutdown completed.");
    }

    /// <summary>
    /// Disposes the TaskManager, releasing all resources.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed) return;

        try
        {
            Shutdown(true, true);

            _newTaskEvent.Dispose();
            _pauseSemaphore.Dispose();

            foreach (var worker in _workers)
                worker.Dispose();

            _workers.Clear();

            foreach (var queue in _taskQueues.Values)
                while (queue.TryDequeue(out _)) { }

            _taskQueues.Clear();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error during TaskManager disposal: {ex.Message}");
        }
        finally
        {
            _isDisposed = true;

            // Remove the singleton instance
            lock (_instanceLock)
            {
                if (_instance == this)
                {
                    _instance = null;
                }
            }
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, nameof(TaskManager));
    }

    #endregion

    #region Diagnostics

    /// <summary>
    /// Gets the current status of the TaskManager including statistics.
    /// </summary>
    /// <returns>A string containing TaskManager diagnostics.</returns>
    public string GetStatus()
    {
        return $"TaskManager Status:\n" +
               $"  Uptime: {TimeSpan.FromMilliseconds(_uptime.ElapsedMilliseconds)}\n" +
               $"  Workers: {WorkerCount} (Active: {_workers.Count(w => w.IsActive)})\n" +
               $"  Tasks Pending: {PendingTaskCount}\n" +
               $"  Tasks Active: {ActiveTaskCount}\n" +
               $"  Tasks Completed: {TotalTasksCompleted}\n" +
               $"  Tasks Cancelled: {TotalTasksCancelled}\n" +
               $"  Tasks Failed: {TotalTasksFailed}\n" +
               $"  Tasks Total: {TotalTasksCreated}\n" +
               $"  Configuration: {(_config.VerboseLogging ? "Verbose" : "Normal")}, " +
               $"Worker Threads: {_maxWorkerThreads}, " +
               $"  State: {(_isPaused ? "Paused" : _isShuttingDown ? "Shutting Down" : "Running")}";
    }

    #endregion
}
