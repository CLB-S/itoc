using System;
using System.Diagnostics;
using System.Threading;
using Godot;

namespace ITOC.Multithreading;

/// <summary>
/// Represents a worker thread in the task management system.
/// Each worker thread is responsible for executing tasks from the queue.
/// </summary>
internal class WorkerThread : IDisposable
{
    #region Fields

    private readonly TaskManager _taskManager;
    private readonly int _id;
    private readonly Thread _thread;
    private readonly CancellationTokenSource _cts;
    private readonly Stopwatch _idleTimer = new();
    private readonly bool _verboseLogging;

    private volatile bool _isDisposed;
    private volatile bool _isProcessingTask;
    private volatile bool _isActive;
    private GameTask _currentTask;

    #endregion

    #region Properties

    /// <summary>
    /// Gets the ID of this worker thread.
    /// </summary>
    public int Id => _id;

    /// <summary>
    /// Gets whether this worker thread is currently processing a task.
    /// </summary>
    public bool IsProcessingTask => _isProcessingTask;

    /// <summary>
    /// Gets whether this worker thread is active (started).
    /// </summary>
    public bool IsActive => _isActive;

    /// <summary>
    /// Gets the idle time of this worker thread in milliseconds.
    /// </summary>
    public long IdleTimeMs => _idleTimer.ElapsedMilliseconds;

    /// <summary>
    /// Gets the current task being processed by this worker thread, or null if none.
    /// </summary>
    public GameTask CurrentTask => _currentTask;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkerThread"/> class.
    /// </summary>
    /// <param name="taskManager">The task manager that owns this worker thread.</param>
    /// <param name="id">The ID of this worker thread.</param>
    /// <param name="verboseLogging">Whether to enable verbose logging for this worker thread.</param>
    public WorkerThread(TaskManager taskManager, int id, bool verboseLogging = false)
    {
        _taskManager = taskManager ?? throw new ArgumentNullException(nameof(taskManager));
        _id = id;
        _verboseLogging = verboseLogging;
        _cts = new CancellationTokenSource();
        _thread = new Thread(WorkerLoop)
        {
            Name = $"TaskWorker-{id}",
            IsBackground = true, // Make sure the thread doesn't prevent app shutdown
            Priority = ThreadPriority.Normal
        };

        _idleTimer.Start();
    }

    #endregion

    #region Thread Management

    /// <summary>
    /// Starts this worker thread.
    /// </summary>
    public void Start()
    {
        if (_isDisposed) return;

        if (!_isActive)
        {
            _isActive = true;
            _thread.Start();
            if (_verboseLogging)
                GD.Print($"Worker thread {_id} started.");
        }
    }

    /// <summary>
    /// Requests this worker thread to stop.
    /// </summary>
    public void RequestStop()
    {
        if (_isDisposed) return;

        _cts.Cancel();
    }

    /// <summary>
    /// Waits for this worker thread to exit.
    /// </summary>
    public void Join()
    {
        if (_isDisposed || !_isActive) return;

        if (_thread.IsAlive)
        {
            try
            {
                _thread.Join();
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Error joining worker thread {_id}: {ex.Message}");
            }
        }

        _isActive = false;
    }

    #endregion

    #region Worker Loop

    /// <summary>
    /// The main loop of the worker thread.
    /// </summary>
    private void WorkerLoop()
    {
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                _idleTimer.Reset();
                _idleTimer.Start();

                // Try to get the next task from the task manager
                if (_taskManager.TryDequeueNextTask(out _currentTask, _cts.Token))
                {
                    _idleTimer.Stop();
                    _isProcessingTask = true;

                    try
                    {
                        // Execute the task
                        _currentTask.Execute();

                        // Notify the task manager that the task has completed
                        _taskManager.NotifyTaskCompletion(_currentTask, _currentTask.State == TaskState.Completed);
                    }
                    catch (Exception ex)
                    {
                        GD.PrintErr($"Uncaught exception in worker thread {_id}: {ex.Message}");
                    }
                    finally
                    {
                        _isProcessingTask = false;
                        _currentTask = null;
                    }
                }
                else if (_cts.IsCancellationRequested)
                {
                    // Exit loop if cancellation was requested
                    break;
                }
                else
                {
                    // No tasks available, wait for a short period to avoid busy waiting
                    Thread.Sleep(10);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Worker thread was cancelled, exit gracefully
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Worker thread {_id} encountered an error: {ex.Message}");
        }
        finally
        {
            _isActive = false;
            _isProcessingTask = false;
            _idleTimer.Stop();
            if (_verboseLogging)
                GD.Print($"Worker thread {_id} stopped.");
        }
    }

    #endregion

    #region IDisposable Implementation

    /// <summary>
    /// Disposes this worker thread, releasing all resources.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed) return;

        _isDisposed = true;

        try
        {
            RequestStop();
            Join();
            _cts.Dispose();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error disposing worker thread {_id}: {ex.Message}");
        }
    }

    #endregion
}