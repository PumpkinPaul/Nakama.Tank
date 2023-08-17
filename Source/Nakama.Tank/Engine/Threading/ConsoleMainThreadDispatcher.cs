// Copyright Pumpkin Games Ltd. All Rights Reserved.

using System;
using System.Collections.Generic;

namespace NakamaTank.Engine;

public class ConsoleMainThreadDispatcher
{
    static ConsoleMainThreadDispatcher _instance = null;
    static readonly Queue<Action> ExecutionQueue = new();

    public static void Update()
    {
        lock (ExecutionQueue)
        {
            while (ExecutionQueue.Count > 0)
                ExecutionQueue.Dequeue().Invoke();
        }
    }

    /// <summary>
    /// Locks the queue and adds the Action to it.
    /// </summary>
    /// <param name="action">function that will be executed from the main thread.</param>
    public static void Enqueue(Action action)
    {
        lock (ExecutionQueue)
        {
            ExecutionQueue.Enqueue(action);
        }
    }

    public static ConsoleMainThreadDispatcher Instance => _instance ??= new ConsoleMainThreadDispatcher();
}