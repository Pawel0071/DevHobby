using Microsoft.Extensions.DependencyInjection;
using RPG.Application.Commands;
using RPG.Application.Interfaces;
using RPG.Application.Diagnostics;
using RPG.Abstractions.Interfaces;
using System.Diagnostics;

namespace RPG.Application.Infrastructure;

public class CommandBus(IServiceProvider serviceProvider) : ICommandBus
{
    public async Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand
    {
        if (command is null) throw new ArgumentNullException(nameof(command));

        using var activity = ApplicationDiagnostics.ActivitySource.StartActivity("CommandBus.Dispatch");
        activity?.SetTag("rpg.command.type", typeof(TCommand).Name);
        ApplicationDiagnostics.CountCommand(typeof(TCommand).Name);

        using var scope = serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetService<ICommandHandler<TCommand>>();
        if (handler is null)
            throw new InvalidOperationException($"No handler registered for command type {typeof(TCommand).Name}");

        var correlationId = Guid.NewGuid();
        var occurredAt = DateTime.UtcNow;
        if (command is IMetadataCommand metaCmd)
        {
            metaCmd.Metadata = new CommandMetadata(Guid.NewGuid(), correlationId, null, occurredAt);
        }

        try
        {
            var result = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);
            activity?.SetTag("rpg.command.success", result.Success);
            activity?.SetTag("rpg.command.error", result.Result.ToString());
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            activity?.SetTag("rpg.command.cancelled", true);
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.type", ex.GetType().FullName);
            activity?.SetTag("exception.message", ex.Message);
            throw;
        }
    }
}
