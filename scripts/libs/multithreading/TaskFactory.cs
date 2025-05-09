using System;
using System.Threading;

namespace ITOC.Multithreading;

/// <summary>
/// A task factory for creating various types of tasks.
/// Provides convenient methods for creating and enqueueing common task types.
/// </summary>
public static class TaskFactory
{
    #region Action Tasks

    /// <summary>
    /// Creates a new action task with the specified action.
    /// </summary>
    /// <param name="name">The name of the task.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="priority">The priority of the task.</param>
    /// <returns>The created task.</returns>
    public static ActionTask CreateAction(string name, Action action, TaskPriority priority = TaskPriority.Normal)
    {
        return new ActionTask(name, action, priority);
    }

    /// <summary>
    /// Creates a new action task with the specified action and immediately enqueues it.
    /// </summary>
    /// <param name="name">The name of the task.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="priority">The priority of the task.</param>
    /// <returns>The enqueued task.</returns>
    public static ActionTask EnqueueAction(string name, Action action, TaskPriority priority = TaskPriority.Normal)
    {
        var task = CreateAction(name, action, priority);
        return (ActionTask)TaskManager.Instance.EnqueueTask(task);
    }

    /// <summary>
    /// Creates a new action task with the specified cancellable action.
    /// </summary>
    /// <param name="name">The name of the task.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="priority">The priority of the task.</param>
    /// <returns>The created task.</returns>
    public static ActionTask CreateAction(string name, Action<CancellationToken> action, TaskPriority priority = TaskPriority.Normal)
    {
        return new ActionTask(name, action, priority);
    }

    /// <summary>
    /// Creates a new action task with the specified cancellable action and immediately enqueues it.
    /// </summary>
    /// <param name="name">The name of the task.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="priority">The priority of the task.</param>
    /// <returns>The enqueued task.</returns>
    public static ActionTask EnqueueAction(string name, Action<CancellationToken> action, TaskPriority priority = TaskPriority.Normal)
    {
        var task = CreateAction(name, action, priority);
        return (ActionTask)TaskManager.Instance.EnqueueTask(task);
    }

    #endregion

    #region Batch Tasks

    /// <summary>
    /// Creates a new batch task that will execute the specified tasks in sequence.
    /// </summary>
    /// <param name="name">The name of the batch task.</param>
    /// <param name="priority">The priority of the batch task.</param>
    /// <param name="tasks">The tasks to execute in sequence.</param>
    /// <returns>The created batch task.</returns>
    public static BatchTask CreateBatch(string name, TaskPriority priority = TaskPriority.Normal, params GameTask[] tasks)
    {
        return new BatchTask(name, priority, tasks);
    }

    /// <summary>
    /// Creates a new batch task that will execute the specified tasks in sequence and immediately enqueues it.
    /// </summary>
    /// <param name="name">The name of the batch task.</param>
    /// <param name="priority">The priority of the batch task.</param>
    /// <param name="tasks">The tasks to execute in sequence.</param>
    /// <returns>The enqueued batch task.</returns>
    public static BatchTask EnqueueBatch(string name, TaskPriority priority = TaskPriority.Normal, params GameTask[] tasks)
    {
        var task = CreateBatch(name, priority, tasks);
        return (BatchTask)TaskManager.Instance.EnqueueTask(task);
    }

    #endregion

    #region Function Tasks

    /// <summary>
    /// Creates a new function task with the specified function.
    /// </summary>
    /// <typeparam name="T">The return type of the function.</typeparam>
    /// <param name="name">The name of the task.</param>
    /// <param name="function">The function to execute.</param>
    /// <param name="priority">The priority of the task.</param>
    /// <returns>The created task.</returns>
    public static FunctionTask<T> CreateFunction<T>(string name, Func<T> function, TaskPriority priority = TaskPriority.Normal)
    {
        return new FunctionTask<T>(name, function, priority);
    }

    /// <summary>
    /// Creates a new function task with the specified function and immediately enqueues it.
    /// </summary>
    /// <typeparam name="T">The return type of the function.</typeparam>
    /// <param name="name">The name of the task.</param>
    /// <param name="function">The function to execute.</param>
    /// <param name="priority">The priority of the task.</param>
    /// <returns>The enqueued task.</returns>
    public static FunctionTask<T> EnqueueFunction<T>(string name, Func<T> function, TaskPriority priority = TaskPriority.Normal)
    {
        var task = CreateFunction(name, function, priority);
        return (FunctionTask<T>)TaskManager.Instance.EnqueueTask(task);
    }

    /// <summary>
    /// Creates a new function task with the specified cancellable function.
    /// </summary>
    /// <typeparam name="T">The return type of the function.</typeparam>
    /// <param name="name">The name of the task.</param>
    /// <param name="function">The function to execute.</param>
    /// <param name="priority">The priority of the task.</param>
    /// <returns>The created task.</returns>
    public static FunctionTask<T> CreateFunction<T>(string name, Func<CancellationToken, T> function, TaskPriority priority = TaskPriority.Normal)
    {
        return new FunctionTask<T>(name, function, priority);
    }

    /// <summary>
    /// Creates a new function task with the specified cancellable function and immediately enqueues it.
    /// </summary>
    /// <typeparam name="T">The return type of the function.</typeparam>
    /// <param name="name">The name of the task.</param>
    /// <param name="function">The function to execute.</param>
    /// <param name="priority">The priority of the task.</param>
    /// <returns>The enqueued task.</returns>
    public static FunctionTask<T> EnqueueFunction<T>(string name, Func<CancellationToken, T> function, TaskPriority priority = TaskPriority.Normal)
    {
        var task = CreateFunction(name, function, priority);
        return (FunctionTask<T>)TaskManager.Instance.EnqueueTask(task);
    }

    #endregion
}