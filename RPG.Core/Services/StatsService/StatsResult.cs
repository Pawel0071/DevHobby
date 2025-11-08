using RPG.Domain.Interfaces;

namespace RPG.Core.Services.StatsService;

public enum StatsError
{
    None,
    NoFreeSlot,
    StackLimitReached,
    ItemNotFound,
    InvalidOperation
}

public record StatsResult(bool Success, StatsError Result, IStatsContainer? Stats = null, string? Message = null)
{
    public static StatsResult Ok(IStatsContainer stats)
    {
        return new StatsResult(true, StatsError.None, stats);
    }

    public static StatsResult Fail(StatsError result, string? message = null)
    {
        return new StatsResult(false, result, null, message);
    }
}
