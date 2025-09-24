using RabbitMQ.Client;
using RPG.GameServer.Controlers;
using RPG.GameServer.Controllers;
using RPG.GameServer.Infrastructure;
using RPG.GameServer.Interfaces;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// gRPC
builder.Services.AddGrpc();

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect("localhost"));

builder.Services.AddSingleton<IDatabase>(sp =>
    sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());

// RabbitMQ
builder.Services.AddSingleton<IConnection>(sp =>
{
    var factory = new ConnectionFactory { HostName = "localhost" };
    return factory.CreateConnection();
});

builder.Services.AddSingleton<IModel>(sp =>
{
    var connection = sp.GetRequiredService<IConnection>();
    var channel = connection.CreateModel();
    channel.ExchangeDeclare("rpg_exchange", ExchangeType.Topic, durable: true);
    return channel;
});

// Repozytoria
builder.Services.AddScoped<ICharacterRepository, CharacterRepository>();

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