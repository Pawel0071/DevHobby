using FluentAssertions;
using Moq;
using RPG.Domain.Models;
using RPG.Domain.Models.MapObjects;
using RPG.Domain.Models.MapObjects.MapObjectComponents;
using RPG.Domain.Models.Quests;
using RPG.Domain.Models.Quests.QuestComponents;
using RPG.Domain.Models.Skills;
using RPG.Domain.Models.Skills.SkillComponents;
using RPG.Infrastructure.Mappers;
using RPG.Infrastructure.Interfaces;

namespace RPG.UnitTest.Infrastructure.Mappers;

public class TagRoundTripTests
{
    [Fact]
    public void Skill_Tags_ShouldBeCanonicalAfterRoundTrip()
    {
        var logger = new Mock<ILogger<SkillModelMapper>>();
        var mapper = new SkillModelMapper(logger.Object);

        var skill = Skill.Create("Fire Nova", "AOE fire damage");
        skill.Components.Add(new DamageComponent());
        skill.Components.Add(new AreaOfEffectComponent());

        var doc = mapper.ToPersistence(skill);
        var rehydrated = mapper.ToDomain(doc);

        rehydrated.Tags.Should().Contain(t => t.StartsWith("skill:") && (t.Contains("damage") || t.Contains("area-of-effect")));
    }

    [Fact]
    public void Quest_Tags_ShouldBeCanonicalAfterRoundTrip()
    {
        var logger = new Mock<ILogger<QuestModelMapper>>();
        var locationLogger = new Mock<ILogger<LocationMapper>>();
        var questMapper = new QuestModelMapper(logger.Object, new LocationMapper(locationLogger.Object));

        var quest = Quest.Create("Collect Herbs", "Gather 5 herbs", "NPC", new Location(), new System.Collections.Generic.HashSet<string>());
        quest.Components.Add(new CollectObjectiveComponent());
        quest.Components.Add(new BasicRewardsComponent());

        var doc = questMapper.ToPersistence(quest);
        var rehydrated = questMapper.ToDomain(doc);

        rehydrated.Tags.Should().Contain(t => t.StartsWith("quest:") && (t.Contains("collect-objective") || t.Contains("basic-rewards")));
    }

    [Fact]
    public void MapObject_Tags_ShouldBeCanonicalAfterRoundTrip()
    {
        var logger = new Mock<ILogger<MapObjectModelMapper>>();
        var locationLogger = new Mock<ILogger<LocationMapper>>();
        var itemLogger = new Mock<ILogger<ItemModelMapper>>();
        var mapper = new MapObjectModelMapper(logger.Object, new LocationMapper(locationLogger.Object), new ItemModelMapper(itemLogger.Object));

        var mapObject = MapObject.Create("Chest", new Location(), Guid.NewGuid(), "");
        mapObject.Components.Add(new ContainerComponent());
        mapObject.Components.Add(new LockableComponent());

        var doc = mapper.ToPersistence(mapObject);
        var rehydrated = mapper.ToDomain(doc);

        rehydrated.Tags.Should().Contain(t => t.StartsWith("map:") && (t.Contains("container") || t.Contains("lockable")));
    }
}
