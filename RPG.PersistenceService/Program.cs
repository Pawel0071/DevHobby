using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RPG.Infrastructure;
using RPG.PersistenceService.Service;
using RPG.PersistenceService.Handlers;
using RPG.PersistenceService.Services;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Helpers;

var builder = WebApplication.CreateBuilder(args);

// Konfiguracja
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("../RPG.Infrastructure/appsettings.infrastructure.json", optional: true, reloadOnChange: true)
    .AddJsonFile("../RPG.Core/appsettings.core.json", optional: true, reloadOnChange: true)
    .AddJsonFile("../RPG.Application/appsettings.application.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Rejestracja Infrastructure (telemetria, health checks, redis, rabbit, mongo, outbox, itd.)
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.ApplicationName);

// Rejestracja strategii persystencji dla wszystkich mapowanych dokumentów
foreach (var mapping in DocumentMappingRegistry.All)
{
    var documentType = mapping.DocumentType;
    var strategyType = typeof(DocumentPersistenceStrategy<>).MakeGenericType(documentType);
    var collectionName = mapping.CollectionName;

    builder.Services.AddSingleton(typeof(IDocumentPersistenceStrategy), sp =>
    {
        var repository = sp.GetRequiredService<IMongoRepository>();
        return Activator.CreateInstance(strategyType, repository, collectionName)!;
    });
}

// MessageHandler + worker nasłuchujący RabbitMQ
builder.Services.AddSingleton<MessageHandler>();
builder.Services.AddSingleton<IRabbitMqToMongoService, RabbitMqToMongoService>();
builder.Services.AddHostedService<RabbitMqListenerHostedService>();

var app = builder.Build();

// Prometheus metrics endpoint
app.MapPrometheusScrapingEndpoint();

// Health endpoints
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = _ => true });

await app.RunAsync();
