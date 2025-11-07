using Cache.WormUp.Service;

namespace Cache.WormUp;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IMongoToRedisService _mongoToRedisService;

    public Worker(
        ILogger<Worker> logger,
        IMongoToRedisService mongoToRedisService)
    {
        _logger = logger;
        _mongoToRedisService = mongoToRedisService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Redis WarmUp Worker starting at: {time}", DateTimeOffset.Now);

        try
        {
            await _mongoToRedisService.StartWarmUpAsync();
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Fatal error in Redis WarmUp Worker");
            throw;
        }
        finally
        {
            _logger.LogInformation("Redis WarmUp Worker stopped at: {time}", DateTimeOffset.Now);
        }
    }
}
