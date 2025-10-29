using MongoDB.Driver;
using PersistenceService;
using RabbitMQ.Client;
using RPG.PersistenceService.Service;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var client = new MongoClient("mongodb://localhost:27017");
    return client.GetDatabase("RPGDatabase");
});

var factory = new ConnectionFactory { HostName = "localhost" };
var connection = await factory.CreateConnectionAsync();

builder.Services.AddSingleton<IConnection>(connection);

builder.Services.AddSingleton<IRabbitMqToMongoService, RabbitMqToMongoService>();

var host = builder.Build();

var rabbitService = host.Services.GetRequiredService<IRabbitMqToMongoService>();
rabbitService.StartListening();

host.Run();