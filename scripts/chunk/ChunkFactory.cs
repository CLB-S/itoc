using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Godot;

namespace ChunkGenerator;

public partial class ChunkFactory : IDisposable
{
    private readonly BlockingCollection<ChunkGenerationRequest> _queue = new();
    private readonly List<Thread> _workerThreads = new();
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed;

    private readonly SemaphoreSlim _throttler;
    private readonly int _maxConcurrentJobs;

    private ChunkFactory() { }

    public ChunkFactory(int maxConcurrentJobs = 0)
    {
        _maxConcurrentJobs = maxConcurrentJobs > 0
            ? maxConcurrentJobs
            : Math.Max(1, System.Environment.ProcessorCount - 2);

        _throttler = new SemaphoreSlim(_maxConcurrentJobs);

        for (var i = 0; i < _maxConcurrentJobs; i++)
            _workerThreads.Add(new Thread(ProcessQueue));

        GD.Print("ChunkFactory initialized with " + _maxConcurrentJobs + " threads.");
    }

    public void Start()
    {
        _workerThreads.ForEach(t => t.Start(_cts.Token));
    }

    public void Enqueue(ChunkGenerationRequest request)
    {
        if (_disposed) return;
        _queue.Add(request);
    }

    private void ProcessQueue(object obj)
    {
        var ct = (CancellationToken)obj;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                _throttler.Wait(ct);

                var request = _queue.Take(ct);

                try
                {
                    var result = new ChunkGenerationPipeline().Excute(request);
                    request?.Callback?.Invoke(result);
                }
                finally
                {
                    _throttler.Release();
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception e)
            {
                GD.PrintErr($"Chunk generation failed: {e}");
            }
        }
    }

    public void Stop()
    {
        if (_disposed) return;
        _cts.Cancel();

        foreach (var thread in _workerThreads)
        {
            if (thread.IsAlive)
                thread.Join();
        }

        Dispose();
    }

    public void Dispose()
    {
        //TODO: Need review.

        if (_disposed) return;
        _disposed = true;

        try
        {
            // Cancel any ongoing operations
            _cts.Cancel();

            // Wait for all threads to finish
            foreach (var thread in _workerThreads)
            {
                if (thread.IsAlive)
                    thread.Join();
            }

            // Dispose managed resources
            _queue?.Dispose();
            _cts?.Dispose();
            _throttler?.Dispose();
        }
        catch (Exception e)
        {
            GD.PrintErr($"Error during disposal: {e}");
        }
    }
}