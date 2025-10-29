namespace RPG.Application.Interfaces;

public interface ICommandBus
{
    void Dispatch<TCommand>(TCommand command) where TCommand : ICommand;
}