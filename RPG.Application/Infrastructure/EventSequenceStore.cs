using System.Collections.Concurrent;
using RPG.Abstractions.Interfaces;

namespace RPG.Application.Infrastructure;

public sealed class InMemoryEventSequenceStore : IEventSequenceStore
{
    private readonly ConcurrentDictionary<Guid, int> _sequences = new();
    public int NextSequence(Guid correlationId)
    {
        return _sequences.AddOrUpdate(correlationId, 1, (_, current) => current + 1);
    }
}
