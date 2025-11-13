using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using RPG.Abstractions.Interfaces;
using RPG.Application;
using RPG.Application.Broadcasters;
using RPG.Core;
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
builder.Services.AddHealthChecks();

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080, o =>
    {
        o.Protocols = HttpProtocols.Http1AndHttp2; // pozwala curl (HTTP/1.1) oraz gRPC (HTTP/2)
    });
});

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
builder.Services.AddControllers();

// Serwisy gRPC
builder.Services.AddScoped<CharacterServiceImpl>();
builder.Services.AddScoped<SessionServiceImpl>();
builder.Services.AddScoped<InteractionServiceImpl>();
builder.Services.AddScoped<WorldServiceImpl>();

var app = builder.Build();

// Prometheus metrics endpoint
app.MapPrometheusScrapingEndpoint();

// Health checks for Kubernetes/Docker
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    // Liveness: nie uruchamiamy żadnych zarejestrowanych checków – sprawdza tylko czy proces i pipeline żyją
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    // Readiness: uruchamia wszystkie zarejestrowane checki (Mongo, Redis, RabbitMQ, itp.)
    Predicate = _ => true
});

// Mapowanie gRPC
app.MapGrpcService<CharacterServiceImpl>();
app.MapGrpcService<SessionServiceImpl>();
app.MapGrpcService<InteractionServiceImpl>();
app.MapGrpcService<WorldServiceImpl>();

app.MapGet("/", () =>
    "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
app.MapControllers();
app.MapGet("/ping", () => Results.Ok("pong"));

app.Run();

public partial class Program;
