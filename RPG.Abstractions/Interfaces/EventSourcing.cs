namespace RPG.Abstractions.Interfaces;

public interface IAggregateRoot
{
    Guid Id { get; }
    int Version { get; }
}

public interface IEventApplier<TAggregate> where TAggregate : IAggregateRoot
{
    TAggregate Apply(TAggregate aggregate, IGameEventWithMetadata evt);
}
