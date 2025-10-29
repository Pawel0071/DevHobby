using FluentAssertions;
using Moq;
using RPG.Core.Interfaces;
using RPG.Core.Services.LevelService;
using RPG.Domain.Entities;
using RPG.Domain.Enums;
using RPG.Domain.Interfaces;
using RPG.Infrastructure.Logger;

namespace RPG.UnitTest.Core.EquipmentSerivce;

public class LevelingServiceTests
{
    private readonly Mock<IStatsService> _statsMock = new();
    private readonly Mock<ISkillService> _skillMock = new();
    private readonly Mock<IExperienceProvider> _xpMock = new();
    private readonly Mock<ILogger<LevelingService>> _loggerMock = new();
    private readonly LevelingService _service;

    public LevelingServiceTests()
    {
        _service = new LevelingService(_statsMock.Object, _skillMock.Object, _xpMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void LevelUp_ShouldSucceed_WhenNotAtMaxLevel()
    {
        var character = CreateCharacter(level: 5);
        _xpMock.Setup(x => x.IsMaxLevel(5)).Returns(false);
        _xpMock.Setup(x => x.GetRequiredExperience(6)).Returns(1000);

        var result = _service.LevelUp(character, amount: 123);

        result.Should().BeEquivalentTo(LevelingResult.Ok());
        character.Level.Should().Be(6);
        character.Experience.Should().Be(123);
        character.ExperienceToNextLevel.Should().Be(1000);

        _statsMock.Verify(s => s.RegenerateStatsAfterLevelUp(character), Times.Once);
        _skillMock.Verify(s => s.AddSkillsAfterLevelUp(character), Times.Once);
    }

    [Fact]
    public void LevelUp_ShouldFail_WhenAtMaxLevel()
    {
        var character = CreateCharacter(level: 99);
        _xpMock.Setup(x => x.IsMaxLevel(99)).Returns(true);

        var result = _service.LevelUp(character);

        result.Success.Should().BeFalse();
        result.Error.Should().Be(LevelingError.AlreadyMaxLevel);
        result.Message.Should().Be("99");
    }

    [Fact]
    public void GrantExperience_ShouldAddXp_WhenNotAtMaxLevel()
    {
        var character = CreateCharacter(level: 10, xp: 100, xpToNext: 500);
        _xpMock.Setup(x => x.IsMaxLevel(10)).Returns(false);

        var result = _service.GrantExperience(character, amount: 200);

        result.Should().BeEquivalentTo(LevelingResult.Ok());
        character.Experience.Should().Be(300);
        character.ExperienceToNextLevel.Should().Be(300);
        character.Level.Should().Be(10);
    }

    [Fact]
    public void GrantExperience_ShouldFail_WhenAtMaxLevel()
    {
        var character = CreateCharacter(level: 99);
        _xpMock.Setup(x => x.IsMaxLevel(99)).Returns(true);

        var result = _service.GrantExperience(character, amount: 500);

        result.Success.Should().BeFalse();
        result.Error.Should().Be(LevelingError.AlreadyMaxLevel);
        result.Message.Should().Be("99");
    }

    [Fact]
    public void GrantExperience_ShouldTriggerLevelUp_WhenXpExceedsThreshold()
    {
        var character = CreateCharacter(level: 10, xp: 900, xpToNext: 100);
        _xpMock.Setup(x => x.IsMaxLevel(10)).Returns(false);
        _xpMock.Setup(x => x.IsMaxLevel(11)).Returns(false);
        _xpMock.Setup(x => x.GetRequiredExperience(11)).Returns(1500);

        var result = _service.GrantExperience(character, amount: 150);

        result.Should().BeEquivalentTo(LevelingResult.Ok());
        character.Level.Should().Be(11);
        character.Experience.Should().Be(50);
        character.ExperienceToNextLevel.Should().Be(1500);

        _statsMock.Verify(s => s.RegenerateStatsAfterLevelUp(character), Times.Once);
        _skillMock.Verify(s => s.AddSkillsAfterLevelUp(character), Times.Once);
    }

    private static Character CreateCharacter(int level = 1, int xp = 100, int xpToNext = 1000) => 
        new( Guid.NewGuid(), CharacterClass.Druid, null, null)
        {
            Id = Guid.NewGuid(),
            Level = level,
            Experience = xp,
            ExperienceToNextLevel = xpToNext,
            Name = "Name"
        };
}