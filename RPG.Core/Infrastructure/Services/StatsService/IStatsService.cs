using RPG.Core.Domain.Entities;
using RPG.Core.Domain.Interfaces;

namespace RPG.Core.Infrastructure.Services.StatsService;

public interface IStatsService
{
    StatsResult InitStats(Character character);
    StatsResult ModifyStats(Character character, IStatsContainer modifier);
    StatsResult UnModifyStats(Character character, IStatsContainer modifier);
    StatsResult RegenerateStatsAfterLevelUp(Character character);
}