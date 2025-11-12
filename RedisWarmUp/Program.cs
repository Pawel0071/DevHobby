using RPG.Infrastructure;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Helpers;
using RedisWarmUp.Services;

var builder = Host.CreateApplicationBuilder(args);

// Load configuration
builder.Configuration
    .AddJsonFile("appsettings.json", false, true)
    .AddJsonFile("../RPG.Infrastructure/appsettings.infrastructure.json", true, true);

// Register Infrastructure (MongoDB, Redis, Logging, etc.)
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.ApplicationName);

// Register warm-up strategies
foreach (var mapping in DocumentMappingRegistry.All)
{
    // Register warm-up strategy for each mapped document to guarantee Redis has the same coverage as Mongo.
    var documentType = mapping.DocumentType;
    var strategyType = typeof(DocumentWarmUpStrategy<>).MakeGenericType(documentType);
    var collectionName = mapping.CollectionName;

    builder.Services.AddSingleton(typeof(RedisWarmUp.Services.IDocumentWarmUpStrategy), sp =>
    {
        var repository = sp.GetRequiredService<IMongoRepository>();
        return Activator.CreateInstance(strategyType, repository, collectionName)!;
    });
}

// Register RedisWarmUpService
builder.Services.AddSingleton<RedisWarmUpService>();

var host = builder.Build();

// Execute warm-up and exit
var logger = host.Services.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Program>>();

try
{
    logger.LogInformation("🚀 Starting RedisWarmUp service");

    var warmUpService = host.Services.GetRequiredService<RedisWarmUpService>();
    await warmUpService.ExecuteAsync();

    logger.LogInformation("✅ RedisWarmUp completed successfully - exiting");
    return 0; // Success exit code
}
catch (Exception ex)
{
    logger.LogError(ex, "❌ RedisWarmUp failed");
    return 1; // Failure exit code
}
