using System.CommandLine;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RPG.Application;
using RPG.Application.Handlers;
using RPG.CLI.Commands;
using RPG.CLI.FunctionalTests;
using RPG.CLI.Scenarios;
using RPG.Core;
using RPG.Domain.Entities;
using RPG.Domain.Entities.Items;
using RPG.Domain.Entities.MapObjects;
using RPG.Domain.Entities.Npcs;
using RPG.Domain.Entities.Quests;
using RPG.Domain.Entities.Skills;
using RPG.Infrastructure;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Helpers;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Mappers;
using RPG.PersistenceService.Handlers;
using RPG.PersistenceService.Services;
using RedisWarmUp.Services;

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

        services.AddSingleton<LocationMapper>();

        // Register document mappers for all entity/document pairs used by DocumentRepository.
        services.AddSingleton<IDocumentMapper<Character, CharacterDocument>, CharacterDocumentMapper>();
        services.AddSingleton<IDocumentMapper<Item, ItemDocument>, ItemDocumentMapper>();
        services.AddSingleton<IDocumentMapper<Skill, SkillDocument>, SkillDocumentMapper>();
        services.AddSingleton<IDocumentMapper<Quest, QuestDocument>, QuestDocumentMapper>();
        services.AddSingleton<IDocumentMapper<Npc, NpcDocument>, NpcDocumentMapper>();
        services.AddSingleton<IDocumentMapper<Player, PlayerDocument>, PlayerDocumentMapper>();
        services.AddSingleton<IDocumentMapper<MapObject, MapObjectDocument>, MapObjectDocumentMapper>();
        services.AddSingleton<IDocumentMapper<WorldState, WorldStateDocument>, WorldStateDocumentMapper>();

        // Persistence strategies mirror the PersistenceService configuration but operate on the in-memory repositories.
        foreach (var mapping in DocumentMappingRegistry.All)
        {
            var documentType = mapping.DocumentType;
            var collectionName = mapping.CollectionName;

            var persistenceStrategyType = typeof(DocumentPersistenceStrategy<>).MakeGenericType(documentType);
            services.AddSingleton<IDocumentPersistenceStrategy>(sp =>
            {
                var repository = sp.GetRequiredService<IMongoDocumentRepository>();
                return (IDocumentPersistenceStrategy)Activator.CreateInstance(persistenceStrategyType, repository, collectionName)!;
            });

            var warmUpStrategyType = typeof(DocumentWarmUpStrategy<>).MakeGenericType(documentType);
            services.AddSingleton<RedisWarmUp.Services.IDocumentWarmUpStrategy>(sp =>
            {
                var repository = sp.GetRequiredService<IMongoDocumentRepository>();
                return (RedisWarmUp.Services.IDocumentWarmUpStrategy)Activator.CreateInstance(warmUpStrategyType, repository, collectionName)!;
            });
        }

        services.AddSingleton<MessageHandler>();
        services.AddSingleton<FunctionalTestRabbitMqPublisher>();
        services.AddSingleton<IRabbitMqPublisher>(sp => sp.GetRequiredService<FunctionalTestRabbitMqPublisher>());
        services.AddSingleton<FunctionalTestRunner>();
        services.AddSingleton<DocumentRepositoryScenarioRunner>();
    });

using var host = builder.Build();
var services = host.Services;

// CLI root
var rootCommand = new RootCommand("RPG CLI");

var equipCommand = new EquipCommand(services);
rootCommand.AddCommand(equipCommand.Build());

var functionalTestsCommand = new FunctionalTestsCommand(services);
rootCommand.AddCommand(functionalTestsCommand.Build());

var documentTestsCommand = new DocumentRepositoryCommand(services);
rootCommand.AddCommand(documentTestsCommand.Build());

await rootCommand.InvokeAsync(args);
