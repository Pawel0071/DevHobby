namespace RPG.Abstractions.Interfaces;

public interface IEventSequenceStore
{
    int NextSequence(Guid correlationId);
}
