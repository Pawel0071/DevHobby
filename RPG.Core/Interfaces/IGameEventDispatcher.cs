namespace RPG.Core.Interfaces;

public interface IGameEventDispatcher
{
    void Dispatch<TEvent>(TEvent gameEvent);
}