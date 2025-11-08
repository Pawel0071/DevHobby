using RPG.Infrastructure.Interfaces;
using RPG.PersistenceService.Service;

namespace PersistenceService;

public class Worker : BackgroundService
{
    private readonly Microsoft.Extensions.Logging.ILogger<Worker> _logger;
    private readonly IRabbitMqToMongoService _rabbitMqService;

    public Worker(Microsoft.Extensions.Logging.ILogger<Worker> logger, IRabbitMqToMongoService rabbitMqService)
    {
        _logger = logger;
        _rabbitMqService = rabbitMqService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Persistence Worker starting at: {time}", DateTimeOffset.Now);

        try
        {
            // Uruchomienie nasłuchiwania na RabbitMQ
            await _rabbitMqService.StartListeningAsync();

            _logger.LogInformation("RabbitMQ listener started successfully");

            // Utrzymanie workera działającego
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

                if (_logger.IsEnabled(LogLevel.Debug))
                    _logger.LogDebug("Worker heartbeat at: {time}", DateTimeOffset.Now);
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Fatal error in Persistence Worker");
            throw;
        }
        finally
        {
            _logger.LogInformation("Persistence Worker stopping at: {time}", DateTimeOffset.Now);
        }
    }
}
