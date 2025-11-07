using RPG.Infrastructure.Interfaces;

namespace Cache.WormUp.Service;

public interface IMongoToRedisService
{
    Task StartWarmUpAsync();
}

public class MongoToRedisService : IMongoToRedisService
{
    private readonly IRedisWarmUpService _warmUpService;
    private readonly Microsoft.Extensions.Logging.ILogger<MongoToRedisService> _logger;

    public MongoToRedisService(
        IRedisWarmUpService warmUpService,
        Microsoft.Extensions.Logging.ILogger<MongoToRedisService> logger)
    {
        _warmUpService = warmUpService;
        _logger = logger;
    }

    public async Task StartWarmUpAsync()
    {
        _logger.LogInformation("Starting MongoDB to Redis warm-up service");
        await _warmUpService.StartWarmUpAsync();
    }
}
