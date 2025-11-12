using Microsoft.Extensions.Logging;
using RPG.Infrastructure.Interfaces;

namespace RedisWarmUp.Services;

/// <summary>
///     Service that loads all documents from MongoDB to Redis cache on startup.
///     Runs ONCE, then signals GameServer readiness.
/// </summary>
public class RedisWarmUpService
{
    private readonly RPG.Infrastructure.Interfaces.ILogger<RedisWarmUpService> _logger;
    private readonly IRedisRepository _redisRepository;
    private readonly IEnumerable<IDocumentWarmUpStrategy> _warmUpStrategies;

    public RedisWarmUpService(
        IRedisRepository redisRepository,
        IEnumerable<IDocumentWarmUpStrategy> warmUpStrategies, RPG.Infrastructure.Interfaces.ILogger<RedisWarmUpService> logger)
    {
        _redisRepository = redisRepository;
        _warmUpStrategies = warmUpStrategies;
        _logger = logger;
    }

    /// <summary>
    ///     Executes warm-up: loads ALL documents from MongoDB to Redis
    /// </summary>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.Info("Starting Redis WarmUp - loading all documents from MongoDB to Redis");

        var startTime = DateTime.UtcNow;
        var totalDocuments = 0;

        try
        {
            foreach (var strategy in _warmUpStrategies)
            {
                totalDocuments += await strategy.WarmUpAsync(_redisRepository, _logger, cancellationToken);
            }

            var duration = DateTime.UtcNow - startTime;
            _logger.Info($"✅ Redis WarmUp COMPLETED: {totalDocuments} documents loaded in {duration.TotalSeconds:F2}s");
        }
        catch (Exception ex)
        {
            _logger.Error( "❌ Redis WarmUp FAILED", ex);
            throw;
        }
    }
}
