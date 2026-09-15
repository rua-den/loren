using System.Collections.Concurrent;

namespace Loren.Web;

public sealed class ConversationExecutionGate
{
    private readonly ConcurrentDictionary<Guid, byte> _active = new();

    public bool TryEnter(Guid conversationId, out IDisposable lease)
    {
        if (!_active.TryAdd(conversationId, 0))
        {
            lease = EmptyLease.Instance;
            return false;
        }

        lease = new Lease(_active, conversationId);
        return true;
    }

    private sealed class Lease(
        ConcurrentDictionary<Guid, byte> active,
        Guid conversationId) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                active.TryRemove(conversationId, out _);
            }
        }
    }

    private sealed class EmptyLease : IDisposable
    {
        public static readonly EmptyLease Instance = new();

        public void Dispose()
        {
        }
    }
}
