using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ITOC.Multithreading;

/// <summary>
/// Represents a task that depends on one or more other tasks to complete before it can execute.
/// </summary>
public class DependentTask : GameTask
{
    private readonly Action _action;
    private readonly Action<CancellationToken> _cancellableAction;
    private readonly List<GameTask> _dependencies;

    /// <summary>
    /// Gets the dependencies of this task.
    /// </summary>
    public IReadOnlyList<GameTask> Dependencies => _dependencies;

    /// <summary>
    /// Gets whether all dependencies of this task have completed successfully.
    /// </summary>
    public bool AreDependenciesFulfilled => _dependencies.All(d => d.State == TaskState.Completed);

    /// <summary>
    /// Initializes a new instance of the <see cref="DependentTask"/> class.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <param name="name">The name of the task.</param>
    /// <param name="priority">The priority of the task.</param>
    /// <param name="dependencies">The tasks that must complete before this task can execute.</param>
    public DependentTask(Action action, string name = null, TaskPriority priority = TaskPriority.Normal, params GameTask[] dependencies)
        : base(name, priority)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
        _dependencies = new List<GameTask>(dependencies ?? Array.Empty<GameTask>());
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DependentTask"/> class.
    /// </summary>
    /// <param name="action">The cancellable action to execute.</param>
    /// <param name="name">The name of the task.</param>
    /// <param name="priority">The priority of the task.</param>
    /// <param name="dependencies">The tasks that must complete before this task can execute.</param>
    public DependentTask(Action<CancellationToken> action, string name = null, TaskPriority priority = TaskPriority.Normal, params GameTask[] dependencies)
        : base(name, priority)
    {
        _cancellableAction = action ?? throw new ArgumentNullException(nameof(action));
        _dependencies = new List<GameTask>(dependencies ?? Array.Empty<GameTask>());
    }

    /// <summary>
    /// Adds a dependency to this task.
    /// </summary>
    /// <param name="task">The task that must complete before this task can execute.</param>
    public void AddDependency(GameTask task)
    {
        if (task == null)
            throw new ArgumentNullException(nameof(task));

        if (State != TaskState.Created)
            throw new InvalidOperationException("Cannot add dependencies after the task has been queued.");

        if (task == this)
            throw new ArgumentException("A task cannot depend on itself.", nameof(task));

        _dependencies.Add(task);
    }

    /// <summary>
    /// Executes the core functionality of this task.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    protected override void ExecuteCore(CancellationToken cancellationToken)
    {
        // Check if all dependencies have completed
        if (!AreDependenciesFulfilled)
        {
            foreach (var dependency in _dependencies)
            {
                if (dependency.State == TaskState.Failed)
                {
                    throw new InvalidOperationException($"Dependency task '{dependency.Name}' has failed, cannot execute this task.");
                }
                else if (dependency.State == TaskState.Cancelled)
                {
                    throw new OperationCanceledException($"Dependency task '{dependency.Name}' was cancelled, cannot execute this task.");
                }
                else if (dependency.State != TaskState.Completed)
                {
                    throw new InvalidOperationException($"Dependency task '{dependency.Name}' is not completed, cannot execute this task.");
                }
            }
        }

        // Execute the action
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
