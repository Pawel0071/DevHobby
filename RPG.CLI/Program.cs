using System.CommandLine;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RPG.Application;
using RPG.Application.Handlers;
using RPG.CLI.Commands;
using RPG.Core;
using RPG.Infrastructure;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("appsettings.json", false, true);
        config.AddJsonFile("../RPG.Infrastructure/appsettings.infrastructure.json", true, true);
        config.AddJsonFile("../RPG.Core/appsettings.core.json", true, true);
        config.AddJsonFile("../RPG.Application/appsettings.application.json", true, true);
    })
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        // MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CharacterCommandHandler).Assembly);
        });

        services.AddInfrastructure(configuration);
        services.AddCore(configuration);
        services.AddApplication(configuration);
    });

using var host = builder.Build();
var services = host.Services;
var mediator = services.GetRequiredService<IMediator>();

// CLI root
var rootCommand = new RootCommand("RPG CLI");

var equipCommand = new EquipCommand(services);

await rootCommand.InvokeAsync(args);
