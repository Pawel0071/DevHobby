using RPG.Core.Domain.Entities;
using RPG.Core.Domain.Entities.Enums;
using RPG.Core.Domain.Interfaces;

namespace RPG.Core.Infrastructure.Services.StatsService;

public class StatsService :IStatsService
{
    public StatsResult ModifyStats(Character character, IStatsContainer modifier)
    {
        if (character == null)
            return StatsResult.Fail(StatsError.InvalidOperation, "");
        if (modifier == null)
            return StatsResult.Fail(StatsError.InvalidOperation, "");
        
        var modifiedStats = AddStats(character.ModifiedStats.Stats, modifier.Stats);
        
        character.MaxHealth = character.ModifiedStats[StatsProperty.Vitality] * 15;
        character.MaxResource = character.ModifiedStats[StatsProperty.Intelligence] * 10;
        
        return StatsResult.Ok(character.ModifiedStats);
    }

    public StatsResult UnModifyStats(Character character, IStatsContainer modifier)
    {
        if (character == null)
            return StatsResult.Fail(StatsError.InvalidOperation, "");
        if (modifier == null)
            return StatsResult.Fail(StatsError.InvalidOperation, "");
        
        var modifiedStats = SubtractStats(character.ModifiedStats.Stats, modifier.Stats);
        
        character.MaxHealth = character.ModifiedStats[StatsProperty.Vitality] * 15;
        character.MaxResource = character.ModifiedStats[StatsProperty.Intelligence] * 10;
        
        return StatsResult.Ok(character.ModifiedStats);
    }

    public StatsResult RegenerateStatsAfterLevelUp(Character character)
    {
        throw new NotImplementedException();
    }

    StatsResult IStatsService.InitStats(Character character)
    {
        return StatsResult.Ok(character.BaseStats);
    }
    
    private static IDictionary<StatsProperty, int> AddStats(
        IDictionary<StatsProperty, int> a,
        IDictionary<StatsProperty, int> b)
    {
        var result = new Dictionary<StatsProperty, int>();

        foreach (var stat in Enum.GetValues<StatsProperty>())
        {
            var valueA = a.TryGetValue(stat, out var va) ? va : 0;
            var valueB = b.TryGetValue(stat, out var vb) ? vb : 0;
            result[stat] = valueA + valueB;
        }

        return result;
    }
    
    private static IDictionary<StatsProperty, int> SubtractStats(
        IDictionary<StatsProperty, int> a,
        IDictionary<StatsProperty, int> b)
    {
        var result = new Dictionary<StatsProperty, int>();

        foreach (var stat in Enum.GetValues<StatsProperty>())
        {
            var valueA = a.TryGetValue(stat, out var va) ? va : 0;
            var valueB = b.TryGetValue(stat, out var vb) ? vb : 0;
            result[stat] = valueA - valueB;
        }

        return result;
    }
}  