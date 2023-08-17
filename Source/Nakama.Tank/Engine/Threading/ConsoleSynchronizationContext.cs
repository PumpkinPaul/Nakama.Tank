// Copyright Pumpkin Games Ltd. All Rights Reserved.

using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace NakamaTank.Engine;

public sealed class ConsoleSynchronizationContext : SynchronizationContext
{
    private static readonly ConcurrentQueue<Message> Queue;

    static ConsoleSynchronizationContext() => Queue = new ConcurrentQueue<Message>();

    private static void Enqueue(SendOrPostCallback callback, object state) => Queue.Enqueue(new Message(callback, state));

    public static void Update()
    {
        if (!Queue.Any())
            return;

        if (!Queue.TryDequeue(out Message message))
            return;

        message.Callback(message.State);
    }

    public override SynchronizationContext CreateCopy() => new ConsoleSynchronizationContext();

    public override void Post(SendOrPostCallback d, object state) => Enqueue(d, state);

    public override void Send(SendOrPostCallback d, object state) => Enqueue(d, state);

    private sealed class Message
    {
        public Message(SendOrPostCallback callback, object state)
        {
            Callback = callback;
            State = state;
        }

        public SendOrPostCallback Callback { get; }

        public object State { get; }
    }
}