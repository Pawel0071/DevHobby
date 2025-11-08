using FluentAssertions;
using Moq;
using RPG.Core.Interfaces;
using RPG.Core.Services.StatsService;
using RPG.Domain.Containers;
using RPG.Domain.Entities;
using RPG.Domain.Enums;
using RPG.Infrastructure.Interfaces;

namespace RPG.UnitTest.Core.StatsServiceTests;

public class StatsServiceTests
{
    private readonly Mock<ILogger<StatsService>> _logger = new();
    private readonly StatsService _service;

    public StatsServiceTests()
    {
        _service = new StatsService(_logger.Object);
    }

    [Fact]
    public void ModifyStats_ShouldApplyModifier_AndUpdateDerivedValues()
    {
        var character = new Character(Guid.NewGuid(), CharacterClass.Warrior)
        {
            Id = Guid.NewGuid(), Name = "TestWarrior"
        };

        // start with base/modified stats zeros
        var modifier = new StatsContainer();
        modifier.Stats[StatsProperty.Vitality] = 2; // affects MaxHealth for Warrior (vitality * 25)
        modifier.Stats[StatsProperty.Strength] = 3; // affects MaxResource for Warrior (strength * 5)

        var result = _service.ModifyStats(character, modifier);

        result.Success.Should().BeTrue();
        // ModifiedStats should reflect added values
        character.ModifiedStats[StatsProperty.Vitality].Should().Be(2);
        character.ModifiedStats[StatsProperty.Strength].Should().Be(3);

        // Strategy should update derived values
        character.MaxHealth.Should().Be(2 * 25);
        character.MaxResource.Should().Be(3 * 5);
    }

    [Fact]
    public void UnModifyStats_ShouldRemoveModifier_AndUpdateDerivedValues()
    {
        var character = new Character(Guid.NewGuid(), CharacterClass.Mage) { Id = Guid.NewGuid(), Name = "TestMage" };

        // prepare initial modified stats
        character.ModifiedStats[StatsProperty.Vitality] = 5;
        character.ModifiedStats[StatsProperty.Intelligence] = 4;

        // apply subtraction
        var modifier = new StatsContainer();
        modifier.Stats[StatsProperty.Vitality] = 2; // will reduce vitality from 5 to 3
        modifier.Stats[StatsProperty.Intelligence] = 1; // intelligence 4->3

        var result = _service.UnModifyStats(character, modifier);

        result.Success.Should().BeTrue();
        character.ModifiedStats[StatsProperty.Vitality].Should().Be(3);
        character.ModifiedStats[StatsProperty.Intelligence].Should().Be(3);

        // For Mage: MaxHealth = vitality * 15, MaxResource = intelligence * 15
        character.MaxHealth.Should().Be(3 * 15);
        character.MaxResource.Should().Be(3 * 15);
    }

    [Fact]
    public void InitStats_ShouldInitializeBaseAndModifiedStats()
    {
        var character = new Character(Guid.NewGuid(), CharacterClass.Druid) { Id = Guid.NewGuid(), Name = "InitTest" };

        // call explicit interface implementation
        var result = ((IStatsService)_service).InitStats(character);

        result.Success.Should().BeTrue();
        // BaseStats should be initialized and copied to ModifiedStats
        character.BaseStats.Values.Should().AllBeEquivalentTo(0);
        character.ModifiedStats.Values.Should().AllBeEquivalentTo(0);
    }
}
