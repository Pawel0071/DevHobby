using RPG.Infrastructure;
using RPG.PersistenceService.Service;
using RPG.PersistenceService.Handlers;
using RPG.PersistenceService.Services;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Helpers;

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
    services.AddInfrastructure(configuration, context.HostingEnvironment.ApplicationName);

        foreach (var mapping in DocumentMappingRegistry.All)
        {
            // Register one persistence strategy per mapping to keep Redis/RabbitMQ/Mongo flow aligned.
            var documentType = mapping.DocumentType;
            var strategyType = typeof(DocumentPersistenceStrategy<>).MakeGenericType(documentType);
            var collectionName = mapping.CollectionName;

            services.AddSingleton(typeof(IDocumentPersistenceStrategy), sp =>
            {
                var repository = sp.GetRequiredService<IMongoRepository>();
                return Activator.CreateInstance(strategyType, repository, collectionName)!;
            });
        }

        // Register MessageHandler
        services.AddSingleton<MessageHandler>();

    // Register RabbitMQ listener service
    services.AddSingleton<IRabbitMqToMongoService, RabbitMqToMongoService>();
    services.AddHostedService<RabbitMqListenerHostedService>();
    });

var host = builder.Build();
await host.RunAsync();

