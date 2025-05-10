using System;
using System.Threading;
using Godot;

namespace ITOC.Multithreading;

/// <summary>
/// Defines the priority levels for tasks in the multithreading system.
/// Higher priority tasks will be processed before lower priority ones.
/// </summary>
public enum TaskPriority
{
    /// <summary>
    /// Lowest priority. Used for background tasks that can wait indefinitely.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal priority. Default for most tasks.
    /// </summary>
    Normal = 1,

    /// <summary>
    /// High priority. Used for tasks that should be processed promptly.
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical priority. Used for urgent tasks that must be processed immediately.
    /// </summary>
    Critical = 3
}

/// <summary>
/// Defines the possible states of a task in the system.
/// </summary>
public enum TaskState
{
    /// <summary>
    /// Task has been created but not yet queued.
    /// </summary>
    Created,

    /// <summary>
    /// Task has been queued but not yet started.
    /// </summary>
    Queued,

    /// <summary>
    /// Task is currently being executed.
    /// </summary>
    Running,

    /// <summary>
    /// Task has completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Task has been cancelled before or during execution.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Task failed with an exception.
    /// </summary>
    Failed
}

/// <summary>
/// Base class for all tasks in the multithreading system.
/// Provides core functionality for task execution, cancellation, and state management.
/// </summary>
public abstract class GameTask
{
    #region Properties

    /// <summary>
    /// Gets the unique identifier for this task.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the name of this task.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the priority of this task.
    /// </summary>
    public TaskPriority Priority { get; }

    /// <summary>
    /// Gets the current state of this task.
    /// </summary>
    public TaskState State { get; internal set; }

    /// <summary>
    /// Gets whether this task has been cancelled.
    /// </summary>
    public bool IsCancelled => _cancellationTokenSource.IsCancellationRequested;

    /// <summary>
    /// Gets the cancellation token for this task.
    /// </summary>
    public CancellationToken CancellationToken => _cancellationTokenSource.Token;

    /// <summary>
    /// Gets the time when this task was created.
    /// </summary>
    public DateTime CreationTime { get; }

    /// <summary>
    /// Gets the time when this task started execution, or null if it hasn't started yet.
    /// </summary>
    public DateTime? StartTime { get; private set; }

    /// <summary>
    /// Gets the time when this task completed execution, or null if it hasn't completed yet.
    /// </summary>
    public DateTime? CompletionTime { get; private set; }

    /// <summary>
    /// Gets the elapsed time since this task was created.
    /// </summary>
    public TimeSpan ElapsedSinceCreation => DateTime.Now - CreationTime;

    /// <summary>
    /// Gets the elapsed time since this task started execution, or TimeSpan.Zero if it hasn't started yet.
    /// </summary>
    public TimeSpan ElapsedSinceStart => StartTime.HasValue ? DateTime.Now - StartTime.Value : TimeSpan.Zero;

    /// <summary>
    /// Gets the total execution time of this task, or null if it hasn't completed yet.
    /// </summary>
    public TimeSpan? ExecutionTime => (StartTime.HasValue && CompletionTime.HasValue) ?
        CompletionTime.Value - StartTime.Value : null;

    /// <summary>
    /// Gets the exception that caused this task to fail, or null if it hasn't failed.
    /// </summary>
    public Exception Exception { get; private set; }

    /// <summary>
    /// Gets or sets a user-defined tag for this task.
    /// </summary>
    public object Tag { get; set; }

    #endregion

    #region Events

    /// <summary>
    /// Event raised when this task starts execution.
    /// </summary>
    public event EventHandler Started;

    /// <summary>
    /// Event raised when this task completes execution.
    /// </summary>
    public event EventHandler<TaskCompletedEventArgs> Completed;

    /// <summary>
    /// Event raised when this task makes progress.
    /// </summary>
    public event EventHandler<TaskProgressEventArgs> ProgressChanged;

    #endregion

    #region Fields

    private readonly CancellationTokenSource _cancellationTokenSource;
    private int _progress;
    private string _statusMessage = string.Empty;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="GameTask"/> class.
    /// </summary>
    /// <param name="name">The name of the task.</param>
    /// <param name="priority">The priority of the task.</param>
    protected GameTask(string name = null, TaskPriority priority = TaskPriority.Normal)
    {
        Id = Guid.NewGuid();
        Name = name ?? $"Task-{Id}";
        Priority = priority;
        State = TaskState.Created;
        CreationTime = DateTime.Now;
        _cancellationTokenSource = new CancellationTokenSource();
    }

    #endregion

    #region Task Execution

    /// <summary>
    /// Executes this task. This method is called by the worker thread.
    /// </summary>
    internal void Execute()
    {
        if (State != TaskState.Queued || IsCancelled)
        {
            State = TaskState.Cancelled;
            OnCompleted(true);
            return;
        }

        StartTime = DateTime.Now;
        State = TaskState.Running;
        OnStarted();

        try
        {
            // Execute the actual task implementation
            ExecuteCore(CancellationToken);

            State = IsCancelled ? TaskState.Cancelled : TaskState.Completed;
            OnCompleted(!IsCancelled);
        }
        catch (OperationCanceledException)
        {
            State = TaskState.Cancelled;
            OnCompleted(true);
        }
        catch (Exception ex)
        {
            Exception = ex;
            State = TaskState.Failed;
            GD.PrintErr($"Task '{Name}' failed: {ex.Message}");
            OnCompleted(false);
        }
        finally
        {
            CompletionTime = DateTime.Now;
        }
    }

    /// <summary>
    /// Executes the core functionality of this task. This method must be implemented by derived classes.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    protected abstract void ExecuteCore(CancellationToken cancellationToken);

    #endregion

    #region Task Control

    /// <summary>
    /// Cancels this task.
    /// </summary>
    public void Cancel()
    {
        try
        {
            _cancellationTokenSource.Cancel();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error cancelling task '{Name}': {ex.Message}");
        }
    }

    /// <summary>
    /// Updates the progress of this task.
    /// </summary>
    /// <param name="progress">The progress value (0-100).</param>
    /// <param name="message">An optional status message.</param>
    protected void ReportProgress(int progress, string message = null)
    {
        progress = Math.Clamp(progress, 0, 100);
        _progress = progress;
        _statusMessage = message ?? _statusMessage;

        OnProgressChanged();
    }

    #endregion

    #region Event Handlers

    private void OnStarted()
    {
        Started?.Invoke(this, EventArgs.Empty);
    }

    private void OnCompleted(bool cancelled)
    {
        var args = new TaskCompletedEventArgs(
            cancelled,
            Exception,
            ExecutionTime ?? TimeSpan.Zero);

        Completed?.Invoke(this, args);
    }

    private void OnProgressChanged()
    {
        var args = new TaskProgressEventArgs(_progress, _statusMessage);
        ProgressChanged?.Invoke(this, args);
    }

    #endregion

    #region IDisposable Implementation

    /// <summary>
    /// Disposes this task, releasing all resources.
    /// </summary>
    public void Dispose()
    {
        _cancellationTokenSource?.Dispose();
    }

    #endregion
}

/// <summary>
/// Provides data for the <see cref="GameTask.Completed"/> event.
/// </summary>
public class TaskCompletedEventArgs : EventArgs
{
    /// <summary>
    /// Gets whether the task was cancelled.
    /// </summary>
    public bool Cancelled { get; }

    /// <summary>
    /// Gets the exception that caused the task to fail, or null if it didn't fail.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// Gets the total execution time of the task.
    /// </summary>
    public TimeSpan ExecutionTime { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskCompletedEventArgs"/> class.
    /// </summary>
    /// <param name="cancelled">Whether the task was cancelled.</param>
    /// <param name="exception">The exception that caused the task to fail, or null if it didn't fail.</param>
    /// <param name="executionTime">The total execution time of the task.</param>
    public TaskCompletedEventArgs(bool cancelled, Exception exception, TimeSpan executionTime)
    {
        Cancelled = cancelled;
        Exception = exception;
        ExecutionTime = executionTime;
    }
}

/// <summary>
/// Provides data for the <see cref="GameTask.ProgressChanged"/> event.
/// </summary>
public class TaskProgressEventArgs : EventArgs
{
    /// <summary>
    /// Gets the progress value (0-100).
    /// </summary>
    public int Progress { get; }

    /// <summary>
    /// Gets the status message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskProgressEventArgs"/> class.
    /// </summary>
    /// <param name="progress">The progress value (0-100).</param>
    /// <param name="message">The status message.</param>
    public TaskProgressEventArgs(int progress, string message)
    {
        Progress = progress;
        Message = message;
    }
}