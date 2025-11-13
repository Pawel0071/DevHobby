using RPG.Core.Services.StatsService;
using RPG.Domain.Interfaces;
using RPG.Domain.Models;

namespace RPG.Core.Interfaces;

public interface IStatsService
{
    StatsResult InitStats(Character character);
    StatsResult ModifyStats(Character character, IStatsContainer modifier);
    StatsResult UnModifyStats(Character character, IStatsContainer modifier);
    StatsResult RegenerateStatsAfterLevelUp(Character character);
}
