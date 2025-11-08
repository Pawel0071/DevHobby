using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using RPG.Application;
using RPG.Core;
using RPG.GameServer.Controlers;
using RPG.GameServer.Controllers;
using RPG.GameServer.EventHandlers;
using RPG.GameServer.Interfaces;
using RPG.GameServer.Services;
using RPG.Infrastructure;
using RPG.Application.Events;
using RPG.Application.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", false, true)
    .AddJsonFile("../RPG.Infrastructure/appsettings.infrastructure.json", true, true)
    .AddJsonFile("../RPG.Core/appsettings.core.json", true, true)
    .AddJsonFile("../RPG.Application/appsettings.application.json", true, true);

// gRPC
builder.Services.AddGrpc();

// OpenTelemetry - Tracing i Metrics
var otlpEndpoint = builder.Configuration.GetValue<string>("OpenTelemetry:OtlpEndpoint") ?? "http://localhost:4317";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService("RPG.GameServer", serviceVersion: "1.0.0"))
    .WithTracing(tracing => tracing
        .AddSource("RPG.GameServer") // Nasz ActivitySource z OpenTelemetryActivityScope
        .AddAspNetCoreInstrumentation(options =>
        {
            options.RecordException = true;
            options.Filter = context => !context.Request.Path.StartsWithSegments("/health");
        })
        .AddGrpcClientInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri(otlpEndpoint);
            options.Protocol = OtlpExportProtocol.Grpc;
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddPrometheusExporter());

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddCore(builder.Configuration);
builder.Services.AddApplication(builder.Configuration);

builder.Services.AddSingleton<ICharacterStateBroadcaster, CharacterStateBroadcaster>();
builder.Services.AddScoped<CharacterMovementEventHandler>();
builder.Services.AddScoped<IGameEventHandler<CharacterMovedEvent>>(sp => sp.GetRequiredService<CharacterMovementEventHandler>());
builder.Services.AddScoped<IGameEventHandler<CharacterMovementStoppedEvent>>(sp => sp.GetRequiredService<CharacterMovementEventHandler>());
builder.Services.AddScoped<IGameEventHandler<CharacterRotationStartedEvent>>(sp => sp.GetRequiredService<CharacterMovementEventHandler>());
builder.Services.AddScoped<IGameEventHandler<CharacterRotationStoppedEvent>>(sp => sp.GetRequiredService<CharacterMovementEventHandler>());

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
