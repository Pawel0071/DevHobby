namespace RPG.Infrastructure.Configuration;

public class RedisWarmUpSettings
{
    /// <summary>
    /// Collections to read from MongoDB and cache in Redis
    /// </summary>
    public List<string> CollectionsToCache { get; set; } = new()
    {
        "Characters",
        "Items",
        "Skills",
        "Quests",
        "Worlds"
    };

    /// <summary>
    /// Batch size for reading from MongoDB
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Interval between warm-up cycles in seconds
    /// </summary>
    public int IntervalSeconds { get; set; } = 300; // 5 minutes

    /// <summary>
    /// Cache expiry in seconds (0 = no expiry)
    /// </summary>
    public int CacheExpirySeconds { get; set; } = 3600; // 1 hour
}
