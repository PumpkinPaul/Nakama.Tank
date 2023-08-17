// Copyright Pumpkin Games Ltd. All Rights Reserved.

//Based on code here:
//https://devblogs.microsoft.com/pfxteam/await-synchronizationcontext-and-console-apps/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NakamaTank.Engine;

public static class AsyncPump
{
    /// <summary>Runs the specified asynchronous function.</summary>
    /// <param name="func">The asynchronous function to execute.</param>
    public static void Run(Func<Task> func)
    {
        if (func == null) 
            throw new ArgumentNullException(nameof(func));

        var prevCtx = SynchronizationContext.Current;
        try
        {
            // Establish the new context
            var syncCtx = new SingleThreadSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(syncCtx);

            // Invoke the function and alert the context to when it completes
            var t = func() ?? throw new InvalidOperationException("No task provided.");
            t.ContinueWith(delegate { syncCtx.Complete(); }, TaskScheduler.Default);

            // Pump continuations and propagate any exceptions
            syncCtx.RunOnCurrentThread();
            t.GetAwaiter().GetResult();
        }
        finally { SynchronizationContext.SetSynchronizationContext(prevCtx); }
    }

    public static void Run(Action asyncMethod)
    {
        var prevCtx = SynchronizationContext.Current;
        try
        {
            var syncCtx = new SingleThreadSynchronizationContext(true);
            SynchronizationContext.SetSynchronizationContext(syncCtx);

            syncCtx.OperationStarted();
            asyncMethod();
            syncCtx.OperationCompleted();

            syncCtx.RunOnCurrentThread();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(prevCtx);
        }
    }

    /// <summary>Provides a SynchronizationContext that's single-threaded.</summary>
    private sealed class SingleThreadSynchronizationContext : SynchronizationContext
    {
        public SingleThreadSynchronizationContext(bool tracking = false)
        {
        }

        int _operationCount = 0;

        /// <summary>The queue of work items.</summary>
        readonly BlockingCollection<KeyValuePair<SendOrPostCallback, object>> _queue = new();

        /// <summary>The processing thread.</summary>
        readonly Thread _thread = Thread.CurrentThread;

        /// <summary>Dispatches an asynchronous message to the synchronization context.</summary>
        /// <param name="callback">The System.Threading.SendOrPostCallback delegate to call.</param>
        /// <param name="state">The object passed to the delegate.</param>
        public override void Post(SendOrPostCallback callback, object state)
        {
            if (callback == null) 
                throw new ArgumentNullException(nameof(callback));

            _queue.Add(new KeyValuePair<SendOrPostCallback, object>(callback, state));
        }

        /// <summary>Not supported.</summary>
        public override void Send(SendOrPostCallback d, object state)
        {
            throw new NotSupportedException("Synchronously sending is not supported.");
        }

        /// <summary>Runs an loop to process all queued work items.</summary>
        public void RunOnCurrentThread()
        {
            foreach (var workItem in _queue.GetConsumingEnumerable())
                workItem.Key(workItem.Value);
        }

        /// <summary>Notifies the context that no more work will arrive.</summary>
        public void Complete() { _queue.CompleteAdding(); }

        public override void OperationStarted()
        {
            Interlocked.Increment(ref _operationCount);
        }

        public override void OperationCompleted()
        {
            if (Interlocked.Decrement(ref _operationCount) == 0)
                Complete();
        }
    }
}