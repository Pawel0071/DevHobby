using System.Threading;
using System.Threading.Tasks;
using RPG.Application.Commands;

namespace RPG.Application.Interfaces;

public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    Task<CommandResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
