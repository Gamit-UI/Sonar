using System.Collections.Concurrent;
using System.Diagnostics;

namespace Sonar.Helpers;

internal sealed class SequentialScheduler : TaskScheduler, IDisposable
{
    private readonly BlockingCollection<Task> mTaskQueue = new();
    private readonly Thread mThread;
    private readonly CancellationTokenSource mCancellation;
    private volatile bool mDisposed;

    public SequentialScheduler()
    {
        mCancellation = new CancellationTokenSource();
        mThread = new Thread(Run)
        {
            IsBackground = true
        };
        
        mThread.Start();
    }

    public void Dispose()
    {
        mDisposed = true;
        mCancellation.Cancel();
    }

    private void Run()
    {
        while (!mDisposed)
        {
            try
            {
                var task = mTaskQueue.Take(mCancellation.Token);
                TryExecuteTask(task);
            }
            catch (OperationCanceledException)
            {
                Debug.Assert(mDisposed);
                break;
            }
        }
    }

    protected override IEnumerable<Task> GetScheduledTasks()
    {
        return mTaskQueue;
    }

    protected override void QueueTask(Task task)
    {
        mTaskQueue.Add(task);
    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
        if (Thread.CurrentThread == mThread)
        {
            return TryExecuteTask(task);
        }

        return false;
    }
}