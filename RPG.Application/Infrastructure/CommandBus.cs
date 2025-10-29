using RPG.Application.Interfaces;

namespace RPG.Application.Infrastructure;

public class CommandBus(IServiceProvider serviceProvider) : ICommandBus
{
    public void Dispatch<TCommand>(TCommand command) where TCommand : ICommand
    {
        var handlerType = typeof(ICommandHandler<TCommand>);
        var handler = serviceProvider.GetService(handlerType);

        if (handler is null)
            throw new InvalidOperationException($"No handler registered for command type {typeof(TCommand).Name}");

        ((ICommandHandler<TCommand>)handler).Handle(command);
    }
}