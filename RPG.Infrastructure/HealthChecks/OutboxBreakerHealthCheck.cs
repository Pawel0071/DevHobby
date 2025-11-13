// filepath: /Volumes/Data/Repositories/DevHobby/RPG.Infrastructure/HealthChecks/OutboxBreakerHealthCheck.cs
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RPG.Infrastructure.Outbox;

namespace RPG.Infrastructure.HealthChecks;

public class OutboxBreakerHealthCheck : IHealthCheck
{
    private readonly IOutboxCircuitBreakerState _state;

    public OutboxBreakerHealthCheck(IOutboxCircuitBreakerState state)
    {
        _state = state;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>
        {
            ["state"] = _state.State,
            ["changedAtUtc"] = _state.ChangedAtUtc,
            ["recentErrorCount"] = _state.RecentErrorCount
        };

        return _state.State switch
        {
            "Closed" => Task.FromResult(new HealthCheckResult(HealthStatus.Healthy, "Outbox breaker closed", null, data)),
            "HalfOpen" => Task.FromResult(new HealthCheckResult(HealthStatus.Degraded, "Outbox breaker half-open", null, data)),
            _ => Task.FromResult(new HealthCheckResult(HealthStatus.Degraded, "Outbox breaker open", null, data))
        };
    }
}
