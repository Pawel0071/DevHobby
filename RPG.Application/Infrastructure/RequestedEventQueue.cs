using System.Collections.Concurrent;
using RPG.Abstractions.Interfaces;

namespace RPG.Application.Infrastructure;

public interface IRequestEventQueue
{
    void Enqueue(IGameEvent gameEvent);
    bool TryDequeue(out IGameEvent gameEvent);
}

public sealed class RequestedEventQueue : IRequestEventQueue
{
    private readonly ConcurrentQueue<IGameEvent> _queue = new();

    public void Enqueue(IGameEvent gameEvent)
    {
        if (gameEvent is null) return;
        _queue.Enqueue(gameEvent);
    }

    public bool TryDequeue(out IGameEvent gameEvent)
    {
        return _queue.TryDequeue(out gameEvent!);
    }
}
