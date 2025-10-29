using RPG.Core.Application.Handlers;

namespace RPG.Application.Interfaces;

public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    CommandResult Handle(TCommand command);
}