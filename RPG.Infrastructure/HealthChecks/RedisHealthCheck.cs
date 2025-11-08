using Microsoft.Extensions.Diagnostics.HealthChecks;
using RPG.Infrastructure.Interfaces;
using StackExchange.Redis;

namespace RPG.Infrastructure.HealthChecks;

public class RedisHealthCheck : IHealthCheck
{
    private readonly ILogger<RedisHealthCheck> _logger;
    private readonly IConnectionMultiplexer _redis;

    public RedisHealthCheck(IConnectionMultiplexer redis, ILogger<RedisHealthCheck> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            await db.PingAsync();

            _logger.Debug("Redis health check: Healthy");
            return HealthCheckResult.Healthy("Redis is responsive");
        }
        catch (Exception ex)
        {
            _logger.Error("Redis health check failed", ex);
            return HealthCheckResult.Unhealthy("Redis is not responsive", ex);
        }
    }
}
