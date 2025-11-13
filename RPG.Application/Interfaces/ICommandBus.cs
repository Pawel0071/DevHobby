using System.Threading;
using System.Threading.Tasks;
using RPG.Application.Commands;
using RPG.Application.Infrastructure;

namespace RPG.Application.Interfaces;

public interface ICommandBus
{
    Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand;
}
