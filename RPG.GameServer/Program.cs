using RPG.Application;
using RPG.Core;
using RPG.GameServer.Controlers;
using RPG.GameServer.Controllers;
using RPG.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("../RPG.Infrastructure/appsettings.infrastructure.json", optional: true, reloadOnChange: true)
    .AddJsonFile("../RPG.Core/appsettings.core.json", optional: true, reloadOnChange: true)
    .AddJsonFile("../RPG.Application/appsettings.application.json", optional: true, reloadOnChange: true);

// gRPC
builder.Services.AddGrpc();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddCore(builder.Configuration);
builder.Services.AddApplication(builder.Configuration);

// Serwisy gRPC
builder.Services.AddScoped<CharacterServiceImpl>();
builder.Services.AddScoped<SessionServiceImpl>();
builder.Services.AddScoped<InteractionServiceImpl>();
builder.Services.AddScoped<WorldServiceImpl>();

var app = builder.Build();

// Mapowanie gRPC
app.MapGrpcService<CharacterServiceImpl>();
app.MapGrpcService<SessionServiceImpl>();
app.MapGrpcService<InteractionServiceImpl>();
app.MapGrpcService<WorldServiceImpl>();

app.MapGet("/", () =>
    "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();