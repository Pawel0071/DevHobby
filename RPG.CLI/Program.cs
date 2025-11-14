using System.CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RPG.Application;
using RPG.CLI.Commands;
using RPG.CLI.FunctionalTests;
using RPG.CLI.Scenarios;
using RPG.Core;
using RPG.Infrastructure;
using RPG.Infrastructure.Helpers;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Mappers;
using RPG.PersistenceService.Handlers;
using RPG.PersistenceService.Services;
using RedisWarmUp.Services;
using RPG.Domain.Models;
using RPG.Domain.Models.Items;
using RPG.Domain.Models.MapObjects;
using RPG.Domain.Models.Npcs;
using RPG.Domain.Models.Quests;
using RPG.Domain.Models.Skills;
using RPG.Infrastructure.Models;
using CharacterServiceClient = RPG.GameServer.Protos.CharacterService.CharacterServiceClient;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.SetBasePath(AppContext.BaseDirectory);
        config.AddJsonFile("appsettings.json", false, true);
        config.AddJsonFile("../RPG.Infrastructure/appsettings.infrastructure.json", true, true);
        config.AddJsonFile("../RPG.Core/appsettings.core.json", true, true);
        config.AddJsonFile("../RPG.Application/appsettings.application.json", true, true);
    })
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        services.AddInfrastructure(configuration, context.HostingEnvironment.ApplicationName);
        services.AddCore(configuration);
        services.AddApplication(configuration);

        var gameServerAddress = configuration.GetValue<string>("GameServer:GrpcAddress") ?? "http://localhost:5124";
        services.AddGrpcClient<CharacterServiceClient>(options =>
        {
            options.Address = new Uri(gameServerAddress);
        });

        // Register document mappers for all entity/document pairs used by ModelRepository.
        services.AddSingleton<IModelMapper<Character, CharacterDocument>, CharacterModelMapper>();
        services.AddSingleton<IModelMapper<Item, ItemDocument>, ItemModelMapper>();
        services.AddSingleton<IModelMapper<Skill, SkillDocument>, SkillModelMapper>();
        services.AddSingleton<IModelMapper<Quest, QuestDocument>, QuestModelMapper>();
        services.AddSingleton<IModelMapper<Npc, NpcDocument>, NpcModelMapper>();
        services.AddSingleton<IModelMapper<Player, PlayerDocument>, PlayerModelMapper>();
        services.AddSingleton<IModelMapper<MapObject, MapObjectDocument>, MapObjectModelMapper>();
        services.AddSingleton<IModelMapper<WorldState, WorldStateDocument>, WorldStateModelMapper>();

        // Persistence strategies mirror the PersistenceService configuration but operate on the in-memory repositories.
        foreach (var mapping in DocumentMappingRegistry.All)
        {
            var documentType = mapping.DocumentType;
            var collectionName = mapping.CollectionName;

            var persistenceStrategyType = typeof(DocumentPersistenceStrategy<>).MakeGenericType(documentType);
            services.AddSingleton<IDocumentPersistenceStrategy>(sp =>
            {
                var repository = sp.GetRequiredService<IMongoRepository>();
                return (IDocumentPersistenceStrategy)Activator.CreateInstance(persistenceStrategyType, repository, collectionName)!;
            });

            var warmUpStrategyType = typeof(DocumentWarmUpStrategy<>).MakeGenericType(documentType);
            services.AddSingleton<RedisWarmUp.Services.IDocumentWarmUpStrategy>(sp =>
            {
                var repository = sp.GetRequiredService<IMongoRepository>();
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
await host.StartAsync();

var services = host.Services;

var endpointLogger = services.GetRequiredService<RPG.Infrastructure.Interfaces.ILogger<Program>>();
var configurationRoot = services.GetRequiredService<IConfiguration>();
LogEndpointConfiguration(endpointLogger, configurationRoot);

// CLI root
var rootCommand = new RootCommand("RPG CLI");

var equipCommand = new EquipCommand(services);
rootCommand.AddCommand(equipCommand.Build());

var functionalTestsCommand = new FunctionalTestsCommand(services);
rootCommand.AddCommand(functionalTestsCommand.Build());

var documentTestsCommand = new DocumentRepositoryCommand(services);
rootCommand.AddCommand(documentTestsCommand.Build());

var characterGrpcCommand = new CharacterGrpcCommand(services);
rootCommand.AddCommand(characterGrpcCommand.Build());

try
{
    await rootCommand.InvokeAsync(args);
}
finally
{
    await host.StopAsync();
}

static void LogEndpointConfiguration(RPG.Infrastructure.Interfaces.ILogger<Program> logger, IConfiguration configuration)
{
    var mongo = RedactCredentials(configuration.GetConnectionString("Mongo"));
    var redis = configuration.GetConnectionString("Redis") ?? "not configured";
    var rabbitSection = configuration.GetSection("RabbitMQ");
    var rabbitHost = rabbitSection.GetValue<string>("Host") ?? "not configured";
    var rabbitPort = rabbitSection.GetValue<int?>("Port")?.ToString() ?? "default";
    var rabbit = $"{rabbitHost}:{rabbitPort}";
    var gameServer = configuration.GetValue<string>("GameServer:GrpcAddress") ?? "http://localhost:5124";
    var otlp = configuration.GetValue<string>("OpenTelemetry:OtlpEndpoint") ?? "not configured";

    logger.Info($"Configured endpoints => GameServer gRPC: {gameServer}, Mongo: {mongo}, Redis: {redis}, RabbitMQ: {rabbit}, OTLP: {otlp}");
}

static string RedactCredentials(string? connectionString)
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return "not configured";
    }

    var schemeSeparatorIndex = connectionString.IndexOf("://", StringComparison.Ordinal);
    var credentialsSeparatorIndex = connectionString.IndexOf('@');

    if (schemeSeparatorIndex >= 0 && credentialsSeparatorIndex > schemeSeparatorIndex)
    {
        var prefix = connectionString[..(schemeSeparatorIndex + 3)];
        var hostPort = connectionString[(credentialsSeparatorIndex + 1)..];
        return prefix + "***@" + hostPort;
    }

    return connectionString;
}

