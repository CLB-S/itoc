using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Godot;
using Environment = System.Environment;

namespace ChunkGenerator;

public class ChunkFactory : IDisposable
{
    private readonly BlockingCollection<ChunkGenerationRequest> _chunkQueue = new();
    private readonly BlockingCollection<ChunkColumnGenerationRequest> _chunkColumnQueue = new();
    private readonly List<Thread> _workerThreads = new();
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed;

    private readonly SemaphoreSlim _throttler;
    private readonly int _maxConcurrentJobs;

    private ChunkFactory()
    {
    }

    public ChunkFactory(int maxConcurrentJobs = 0)
    {
        _maxConcurrentJobs = maxConcurrentJobs > 0
            ? maxConcurrentJobs
            : Math.Max(1, Environment.ProcessorCount - 2);

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
        _chunkQueue.Add(request);
    }

    public void Enqueue(ChunkColumnGenerationRequest request)
    {
        if (_disposed) return;
        _chunkColumnQueue.Add(request);
    }

    private void ProcessQueue(object obj)
    {
        var ct = (CancellationToken)obj;
        while (!ct.IsCancellationRequested)
            try
            {
                _throttler.Wait(ct);

                try
                {
                    if (_chunkQueue.TryTake(out var request))
                    {
                        var result = new ChunkGenerationPipeline(request).Execute();
                        request.Callback?.Invoke(result);
                    }
                    else if (_chunkColumnQueue.TryTake(out var columnRequest))
                    {
                        columnRequest.Callback?.Invoke(columnRequest.Execute());
                    }
                    else
                    {
                        // No requests to process, wait for a short time before checking again
                        Thread.Sleep(10);
                    }
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

    public void Stop()
    {
        if (_disposed) return;
        _cts.Cancel();

        foreach (var thread in _workerThreads)
            if (thread.IsAlive)
                thread.Join();

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
                if (thread.IsAlive)
                    thread.Join();

            // Dispose managed resources
            _chunkQueue?.Dispose();
            _chunkColumnQueue?.Dispose();
            _cts?.Dispose();
            _throttler?.Dispose();
        }
        catch (Exception e)
        {
            GD.PrintErr($"Error during disposal: {e}");
        }
    }
}