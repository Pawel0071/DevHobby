using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;
using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.HealthChecks;

public class MongoHealthCheck : IHealthCheck
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<MongoHealthCheck> _logger;

    public MongoHealthCheck(IMongoDatabase database, ILogger<MongoHealthCheck> logger)
    {
        _database = database;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Ping MongoDB
            await _database.RunCommandAsync<BsonDocument>(
                new BsonDocument("ping", 1),
                cancellationToken: cancellationToken);

            _logger.Debug("MongoDB health check: Healthy");
            return HealthCheckResult.Healthy("MongoDB is responsive");
        }
        catch (Exception ex)
        {
            _logger.Error("MongoDB health check failed", ex);
            return HealthCheckResult.Unhealthy("MongoDB is not responsive", ex);
        }
    }
}
