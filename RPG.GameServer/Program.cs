using Microsoft.AspNetCore.Builder;
using RabbitMQ.Client;
using RPG.Core.Interfaces;
using RPG.Core.Services.EquipmentService;
using RPG.Core.Services.InventoryService;
using RPG.Core.Services.LevelService;
using RPG.Core.Services.SkillService;
using RPG.Core.Services.StatsService;
using RPG.GameServer.Controlers;
using RPG.GameServer.Controllers;
using RPG.Infrastructure.Logger;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// gRPC
builder.Services.AddGrpc();

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect("localhost"));

builder.Services.AddSingleton<IDatabase>(sp =>
    sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());

builder.Services.AddSingleton(typeof(RPG.Infrastructure.Logger.ILogger<>), typeof(SerilogWrapper<>));
builder.Services.AddSingleton<IEquipmentService, EquipmentService>();
builder.Services.AddSingleton<IInventoryService, InventoryService>();
builder.Services.AddSingleton<ISkillService, SkillService>();
builder.Services.AddSingleton<IStatsService, StatsService>();
builder.Services.AddSingleton<ILevelingService, LevelingService>();

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