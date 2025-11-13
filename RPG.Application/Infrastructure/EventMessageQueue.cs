using System.Diagnostics;
using RPG.Abstractions.Interfaces;
using RPG.Application.Diagnostics;
using RPG.Application.Interfaces;

namespace RPG.Application.Infrastructure;

public interface IEventMessageQueue
{
    void Enqueue(IGameEvent gameEvent);
    IReadOnlyCollection<IGameEvent> Pending { get; }
    Task PublishAsync(CancellationToken cancellationToken = default);
    void Clear();
}

public sealed class EventMessageQueue : IEventMessageQueue
{
    private readonly List<IGameEvent> _events = new();
    private readonly IGameEventDispatcher _dispatcher;
    private readonly object _lock = new();

    public EventMessageQueue(IGameEventDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public void Enqueue(IGameEvent gameEvent)
    {
        if (gameEvent is null) return;
        lock (_lock)
        {
            _events.Add(gameEvent);
        }
    }

    public IReadOnlyCollection<IGameEvent> Pending { get { lock (_lock) return _events.ToArray(); } }

    public async Task PublishAsync(CancellationToken cancellationToken = default)
    {
        if (_events.Count == 0) return;
        using var activity = ApplicationDiagnostics.ActivitySource.StartActivity("EventMessageQueue.Publish");
        activity?.SetTag("rpg.event.count", _events.Count);

        // Kopia aby zdarzenia dodane podczas publikacji nie były iterowane w tej samej rundzie
        var snapshot = Pending;
        lock (_lock) { _events.Clear(); }
        foreach (var e in snapshot)
        {
            await _dispatcher.DispatchAsync((dynamic)e, cancellationToken).ConfigureAwait(false);
        }
    }

    public void Clear() => _events.Clear();
}
