using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class AIWorker
{
    private readonly Queue<Func<Task>> _tasks = new();
    private readonly SemaphoreSlim _semaphore = new(1);

    public async Task Enqueue(Func<Task> task)
    {
        lock (_tasks)
        {
            _tasks.Enqueue(task);
        }
        await ProcessQueueAsync();
    }

    private async Task ProcessQueueAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            while (true)
            {
                Func<Task>? task;
                lock (_tasks)
                {
                    if (_tasks.Count == 0) return;
                    task = _tasks.Dequeue();
                }
                await task();
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
