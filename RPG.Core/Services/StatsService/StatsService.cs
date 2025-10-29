using Microsoft.Extensions.Logging;
using RPG.Core.Interfaces;
using RPG.Domain.Entities;
using RPG.Domain.Enums;
using RPG.Domain.Interfaces;

namespace RPG.Core.Services.StatsService;

public class StatsService :IStatsService
{
    private readonly ILogger<StatsService> _logger;

    public StatsService (ILogger<StatsService> logger)
    {
        _logger = logger;
    }
    public StatsResult ModifyStats(Character character, IStatsContainer modifier)
    {
        if (character == null)
            return StatsResult.Fail(StatsError.InvalidOperation, "");
        if (modifier == null)
            return StatsResult.Fail(StatsError.InvalidOperation, "");
        
        character.ModifiedStats.Stats.AddStats(modifier.Stats);
        
        var strategy = GetStrategyFor(character);
        strategy.Apply(character);
        
        return StatsResult.Ok(character.ModifiedStats);
    }

    public StatsResult UnModifyStats(Character character, IStatsContainer modifier)
    {
        if (character == null)
            return StatsResult.Fail(StatsError.InvalidOperation, "");
        if (modifier == null)
            return StatsResult.Fail(StatsError.InvalidOperation, "");
        
        character.ModifiedStats.Stats.SubtractStats(modifier.Stats);
        
        var strategy = GetStrategyFor(character);
        strategy.Apply(character);
        
        return StatsResult.Ok(character.ModifiedStats);
    }

    public StatsResult RegenerateStatsAfterLevelUp(Character character)
    {
        throw new NotImplementedException();
    }

    StatsResult IStatsService.InitStats(Character character)
    {
        character.BaseStats.Stats.CreateEmptyStats();
        var strategy = GetStrategyFor(character);
        strategy.Initialize(character);
        character.ModifiedStats.Stats.CopyStatsFrom(character.BaseStats.Stats);
        
        return StatsResult.Ok(character.BaseStats);
    }
    
    private static IStatModifierStrategy GetStrategyFor(Character character)
    {
        return character.Class switch
        {
            CharacterClass.Warrior => new WarriorStatModifierStrategy(),
            CharacterClass.Mage => new MageStatModifierStrategy(),
            CharacterClass.Assassin => new AssassinStatModifierStrategy(),
            CharacterClass.Druid => new DruidStatModifierStrategy(),
            CharacterClass.Monk => new MonkStatModifierStrategy(),
            CharacterClass.Paladin => new PaladinStatModifierStrategy(),
            CharacterClass.Shaman => new ShamanStatModifierStrategy(),
            CharacterClass.Warlock => new WarlockStatModifierStrategy(),
            _ => throw new InvalidOperationException("Unknown character class")
        };
    }
}  

public class WarriorStatModifierStrategy : IStatModifierStrategy
{
    public void Apply(Character character)
    {
        character.MaxHealth = character.ModifiedStats[StatsProperty.Vitality] * 25;
        character.MaxResource = character.ModifiedStats[StatsProperty.Strength] * 5;
    }

    public void Initialize(Character character)
    {
        throw new NotImplementedException();
    }
}

public class MageStatModifierStrategy : IStatModifierStrategy
{
    public void Apply(Character character)
    {
        character.MaxHealth = character.ModifiedStats[StatsProperty.Vitality] * 15;
        character.MaxResource = character.ModifiedStats[StatsProperty.Intelligence] * 15;
    }
    
    public void Initialize(Character character)
    {
        throw new NotImplementedException();
    }
}

public class WarlockStatModifierStrategy : IStatModifierStrategy
{
    public void Apply(Character character)
    {
        character.MaxHealth = character.ModifiedStats[StatsProperty.Vitality] * 20;
        character.MaxResource = character.ModifiedStats[StatsProperty.Intelligence] * 10;
    }
    
    public void Initialize(Character character)
    {
        throw new NotImplementedException();
    }
}

public class DruidStatModifierStrategy : IStatModifierStrategy
{
    public void Apply(Character character)
    {
        character.MaxHealth = character.ModifiedStats[StatsProperty.Vitality] * 15;
        character.MaxResource = character.ModifiedStats[StatsProperty.Wisdom] * 15;
    }
    
    public void Initialize(Character character)
    {
        throw new NotImplementedException();
    }
}

public class MonkStatModifierStrategy : IStatModifierStrategy
{
    public void Apply(Character character)
    {
        character.MaxHealth = character.ModifiedStats[StatsProperty.Vitality] * 10;
        character.MaxResource = character.ModifiedStats[StatsProperty.Wisdom] * 20;
    }
    
    public void Initialize(Character character)
    {
        throw new NotImplementedException();
    }
}

public class PaladinStatModifierStrategy : IStatModifierStrategy
{
    public void Apply(Character character)
    {
        character.MaxHealth = character.ModifiedStats[StatsProperty.Vitality] * 20;
        character.MaxResource = character.ModifiedStats[StatsProperty.Wisdom] * 10;
    }
    
    public void Initialize(Character character)
    {
        throw new NotImplementedException();
    }
}

public class ShamanStatModifierStrategy : IStatModifierStrategy
{
    public void Apply(Character character)
    {
        character.MaxHealth = character.ModifiedStats[StatsProperty.Vitality] * 15;
        character.MaxResource = character.ModifiedStats[StatsProperty.Wisdom] * 15;
    }

    public void Initialize(Character character)
    {
        throw new NotImplementedException();
    }
}

public class AssassinStatModifierStrategy : IStatModifierStrategy
{
    public void Apply(Character character)
    {
        character.MaxHealth = character.ModifiedStats[StatsProperty.Vitality] * 15;
        character.MaxResource = character.ModifiedStats[StatsProperty.Agility] * 15;
    }
    
    public void Initialize(Character character)
    {
        throw new NotImplementedException();
    }
}