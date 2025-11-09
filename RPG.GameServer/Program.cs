using System.IO;
using RPG.Abstractions;
using RPG.Abstractions.Interfaces;
using RPG.Application;
using RPG.Application.Broadcasters;
using RPG.Core;
using RPG.GameServer.Controlers;
using RPG.GameServer.Controllers;
using RPG.Core.Interfaces.NpcServices;
using RPG.Core.Services.NpcServices;
using RPG.Infrastructure;
using RPG.Application.Events;
using RPG.Application.Handlers;
using RPG.Application.Interfaces;

var builder = WebApplication.CreateBuilder(args);

var environmentName = builder.Environment.EnvironmentName;

builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

var infrastructureCandidates = new[]
{
    Path.Combine(AppContext.BaseDirectory, "appsettings.infrastructure.json"),
    Path.Combine(AppContext.BaseDirectory, $"appsettings.infrastructure.{environmentName}.json"),
    Path.Combine(builder.Environment.ContentRootPath, "..", "RPG.Infrastructure", "appsettings.infrastructure.json"),
    Path.Combine(builder.Environment.ContentRootPath, "..", "RPG.Infrastructure", $"appsettings.infrastructure.{environmentName}.json")
};

foreach (var candidate in infrastructureCandidates)
{
    if (File.Exists(candidate))
    {
        builder.Configuration.AddJsonFile(candidate, optional: false, reloadOnChange: true);
    }
}

// gRPC
builder.Services.AddGrpc();

builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.ApplicationName);
builder.Services.AddCore(builder.Configuration);
builder.Services.AddApplication(builder.Configuration);

builder.Services.AddSingleton<ICharacterStateBroadcaster, CharacterStateBroadcaster>();
builder.Services.AddScoped<CharacterMovementEventHandler>();
builder.Services.AddScoped<IGameEventHandler<CharacterMovedEvent>>(sp => sp.GetRequiredService<CharacterMovementEventHandler>());
builder.Services.AddScoped<IGameEventHandler<CharacterMovementStoppedEvent>>(sp => sp.GetRequiredService<CharacterMovementEventHandler>());
builder.Services.AddScoped<IGameEventHandler<CharacterRotationStartedEvent>>(sp => sp.GetRequiredService<CharacterMovementEventHandler>());
builder.Services.AddScoped<IGameEventHandler<CharacterRotationStoppedEvent>>(sp => sp.GetRequiredService<CharacterMovementEventHandler>());
builder.Services.AddSingleton<INpcAiService, NpcAiService>();
builder.Services.AddHostedService<NpcAiHostedService>();
builder.Services.AddSingleton<INpcCombatService, NpcCombatService>();

// Serwisy gRPC
builder.Services.AddScoped<CharacterServiceImpl>();
builder.Services.AddScoped<SessionServiceImpl>();
builder.Services.AddScoped<InteractionServiceImpl>();
builder.Services.AddScoped<WorldServiceImpl>();

var app = builder.Build();

// Prometheus metrics endpoint
app.MapPrometheusScrapingEndpoint();

// Mapowanie gRPC
app.MapGrpcService<CharacterServiceImpl>();
app.MapGrpcService<SessionServiceImpl>();
app.MapGrpcService<InteractionServiceImpl>();
app.MapGrpcService<WorldServiceImpl>();

app.MapGet("/", () =>
    "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();

public partial class Program;
