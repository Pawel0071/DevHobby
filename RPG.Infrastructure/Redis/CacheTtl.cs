namespace RPG.Infrastructure.Redis;

/// <summary>
/// Predefiniowane strategie TTL dla różnych typów danych w Redis.
/// </summary>
public static class CacheTtl
{
    // Short-lived cache (5 minutes) - for frequently changing data
    public static TimeSpan Short => TimeSpan.FromMinutes(5);
    
    // Medium-lived cache (1 hour) - for session data
    public static TimeSpan Medium => TimeSpan.FromHours(1);
    
    // Long-lived cache (24 hours) - for dictionary/static data
    public static TimeSpan Long => TimeSpan.FromHours(24);
    
    // Permanent (no expiration) - explicitly set no TTL
    public static TimeSpan? Permanent => null;
    
    // Custom TTL factory
    public static TimeSpan Minutes(int minutes) => TimeSpan.FromMinutes(minutes);
    public static TimeSpan Hours(int hours) => TimeSpan.FromHours(hours);
    public static TimeSpan Days(int days) => TimeSpan.FromDays(days);
}
