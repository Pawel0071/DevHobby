using RPG.Core.Interfaces;
using RPG.Domain.Containers;
using RPG.Domain.Entities;
using RPG.Domain.Enums;
using RPG.Domain.Interfaces;
using RPG.Infrastructure.Interfaces;

namespace RPG.Core.Services.StatsService;

public class StatsService : IStatsService
{
    private readonly ILogger<StatsService> _logger;

    public StatsService(ILogger<StatsService> logger)
    {
        _logger = logger;
    }

    public StatsResult ModifyStats(Character character, IStatsContainer modifier)
    {
        if (character == null)
            return StatsResult.Fail(StatsError.InvalidOperation, "");
        if (modifier == null)
            return StatsResult.Fail(StatsError.InvalidOperation, "");

        character.ModifiedStats.AddStats(modifier.Stats);

        var strategy = GetStrategyFor(character);
        strategy.Apply(character);

        return StatsResult.Ok(new StatsContainer(character.ModifiedStats));
    }

    public StatsResult UnModifyStats(Character character, IStatsContainer modifier)
    {
        if (character == null)
            return StatsResult.Fail(StatsError.InvalidOperation, "");
        if (modifier == null)
            return StatsResult.Fail(StatsError.InvalidOperation, "");

        character.ModifiedStats.SubtractStats(modifier.Stats);

        var strategy = GetStrategyFor(character);
        strategy.Apply(character);

        return StatsResult.Ok(new StatsContainer(character.ModifiedStats));
    }

    public StatsResult RegenerateStatsAfterLevelUp(Character character)
    {
        throw new NotImplementedException();
    }

    StatsResult IStatsService.InitStats(Character character)
    {
        character.BaseStats.CreateEmptyStats();
        var strategy = GetStrategyFor(character);
        strategy.Initialize(character);
        character.ModifiedStats.CopyStatsFrom(character.BaseStats);

        return StatsResult.Ok(new StatsContainer(character.BaseStats));
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
        // Initialize base stats for Warrior - nothing special for now
        // Base stats are created by caller; strategy may adjust defaults in future
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
        // no-op initialization for Mage
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
        // no-op initialization for Warlock
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
        // no-op initialization for Druid
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
        // no-op initialization for Monk
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
        // no-op initialization for Paladin
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
        // no-op initialization for Shaman
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
        // no-op initialization for Assassin
    }
}
