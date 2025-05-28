namespace ITOC.Core.Multithreading;

/// <summary>
/// Provides configuration options for the multithreading system.
/// </summary>
public class TaskManagerConfig
{
    /// <summary>
    /// Gets or sets the maximum number of worker threads.
    /// If 0 or negative, will use Environment.ProcessorCount - 1.
    /// </summary>
    public int MaxWorkerThreads { get; set; }

    /// <summary>
    /// Gets or sets whether to automatically start the worker threads after initialization.
    /// </summary>
    public bool AutoStart { get; set; }

    /// <summary>
    /// Gets or sets whether to log detailed information about task execution.
    /// </summary>
    public bool VerboseLogging { get; set; }

    /// <summary>
    /// Gets or sets the maximum time in milliseconds a worker thread can be idle before being suspended.
    /// A value of 0 means threads are never suspended.
    /// </summary>
    public int IdleThreadTimeoutMs { get; set; }

    /// <summary>
    /// Gets or sets the period in milliseconds to check for and log statistics.
    /// A value of 0 means statistics are never logged.
    /// </summary>
    public int StatisticsLoggingPeriodMs { get; set; }

    /// <summary>
    /// Gets or sets the timeout in milliseconds for tasks before they are considered stuck.
    /// A value of 0 means tasks are never considered stuck.
    /// </summary>
    public int TaskTimeoutMs { get; set; }

    /// <summary>
    /// Gets or sets whether to capture stack traces when tasks are created.
    /// This is useful for debugging but has a performance impact.
    /// </summary>
    public bool CaptureTaskStackTraces { get; set; }

    /// <summary>
    /// Creates a new instance of the <see cref="TaskManagerConfig"/> class with default values.
    /// </summary>
    public TaskManagerConfig()
    {
        MaxWorkerThreads = 0; // Auto (ProcessorCount - 1)
        AutoStart = true;
        VerboseLogging = false;
        IdleThreadTimeoutMs = 0; // Never suspend threads
        StatisticsLoggingPeriodMs = 0; // Never log statistics
        TaskTimeoutMs = 0; // Never timeout tasks
        CaptureTaskStackTraces = false;
    }

    /// <summary>
    /// Creates a default configuration for a development environment.
    /// </summary>
    /// <returns>A development-oriented configuration.</returns>
    public static TaskManagerConfig Development()
    {
        return new TaskManagerConfig
        {
            MaxWorkerThreads = 0, // Auto
            AutoStart = true,
            VerboseLogging = true,
            IdleThreadTimeoutMs = 0, // Never suspend threads
            StatisticsLoggingPeriodMs = 10000, // Log statistics every 10 seconds
            TaskTimeoutMs = 30000, // 30 seconds task timeout
            CaptureTaskStackTraces = true
        };
    }

    /// <summary>
    /// Creates a default configuration for a production environment.
    /// </summary>
    /// <returns>A production-oriented configuration.</returns>
    public static TaskManagerConfig Production()
    {
        return new TaskManagerConfig
        {
            MaxWorkerThreads = 0, // Auto
            AutoStart = true,
            VerboseLogging = false,
            IdleThreadTimeoutMs = 60000, // Suspend threads after 1 minute of inactivity
            StatisticsLoggingPeriodMs = 0, // Don't log statistics
            TaskTimeoutMs = 0, // Never timeout tasks
            CaptureTaskStackTraces = false
        };
    }

    /// <summary>
    /// Creates a configuration optimized for background processing with minimal resource usage.
    /// </summary>
    /// <returns>A low-resource configuration.</returns>
    public static TaskManagerConfig LowResource()
    {
        return new TaskManagerConfig
        {
            MaxWorkerThreads = Math.Max(1, Environment.ProcessorCount / 2),
            AutoStart = true,
            VerboseLogging = false,
            IdleThreadTimeoutMs = 10000, // Suspend threads after 10 seconds of inactivity
            StatisticsLoggingPeriodMs = 0, // Don't log statistics
            TaskTimeoutMs = 0, // Never timeout tasks
            CaptureTaskStackTraces = false
        };
    }

    /// <summary>
    /// Creates a configuration optimized for maximum performance.
    /// </summary>
    /// <returns>A high-performance configuration.</returns>
    public static TaskManagerConfig HighPerformance()
    {
        return new TaskManagerConfig
        {
            MaxWorkerThreads = Math.Max(1, Environment.ProcessorCount),
            AutoStart = true,
            VerboseLogging = false,
            IdleThreadTimeoutMs = 0, // Never suspend threads
            StatisticsLoggingPeriodMs = 0, // Don't log statistics
            TaskTimeoutMs = 0, // Never timeout tasks
            CaptureTaskStackTraces = false
        };
    }
}