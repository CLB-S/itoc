using System;
using System.Threading;

namespace ITOC.Multithreading;

/// <summary>
/// Represents a task that executes a simple action.
/// </summary>
public class ActionTask : GameTask
{
    private readonly Action _action;
    private readonly Action<CancellationToken> _cancellableAction;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActionTask"/> class.
    /// </summary>
    /// <param name="name">The name of the task.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="priority">The priority of the task.</param>
    public ActionTask(string name, Action action, TaskPriority priority = TaskPriority.Normal)
        : base(name, priority)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ActionTask"/> class.
    /// </summary>
    /// <param name="name">The name of the task.</param>
    /// <param name="action">The cancellable action to execute.</param>
    /// <param name="priority">The priority of the task.</param>
    public ActionTask(string name, Action<CancellationToken> action, TaskPriority priority = TaskPriority.Normal)
        : base(name, priority)
    {
        _cancellableAction = action ?? throw new ArgumentNullException(nameof(action));
    }

    /// <summary>
    /// Executes the core functionality of this task.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    protected override void ExecuteCore(CancellationToken cancellationToken)
    {
        if (_cancellableAction != null)
        {
            _cancellableAction(cancellationToken);
        }
        else if (_action != null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _action();
        }
    }
}
