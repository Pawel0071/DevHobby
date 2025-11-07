using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;
using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.HealthChecks;

public class RabbitMqHealthCheck : IHealthCheck
{
    private readonly IConnection? _connection;
    private readonly ILogger<RabbitMqHealthCheck> _logger;

    public RabbitMqHealthCheck(ILogger<RabbitMqHealthCheck> logger, IConnection? connection = null)
    {
        _connection = connection;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_connection == null)
            {
                _logger.Debug("RabbitMQ health check: Not configured (using NullPublisher)");
                return Task.FromResult(
                    HealthCheckResult.Healthy("RabbitMQ not configured - using NullPublisher"));
            }

            if (_connection.IsOpen)
            {
                _logger.Debug("RabbitMQ health check: Healthy");
                return Task.FromResult(HealthCheckResult.Healthy("RabbitMQ connection is open"));
            }

            _logger.Warn("RabbitMQ health check: Degraded (connection closed)");
            return Task.FromResult(
                HealthCheckResult.Degraded("RabbitMQ connection is closed"));
        }
        catch (Exception ex)
        {
            _logger.Error("RabbitMQ health check failed", ex);
            return Task.FromResult(
                HealthCheckResult.Unhealthy("RabbitMQ connection failed", ex));
        }
    }
}
