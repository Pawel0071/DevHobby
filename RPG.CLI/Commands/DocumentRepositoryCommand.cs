using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using RPG.CLI.Scenarios;

namespace RPG.CLI.Commands;

/// <summary>
///     CLI command that executes the document repository scenarios for one or all entity mappings.
/// </summary>
internal sealed class DocumentRepositoryCommand
{
    private readonly IServiceProvider _services;

    public DocumentRepositoryCommand(IServiceProvider services)
    {
        _services = services;
    }

    public Command Build()
    {
        var entityOption = new Option<string?>(
            name: "--entity",
            description: "Run the document repository test only for the specified entity key (e.g. 'item').");

        var command = new Command(
            "document-tests",
            "Runs CRUD verification scenarios for each DocumentRepository mapping.")
        {
            entityOption
        };

        command.SetHandler(async (string? entityKey) =>
        {
            var runner = _services.GetRequiredService<DocumentRepositoryScenarioRunner>();
            var exitCode = await runner.RunAsync(entityKey, CancellationToken.None);
            Environment.ExitCode = exitCode;
        }, entityOption);

        return command;
    }
}
