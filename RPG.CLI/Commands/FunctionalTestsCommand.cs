using System.CommandLine;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using RPG.CLI.FunctionalTests;

namespace RPG.CLI.Commands;

internal sealed class FunctionalTestsCommand
{
    private const string DefaultSamplePath = "Samples/item.json";
    private readonly IServiceProvider _services;

    public FunctionalTestsCommand(IServiceProvider services)
    {
        _services = services;
    }

    public Command Build()
    {
        var sampleOption = new Option<string>("--sample", () => DefaultSamplePath, "Path to the sample JSON payload.");
        var command = new Command("functional-tests", "Runs the infrastructure/persistence/cache functional pipeline.")
        {
            sampleOption
        };

        command.SetHandler(async samplePath =>
        {
            var runner = _services.GetRequiredService<FunctionalTestRunner>();
            var exitCode = await runner.RunAsync(Path.GetFullPath(samplePath), CancellationToken.None);
            Environment.ExitCode = exitCode;
        }, sampleOption);

        return command;
    }
}
