namespace RPG.Infrastructure.Interfaces;

/// <summary>
/// Service for warming up Redis cache from MongoDB
/// </summary>
public interface IRedisWarmUpService
{
    /// <summary>
    /// Starts the warm-up process - reads from MongoDB and writes to Redis
    /// </summary>
    Task StartWarmUpAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Performs a single warm-up cycle
    /// </summary>
    Task WarmUpCycleAsync(CancellationToken cancellationToken = default);
}
