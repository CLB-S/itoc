using System;
using System.Threading;

namespace ITOC.Multithreading;

/// <summary>
/// Represents a task that executes multiple tasks in sequence.
/// </summary>
public class BatchTask : GameTask
{
    private readonly GameTask[] _tasks;

    /// <summary>
    /// Gets the tasks in this batch.
    /// </summary>
    public GameTask[] Tasks => _tasks;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchTask"/> class.
    /// </summary>
    /// <param name="name">The name of the task.</param>
    /// <param name="priority">The priority of the task.</param>
    /// <param name="tasks">The tasks to execute in sequence.</param>
    public BatchTask(string name, TaskPriority priority = TaskPriority.Normal, params GameTask[] tasks)
        : base(name, priority)
    {
        _tasks = tasks ?? throw new ArgumentNullException(nameof(tasks));
    }

    /// <summary>
    /// Executes the core functionality of this task.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    protected override void ExecuteCore(CancellationToken cancellationToken)
    {
        for (int i = 0; i < _tasks.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var task = _tasks[i];

            // Skip tasks that are already completed, cancelled, or failed
            if (task.State == TaskState.Completed ||
                task.State == TaskState.Cancelled ||
                task.State == TaskState.Failed)
            {
                continue;
            }

            // Update progress
            ReportProgress((i * 100) / _tasks.Length, $"Executing task {i + 1} of {_tasks.Length}: {task.Name}");

            try
            {
                // Reset task state if it was previously queued but not executed
                if (task.State != TaskState.Created)
                {
                    task.State = TaskState.Created;
                }

                // Simulate the task being queued
                task.State = TaskState.Queued;

                // Execute the task
                task.Execute();

                // If the batch was cancelled, cancel all remaining tasks
                if (cancellationToken.IsCancellationRequested)
                {
                    for (int j = i + 1; j < _tasks.Length; j++)
                    {
                        _tasks[j].Cancel();
                    }
                    break;
                }
            }
            catch (Exception ex)
            {
                Godot.GD.PrintErr($"Error executing task {i + 1} of {_tasks.Length} in batch '{Name}': {ex.Message}");
                throw;
            }
        }

        ReportProgress(100, "Batch execution completed");
    }
}
