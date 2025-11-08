using System.Text.Json;
using FluentAssertions;
using Moq;
using RPG.Domain.Entities;
using RPG.Domain.Entities.Quests;
using RPG.Domain.Entities.Quests.QuestComponents;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Mappers;

namespace RPG.UnitTest.Infrastructure.Mappers;

/// <summary>
///     Tests for QuestDocumentMapper - Quest to/from QuestDocument conversion with all component types
/// </summary>
public class QuestDocumentMapperTests
{
    private readonly QuestDocumentMapper _mapper;
    private readonly LocationMapper _locationMapper;

    public QuestDocumentMapperTests()
    {
        var mockLogger = new Mock<ILogger<QuestDocumentMapper>>();
        _locationMapper = new LocationMapper(new Mock<ILogger<LocationMapper>>().Object);
        _mapper = new QuestDocumentMapper(mockLogger.Object, _locationMapper);
    }

    [Fact]
    public void ToDocument_ShouldMapBasicQuestProperties()
    {
        // Arrange
        var startLocation = Location.Create(new(10, 20, 30), Guid.NewGuid(), "TestMap", "TestZone");
        var quest = Quest.Create(
            "Kill 10 Rats",
            "Please help us with the rat problem",
            "Village Elder",
            startLocation,
            new HashSet<string> { "starter", "combat" });

        quest.QuestGiverId = Guid.NewGuid();

        // Act
        var document = _mapper.ToDocument(quest);

        // Assert
        document.Id.Should().Be(quest.Id);
        document.Title.Should().Be("Kill 10 Rats");
        document.Description.Should().Be("Please help us with the rat problem");
        document.QuestGiverName.Should().Be("Village Elder");
        document.QuestGiverId.Should().Be(quest.QuestGiverId);
        document.StartLocation.Should().NotBeNull();
        document.Tags.Should().Contain("starter");
        document.Tags.Should().Contain("combat");
    }

    [Fact]
    public void ToDocument_WithKillObjective_ShouldSerializeComponent()
    {
        // Arrange
        var startLocation = Location.Create(new(0, 0, 0), Guid.NewGuid(), "Map", "Zone");
        var quest = Quest.Create("Hunt Quest", "Kill stuff", "Hunter", startLocation, new HashSet<string>());
        
        var killObjective = new KillObjectiveComponent
        {
            TargetNpcId = Guid.NewGuid(),
            TargetNpcName = "Rat",
            RequiredCount = 10,
            CurrentCount = 0
        };
        quest.Components.Add(killObjective);

        // Act
        var document = _mapper.ToDocument(quest);

        // Assert
        document.Components.Should().HaveCount(1);
        document.Components[0].Type.Should().Be(nameof(KillObjectiveComponent));
        document.Components[0].Data.Should().Contain("Rat");
        document.Components[0].Data.Should().Contain("10");
    }

    [Fact]
    public void ToDocument_WithExploreObjective_ShouldSerializeComponent()
    {
        // Arrange
        var startLocation = Location.Create(new(0, 0, 0), Guid.NewGuid(), "Map", "Zone");
        var quest = Quest.Create("Exploration Quest", "Explore", "Explorer", startLocation, new HashSet<string>());
        
        var targetLocation = Location.Create(new(100, 200, 300), Guid.NewGuid(), "Cave", "DarkZone");
        var exploreObjective = new ExploreObjectiveComponent
        {
            TargetLocation = targetLocation,
            LocationName = "Dark Cave",
            ProximityRadius = 50.0f,
            IsVisited = false
        };
        quest.Components.Add(exploreObjective);

        // Act
        var document = _mapper.ToDocument(quest);

        // Assert
        document.Components.Should().HaveCount(1);
        document.Components[0].Type.Should().Be(nameof(ExploreObjectiveComponent));
        document.Components[0].Data.Should().Contain("Dark Cave");
        document.Components[0].Data.Should().Contain("50");
    }

    [Fact]
    public void ToDocument_WithInteractObjective_ShouldSerializeComponent()
    {
        // Arrange
        var startLocation = Location.Create(new(0, 0, 0), Guid.NewGuid(), "Map", "Zone");
        var quest = Quest.Create("Interaction Quest", "Interact", "NPC", startLocation, new HashSet<string>());
        
        var interactObjective = new InteractObjectiveComponent
        {
            TargetObjectId = Guid.NewGuid(),
            TargetObjectName = "Ancient Shrine",
            RequiredInteractions = 3,
            CurrentInteractions = 0
        };
        quest.Components.Add(interactObjective);

        // Act
        var document = _mapper.ToDocument(quest);

        // Assert
        document.Components.Should().HaveCount(1);
        document.Components[0].Type.Should().Be(nameof(InteractObjectiveComponent));
        document.Components[0].Data.Should().Contain("Ancient Shrine");
        document.Components[0].Data.Should().Contain("3");
    }

    [Fact]
    public void ToDocument_WithBasicRewards_ShouldSerializeComponent()
    {
        // Arrange
        var startLocation = Location.Create(new(0, 0, 0), Guid.NewGuid(), "Map", "Zone");
        var quest = Quest.Create("Reward Quest", "Get rewards", "Benefactor", startLocation, new HashSet<string>());
        
        var rewards = new BasicRewardsComponent
        {
            ExperienceReward = 1000,
            GoldReward = 50
        };
        quest.Components.Add(rewards);

        // Act
        var document = _mapper.ToDocument(quest);

        // Assert
        document.Components.Should().HaveCount(1);
        document.Components[0].Type.Should().Be(nameof(BasicRewardsComponent));
        document.Components[0].Data.Should().Contain("1000");
        document.Components[0].Data.Should().Contain("50");
    }

    [Fact]
    public void ToDocument_WithLevelRequirement_ShouldSerializeComponent()
    {
        // Arrange
        var startLocation = Location.Create(new(0, 0, 0), Guid.NewGuid(), "Map", "Zone");
        var quest = Quest.Create("High Level Quest", "For experts", "Master", startLocation, new HashSet<string>());
        
        var requirement = new LevelRequirementComponent { MinLevel = 50, MaxLevel = 60 };
        quest.Components.Add(requirement);

        // Act
        var document = _mapper.ToDocument(quest);

        // Assert
        document.Components.Should().HaveCount(1);
        document.Components[0].Type.Should().Be(nameof(LevelRequirementComponent));
        document.Components[0].Data.Should().Contain("50");
        document.Components[0].Data.Should().Contain("60");
    }

    [Fact]
    public void ToDocument_WithTimeLimitComponent_ShouldSerializeComponent()
    {
        // Arrange
        var startLocation = Location.Create(new(0, 0, 0), Guid.NewGuid(), "Map", "Zone");
        var quest = Quest.Create("Timed Quest", "Hurry", "Timer", startLocation, new HashSet<string>());
        
        var timeLimit = new TimeLimitComponent
        {
            TimeLimitMinutes = 30,
            StartTime = DateTime.UtcNow
        };
        quest.Components.Add(timeLimit);

        // Act
        var document = _mapper.ToDocument(quest);

        // Assert
        document.Components.Should().HaveCount(1);
        document.Components[0].Type.Should().Be(nameof(TimeLimitComponent));
        document.Components[0].Data.Should().Contain("30");
    }

    [Fact]
    public void ToDocument_WithRepeatableComponent_ShouldSerializeComponent()
    {
        // Arrange
        var startLocation = Location.Create(new(0, 0, 0), Guid.NewGuid(), "Map", "Zone");
        var quest = Quest.Create("Daily Quest", "Repeatable", "QuestGiver", startLocation, new HashSet<string>());
        
        var repeatable = new RepeatableQuestComponent
        {
            CooldownHours = 24,
            LastCompletedTime = DateTime.UtcNow.AddHours(-25)
        };
        quest.Components.Add(repeatable);

        // Act
        var document = _mapper.ToDocument(quest);

        // Assert
        document.Components.Should().HaveCount(1);
        document.Components[0].Type.Should().Be(nameof(RepeatableQuestComponent));
        document.Components[0].Data.Should().Contain("24");
    }

    [Fact]
    public void ToDocument_WithMultipleComponents_ShouldSerializeAll()
    {
        // Arrange
        var startLocation = Location.Create(new(0, 0, 0), Guid.NewGuid(), "Map", "Zone");
        var quest = Quest.Create("Complex Quest", "Multi-part", "Quest Master", startLocation, new HashSet<string>());
        
        quest.Components.Add(new KillObjectiveComponent { TargetNpcName = "Dragon", RequiredCount = 1 });
        quest.Components.Add(new BasicRewardsComponent { ExperienceReward = 5000, GoldReward = 100 });
        quest.Components.Add(new LevelRequirementComponent { MinLevel = 60 });

        // Act
        var document = _mapper.ToDocument(quest);

        // Assert
        document.Components.Should().HaveCount(3);
        document.Components.Should().Contain(c => c.Type == nameof(KillObjectiveComponent));
        document.Components.Should().Contain(c => c.Type == nameof(BasicRewardsComponent));
        document.Components.Should().Contain(c => c.Type == nameof(LevelRequirementComponent));
    }

    [Fact]
    public void ToEntity_ShouldMapBasicQuestProperties()
    {
        // Arrange
        var questId = Guid.NewGuid();
        var questGiverId = Guid.NewGuid();
        var document = new QuestDocument
        {
            Id = questId,
            Title = "Test Quest",
            Description = "Test Description",
            QuestGiverName = "Test NPC",
            QuestGiverId = questGiverId,
            StartLocation = new LocationData { X = 10, Y = 20, Z = 30, WorldId = Guid.NewGuid().ToString(), MapId = "Map1", ZoneName = "Zone1" },
            TurnInLocation = null,
            Tags = new List<string> { "test", "quest" },
            Components = new List<ComponentData>()
        };

        // Act
        var quest = _mapper.ToEntity(document);

        // Assert
        quest.Id.Should().Be(questId, "ID should be preserved from document");
        quest.Title.Should().Be("Test Quest");
        quest.Description.Should().Be("Test Description");
        quest.QuestGiverName.Should().Be("Test NPC");
        quest.QuestGiverId.Should().Be(questGiverId);
        quest.Tags.Should().Contain("test");
    }

    [Fact]
    public void ToEntity_WithKillObjective_ShouldDeserializeComponent()
    {
        // Arrange
        var killObjective = new KillObjectiveComponent
        {
            TargetNpcId = Guid.NewGuid(),
            TargetNpcName = "Goblin",
            RequiredCount = 20,
            CurrentCount = 5
        };
        var componentData = new ComponentData
        {
            Type = nameof(KillObjectiveComponent),
            Data = JsonSerializer.Serialize(killObjective)
        };

        var document = new QuestDocument
        {
            Id = Guid.NewGuid(),
            Title = "Kill Quest",
            Description = "Desc",
            QuestGiverName = "NPC",
            StartLocation = new LocationData { X = 0, Y = 0, Z = 0, WorldId = Guid.NewGuid().ToString(), MapId = "Map", ZoneName = "Zone" },
            Tags = new List<string>(),
            Components = new List<ComponentData> { componentData }
        };

        // Act
        var quest = _mapper.ToEntity(document);

        // Assert
        quest.Components.Should().HaveCount(1);
        var component = quest.Components[0] as KillObjectiveComponent;
        component.Should().NotBeNull();
        component!.TargetNpcName.Should().Be("Goblin");
        component.RequiredCount.Should().Be(20);
        component.CurrentCount.Should().Be(5);
    }

    [Fact]
    public void ToEntity_WithExploreObjective_ShouldDeserializeComponent()
    {
        // Arrange
        var targetLocation = Location.Create(new(50, 60, 70), Guid.NewGuid(), "CaveMap", "CaveZone");
        var exploreObjective = new ExploreObjectiveComponent
        {
            TargetLocation = targetLocation,
            LocationName = "Hidden Cave",
            ProximityRadius = 25.0f,
            IsVisited = true
        };
        var componentData = new ComponentData
        {
            Type = nameof(ExploreObjectiveComponent),
            Data = JsonSerializer.Serialize(exploreObjective)
        };

        var document = new QuestDocument
        {
            Id = Guid.NewGuid(),
            Title = "Explore Quest",
            Description = "Desc",
            QuestGiverName = "NPC",
            StartLocation = new LocationData { X = 0, Y = 0, Z = 0, WorldId = Guid.NewGuid().ToString(), MapId = "Map", ZoneName = "Zone" },
            Tags = new List<string>(),
            Components = new List<ComponentData> { componentData }
        };

        // Act
        var quest = _mapper.ToEntity(document);

        // Assert
        quest.Components.Should().HaveCount(1);
        var component = quest.Components[0] as ExploreObjectiveComponent;
        component.Should().NotBeNull();
        component!.LocationName.Should().Be("Hidden Cave");
        component.ProximityRadius.Should().Be(25.0f);
        component.IsVisited.Should().BeTrue();
    }

    [Fact]
    public void ToEntity_WithBasicRewards_ShouldDeserializeComponent()
    {
        // Arrange
        var rewards = new BasicRewardsComponent { ExperienceReward = 2000, GoldReward = 75 };
        var componentData = new ComponentData
        {
            Type = nameof(BasicRewardsComponent),
            Data = JsonSerializer.Serialize(rewards)
        };

        var document = new QuestDocument
        {
            Id = Guid.NewGuid(),
            Title = "Reward Quest",
            Description = "Desc",
            QuestGiverName = "NPC",
            StartLocation = new LocationData { X = 0, Y = 0, Z = 0, WorldId = Guid.NewGuid().ToString(), MapId = "Map", ZoneName = "Zone" },
            Tags = new List<string>(),
            Components = new List<ComponentData> { componentData }
        };

        // Act
        var quest = _mapper.ToEntity(document);

        // Assert
        quest.Components.Should().HaveCount(1);
        var component = quest.Components[0] as BasicRewardsComponent;
        component.Should().NotBeNull();
        component!.ExperienceReward.Should().Be(2000);
        component.GoldReward.Should().Be(75);
    }

    [Fact]
    public void ToEntity_WithLevelRequirement_ShouldDeserializeComponent()
    {
        // Arrange
        var requirement = new LevelRequirementComponent { MinLevel = 30, MaxLevel = 40 };
        var componentData = new ComponentData
        {
            Type = nameof(LevelRequirementComponent),
            Data = JsonSerializer.Serialize(requirement)
        };

        var document = new QuestDocument
        {
            Id = Guid.NewGuid(),
            Title = "Level Quest",
            Description = "Desc",
            QuestGiverName = "NPC",
            StartLocation = new LocationData { X = 0, Y = 0, Z = 0, WorldId = Guid.NewGuid().ToString(), MapId = "Map", ZoneName = "Zone" },
            Tags = new List<string>(),
            Components = new List<ComponentData> { componentData }
        };

        // Act
        var quest = _mapper.ToEntity(document);

        // Assert
        quest.Components.Should().HaveCount(1);
        var component = quest.Components[0] as LevelRequirementComponent;
        component.Should().NotBeNull();
        component!.MinLevel.Should().Be(30);
        component.MaxLevel.Should().Be(40);
    }

    [Fact]
    public void ToEntity_WithMultipleComponents_ShouldDeserializeAll()
    {
        // Arrange
        var components = new List<ComponentData>
        {
            new() { Type = nameof(KillObjectiveComponent), Data = JsonSerializer.Serialize(new KillObjectiveComponent { TargetNpcName = "Boss", RequiredCount = 1 }) },
            new() { Type = nameof(BasicRewardsComponent), Data = JsonSerializer.Serialize(new BasicRewardsComponent { ExperienceReward = 10000 }) },
            new() { Type = nameof(LevelRequirementComponent), Data = JsonSerializer.Serialize(new LevelRequirementComponent { MinLevel = 70 }) }
        };

        var document = new QuestDocument
        {
            Id = Guid.NewGuid(),
            Title = "Epic Quest",
            Description = "Desc",
            QuestGiverName = "NPC",
            StartLocation = new LocationData { X = 0, Y = 0, Z = 0, WorldId = Guid.NewGuid().ToString(), MapId = "Map", ZoneName = "Zone" },
            Tags = new List<string>(),
            Components = components
        };

        // Act
        var quest = _mapper.ToEntity(document);

        // Assert
        quest.Components.Should().HaveCount(3);
        quest.Components.OfType<KillObjectiveComponent>().Should().HaveCount(1);
        quest.Components.OfType<BasicRewardsComponent>().Should().HaveCount(1);
        quest.Components.OfType<LevelRequirementComponent>().Should().HaveCount(1);
    }

    [Fact]
    public void RoundTrip_ShouldPreserveQuestData()
    {
        // Arrange
        var startLocation = Location.Create(new(10, 20, 30), Guid.NewGuid(), "TestMap", "TestZone");
        var quest = Quest.Create("Round Trip Quest", "Testing", "Test NPC", startLocation, new HashSet<string> { "test" });
        quest.Components.Add(new KillObjectiveComponent { TargetNpcName = "Test Monster", RequiredCount = 5 });
        quest.Components.Add(new BasicRewardsComponent { ExperienceReward = 500, GoldReward = 25 });

        // Act
        var document = _mapper.ToDocument(quest);
        var roundTrippedQuest = _mapper.ToEntity(document);

        // Assert
        roundTrippedQuest.Title.Should().Be(quest.Title);
        roundTrippedQuest.Description.Should().Be(quest.Description);
        roundTrippedQuest.QuestGiverName.Should().Be(quest.QuestGiverName);
        roundTrippedQuest.Components.Should().HaveCount(2);
    }
}
