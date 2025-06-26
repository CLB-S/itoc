using Godot;

namespace ITOC.Core.Multithreading;

/// <summary>
/// Represents a task that coordinates and manages a collection of sub-tasks with dependencies.
/// Unlike the BatchTask which executes tasks sequentially, CompositeTask handles a directed acyclic graph of tasks.
/// </summary>
public class CompositeTask : GameTask
{
    private readonly List<GameTask> _subTasks = new();
    private readonly Dictionary<Guid, List<Guid>> _dependencies = new();
    private readonly Dictionary<Guid, int> _remainingDependencies = new();
    private readonly Dictionary<Guid, GameTask> _taskLookup = new();
    private readonly object _syncLock = new();

    private int _completedTasks;
    private int _totalTasks;
    private volatile bool _isBuilt;

    /// <summary>
    /// Gets a read-only collection of all sub-tasks in this composite task.
    /// </summary>
    public IReadOnlyList<GameTask> SubTasks => _subTasks;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeTask"/> class.
    /// </summary>
    /// <param name="name">The name of the task.</param>
    /// <param name="priority">The priority of the task.</param>
    public CompositeTask(string name = null, TaskPriority priority = TaskPriority.Normal)
        : base(name, priority) { }

    /// <summary>
    /// Adds a task to be executed as part of this composite task.
    /// </summary>
    /// <param name="task">The task to add.</param>
    /// <returns>This instance for method chaining.</returns>
    public CompositeTask AddTask(GameTask task)
    {
        if (_isBuilt)
            throw new InvalidOperationException(
                "Cannot modify task structure after build has been called."
            );

        ArgumentNullException.ThrowIfNull(task);

        if (task == this)
            throw new ArgumentException("A task cannot contain itself.", nameof(task));

        lock (_syncLock)
        {
            _subTasks.Add(task);
            _taskLookup[task.Id] = task;
            _remainingDependencies[task.Id] = 0;
        }

        return this;
    }

    /// <summary>
    /// Adds a dependency between two tasks, indicating that the dependent task
    /// should only be executed after the prerequisite task has completed.
    /// </summary>
    /// <param name="prerequisiteTask">The task that must complete first.</param>
    /// <param name="dependentTask">The task that depends on the prerequisite task.</param>
    /// <returns>This instance for method chaining.</returns>
    public CompositeTask AddDependency(GameTask prerequisiteTask, GameTask dependentTask)
    {
        if (_isBuilt)
            throw new InvalidOperationException(
                "Cannot modify task structure after build has been called."
            );

        ArgumentNullException.ThrowIfNull(prerequisiteTask);
        ArgumentNullException.ThrowIfNull(dependentTask);

        if (prerequisiteTask == dependentTask)
            throw new ArgumentException("A task cannot depend on itself.");

        lock (_syncLock)
        {
            // Ensure both tasks are in our task list
            if (!_taskLookup.ContainsKey(prerequisiteTask.Id))
                AddTask(prerequisiteTask);

            if (!_taskLookup.ContainsKey(dependentTask.Id))
                AddTask(dependentTask);

            // Add the dependency
            if (!_dependencies.TryGetValue(prerequisiteTask.Id, out var dependents))
            {
                dependents = new List<Guid>();
                _dependencies[prerequisiteTask.Id] = dependents;
            }

            if (!dependents.Contains(dependentTask.Id))
            {
                dependents.Add(dependentTask.Id);
                _remainingDependencies[dependentTask.Id]++;
            }
        }

        return this;
    }

    /// <summary>
    /// Finalizes the task structure and prepares it for execution.
    /// This method must be called before the task is enqueued.
    /// </summary>
    /// <returns>This instance for method chaining.</returns>
    public CompositeTask Build()
    {
        if (_isBuilt)
            return this;

        lock (_syncLock)
        {
            DetectCycles();
            _totalTasks = _subTasks.Count;
            _isBuilt = true;
        }

        return this;
    }

    /// <summary>
    /// Executes the core functionality of this task.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    protected override void ExecuteCore(CancellationToken cancellationToken)
    {
        if (!_isBuilt)
            Build();

        _completedTasks = 0;

        // Create a local working copy of the dependency counts
        var remainingDependencies = new Dictionary<Guid, int>(_remainingDependencies);

        // Track tasks that are ready to execute (no dependencies)
        var readyTasks = new Queue<GameTask>();

        // Initialize with tasks that have no dependencies
        foreach (var taskId in _remainingDependencies.Keys)
        {
            if (_remainingDependencies[taskId] == 0)
            {
                readyTasks.Enqueue(_taskLookup[taskId]);
            }
        }

        // Execute tasks in dependency order
        while (readyTasks.Count > 0 && !cancellationToken.IsCancellationRequested)
        {
            var currentTask = readyTasks.Dequeue();

            try
            {
                // Update progress
                ReportProgress(
                    (_completedTasks * 100) / _totalTasks,
                    $"Executing sub-task {_completedTasks + 1} of {_totalTasks}: {currentTask.Name}"
                );

                // Reset the task state if needed
                if (currentTask.State != TaskState.Created)
                {
                    currentTask.State = TaskState.Created;
                }

                // Simulate the task being queued and execute it
                currentTask.State = TaskState.Queued;
                currentTask.Execute();

                // Skip to next task if it failed or was cancelled
                if (currentTask.State != TaskState.Completed)
                {
                    // Special handling for failed tasks
                    if (currentTask.State == TaskState.Failed)
                    {
                        throw new InvalidOperationException(
                            $"Sub-task '{currentTask.Name}' failed: {currentTask.Exception?.Message} \n {currentTask.Exception?.StackTrace}"
                        );
                    }
                    continue;
                }

                // Task completed successfully, update task completion count
                _completedTasks++;

                // Check for dependent tasks that might now be ready
                if (_dependencies.TryGetValue(currentTask.Id, out var dependentIds))
                {
                    foreach (var dependentId in dependentIds)
                    {
                        if (--remainingDependencies[dependentId] == 0)
                        {
                            // This dependent task is now ready to execute
                            readyTasks.Enqueue(_taskLookup[dependentId]);
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                GD.PrintErr($"Error executing sub-task '{currentTask.Name}': {ex.Message}");
                throw;
            }
        }

        // Check if we were cancelled
        cancellationToken.ThrowIfCancellationRequested();

        // Check if all tasks completed
        if (_completedTasks < _totalTasks)
        {
            throw new InvalidOperationException(
                $"Not all sub-tasks completed. Expected {_totalTasks}, but only {_completedTasks} finished."
            );
        }

        ReportProgress(100, "All sub-tasks completed successfully");
    }

    /// <summary>
    /// Detects cycles in the dependency graph and throws an exception if any are found.
    /// </summary>
    private void DetectCycles()
    {
        var visited = new HashSet<Guid>();
        var recursionStack = new HashSet<Guid>();

        foreach (var task in _subTasks)
        {
            if (IsCyclicUtil(task.Id, visited, recursionStack))
            {
                throw new InvalidOperationException("Cyclic dependency detected in task graph.");
            }
        }
    }

    private bool IsCyclicUtil(Guid taskId, HashSet<Guid> visited, HashSet<Guid> recursionStack)
    {
        // Mark the current node as visited and part of recursion stack
        if (!visited.Contains(taskId))
        {
            visited.Add(taskId);
            recursionStack.Add(taskId);

            // Recur for all the dependencies
            if (_dependencies.TryGetValue(taskId, out var dependents))
            {
                foreach (var dependentId in dependents)
                {
                    if (
                        !visited.Contains(dependentId)
                        && IsCyclicUtil(dependentId, visited, recursionStack)
                    )
                    {
                        return true;
                    }
                    else if (recursionStack.Contains(dependentId))
                    {
                        return true;
                    }
                }
            }
        }

        // Remove the task from recursion stack
        recursionStack.Remove(taskId);
        return false;
    }
}
