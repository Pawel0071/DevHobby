namespace RPG.Application.Interfaces;

public interface IGameEventDispatcher
{
    void Dispatch<TEvent>(TEvent gameEvent);
}