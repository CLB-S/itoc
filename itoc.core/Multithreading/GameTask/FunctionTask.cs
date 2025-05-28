namespace ITOC.Core.Multithreading;

/// <summary>
/// Represents a task that executes a function and returns a result.
/// </summary>
/// <typeparam name="T">The type of the result.</typeparam>
public class FunctionTask<T> : GameTask
{
    private readonly Func<T> _function;
    private readonly Func<CancellationToken, T> _cancellableFunction;
    private readonly Action<T> _callback;
    private T _result;

    /// <summary>
    /// Gets the result of the function.
    /// </summary>
    public T Result
    {
        get
        {
            if (State != TaskState.Completed)
                throw new InvalidOperationException("Cannot get result of a task that hasn't completed successfully.");
            return _result;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionTask{T}"/> class.
    /// </summary>
    /// <param name="name">The name of the task.</param>
    /// <param name="function">The function to execute.</param>
    /// <param name="priority">The priority of the task.</param>
    public FunctionTask(Func<T> function, Action<T> callback, string name = null, TaskPriority priority = TaskPriority.Normal)
        : base(name, priority)
    {
        _function = function ?? throw new ArgumentNullException(nameof(function));
        _callback = callback;
        Completed += (s, e) =>
        {
            if ((s as GameTask).State == TaskState.Completed)
                _callback?.Invoke(Result);
        };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionTask{T}"/> class.
    /// </summary>
    /// <param name="name">The name of the task.</param>
    /// <param name="function">The cancellable function to execute.</param>
    /// <param name="priority">The priority of the task.</param>
    public FunctionTask(Func<CancellationToken, T> function, Action<T> callback, string name = null, TaskPriority priority = TaskPriority.Normal)
        : base(name, priority)
    {
        _cancellableFunction = function ?? throw new ArgumentNullException(nameof(function));
        _callback = callback;
        Completed += (s, e) =>
        {
            if ((s as GameTask).State == TaskState.Completed)
                _callback?.Invoke(Result);
        };
    }

    /// <summary>
    /// Executes the core functionality of this task.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    protected override void ExecuteCore(CancellationToken cancellationToken)
    {
        if (_cancellableFunction != null)
        {
            _result = _cancellableFunction(cancellationToken);
        }
        else if (_function != null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _result = _function();
        }
    }
}
