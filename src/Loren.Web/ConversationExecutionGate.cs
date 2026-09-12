using System.Collections.Concurrent;

namespace Loren.Web;

public sealed class ConversationExecutionGate
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

    public bool TryEnter(Guid conversationId, out IDisposable lease)
    {
        SemaphoreSlim gate = _locks.GetOrAdd(conversationId, static _ => new SemaphoreSlim(1, 1));
        if (!gate.Wait(0)) { lease = EmptyLease.Instance; return false; }
        lease = new Lease(gate);
        return true;
    }

    private sealed class Lease(SemaphoreSlim gate) : IDisposable
    {
        public void Dispose() => gate.Release();
    }

    private sealed class EmptyLease : IDisposable
    {
        public static readonly EmptyLease Instance = new();
        public void Dispose() { }
    }
}
