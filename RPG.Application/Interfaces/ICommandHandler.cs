using RPG.Core.Application.Handlers;

namespace RPG.Application.Interfaces;

public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    Task<CommandResult> HandleAsync(TCommand command);
}