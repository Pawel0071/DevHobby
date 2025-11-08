using System.Text.Json;
using FluentAssertions;
using Moq;
using RPG.Domain.Entities.Skills;
using RPG.Domain.Entities.Skills.SkillComponents;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Mappers;

namespace RPG.UnitTest.Infrastructure.Mappers;

/// <summary>
///     Tests for SkillDocumentMapper - Skill to/from SkillDocument conversion with all component types
/// </summary>
public class SkillDocumentMapperTests
{
    private readonly SkillDocumentMapper _mapper;

    public SkillDocumentMapperTests()
    {
        var mockLogger = new Mock<ILogger<SkillDocumentMapper>>();
        _mapper = new SkillDocumentMapper(mockLogger.Object);
    }

    [Fact]
    public void ToDocument_ShouldMapBasicSkillProperties()
    {
        // Arrange
        var skill = Skill.Create("Fireball", "Launches a ball of fire");
        skill.IconId = "icon_fireball_01";
    skill.Tags = new HashSet<string> { "fire", "damage", "ranged", "class-mage", "weapon-staff", "resource-mana" };

        // Act
        var document = _mapper.ToDocument(skill);

        // Assert
        document.Id.Should().Be(skill.Id);
        document.Name.Should().Be("Fireball");
        document.Description.Should().Be("Launches a ball of fire");
        document.IconId.Should().Be("icon_fireball_01");
    document.Tags.Should().Contain("fire");
    document.Tags.Should().Contain("damage");
    document.Tags.Should().Contain("class-mage");
    }

    [Fact]
    public void ToDocument_WithDamageComponent_ShouldSerializeComponent()
    {
        // Arrange
        var skill = Skill.Create("Lightning Bolt", "Strikes with lightning");
        var damageComponent = new DamageComponent
        {
            BaseDamage = 75,
            MinDamage = 50,
            MaxDamage = 100,
            DamageType = "Lightning",
            ScalingStat = "intelligence",
            ScalingFactor = 1.5f,
            CanCrit = true,
            CritMultiplier = 2.0f
        };
        skill.Components.Add(damageComponent);

        // Act
        var document = _mapper.ToDocument(skill);

        // Assert
        document.Components.Should().HaveCount(1);
        document.Components[0].Type.Should().Be(nameof(DamageComponent));
        document.Components[0].Data.Should().Contain("75");
        document.Components[0].Data.Should().Contain("Lightning");
        document.Components[0].Data.Should().Contain("intelligence");
    }

    [Fact]
    public void ToDocument_WithHealingComponent_ShouldSerializeComponent()
    {
        // Arrange
        var skill = Skill.Create("Healing Touch", "Heals the target");
        var healingComponent = new HealingComponent
        {
            BaseHealing = 50,
            MinHealing = 30,
            MaxHealing = 60,
            HealingType = "direct",
            ScalingStat = "intelligence",
            ScalingFactor = 1.2f,
            AffectsSelf = true,
            AffectsAllies = true
        };
        skill.Components.Add(healingComponent);

        // Act
        var document = _mapper.ToDocument(skill);

        // Assert
        document.Components.Should().HaveCount(1);
        document.Components[0].Type.Should().Be(nameof(HealingComponent));
        document.Components[0].Data.Should().Contain("50");
        document.Components[0].Data.Should().Contain("direct");
    }

    [Fact]
    public void ToDocument_WithCooldownComponent_ShouldSerializeComponent()
    {
        // Arrange
        var skill = Skill.Create("Ultimate Strike", "Powerful attack");
        var cooldownComponent = new CooldownComponent
        {
            CooldownSeconds = 120,
            UseGlobalCooldown = true,
            GlobalCooldownMs = 1500,
            MaxCharges = 2,
            ChargeRecoverySeconds = 60
        };
        skill.Components.Add(cooldownComponent);

        // Act
        var document = _mapper.ToDocument(skill);

        // Assert
        document.Components.Should().HaveCount(1);
        document.Components[0].Type.Should().Be(nameof(CooldownComponent));
        document.Components[0].Data.Should().Contain("120");
        document.Components[0].Data.Should().Contain("1500");
    }

    [Fact]
    public void ToDocument_WithCastingComponent_ShouldSerializeComponent()
    {
        // Arrange
        var skill = Skill.Create("Arcane Blast", "Channeled spell");
        var castingComponent = new CastingComponent
        {
            CastTimeMs = 3000,
            IsChanneled = true,
            ChannelDurationMs = 5000,
            ChannelTickIntervalMs = 1000,
            CanMoveWhileCasting = false,
            IsInterruptible = true,
            RequiresTarget = true,
            MaxRange = 30.0f,
            MinRange = 5.0f
        };
        skill.Components.Add(castingComponent);

        // Act
        var document = _mapper.ToDocument(skill);

        // Assert
        document.Components.Should().HaveCount(1);
        document.Components[0].Type.Should().Be(nameof(CastingComponent));
        document.Components[0].Data.Should().Contain("3000");
        document.Components[0].Data.Should().Contain("true");
        document.Components[0].Data.Should().Contain("30");
    }

    [Fact]
    public void ToDocument_WithMultipleComponents_ShouldSerializeAll()
    {
        // Arrange
        var skill = Skill.Create("Complex Spell", "Multi-faceted ability");
        skill.Components.Add(new DamageComponent { BaseDamage = 100, MinDamage = 80, MaxDamage = 120, DamageType = "Magic" });
        skill.Components.Add(new CooldownComponent { CooldownSeconds = 30, MaxCharges = 1 });
        skill.Components.Add(new CastingComponent { CastTimeMs = 2000, IsInterruptible = true });

        // Act
        var document = _mapper.ToDocument(skill);

        // Assert
        document.Components.Should().HaveCount(3);
        document.Components.Should().Contain(c => c.Type == nameof(DamageComponent));
        document.Components.Should().Contain(c => c.Type == nameof(CooldownComponent));
        document.Components.Should().Contain(c => c.Type == nameof(CastingComponent));
    }

    [Fact]
    public void ToEntity_ShouldMapBasicSkillProperties()
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var document = new SkillDocument
        {
            Id = skillId,
            Name = "Ice Shard",
            Description = "Launches shards of ice",
            IconId = "icon_ice_01",
            Tags = new List<string> { "ice", "damage", "class-mage", "resource-mana" },
            Components = new List<ComponentData>()
        };

        // Act
        var skill = _mapper.ToEntity(document);

        // Assert
        skill.Id.Should().Be(skillId, "ID should be preserved from document");
        skill.Name.Should().Be("Ice Shard");
        skill.Description.Should().Be("Launches shards of ice");
        skill.IconId.Should().Be("icon_ice_01");
    skill.Tags.Should().Contain("ice");
    skill.Tags.Should().Contain("class-mage");
    }

    [Fact]
    public void ToEntity_WithDamageComponent_ShouldDeserializeComponent()
    {
        // Arrange
        var damageComponent = new DamageComponent
        {
            BaseDamage = 60,
            MinDamage = 40,
            MaxDamage = 80,
            DamageType = "Fire",
            ScalingStat = "strength",
            ScalingFactor = 1.3f,
            CanCrit = true,
            CritMultiplier = 2.5f
        };
        var componentData = new ComponentData
        {
            Type = nameof(DamageComponent),
            Data = JsonSerializer.Serialize(damageComponent)
        };

        var document = new SkillDocument
        {
            Id = Guid.NewGuid(),
            Name = "Fire Blast",
            Description = "Desc",
            IconId = "icon",
            Tags = new List<string>(),
            Components = new List<ComponentData> { componentData }
        };

        // Act
        var skill = _mapper.ToEntity(document);

        // Assert
        skill.Components.Should().HaveCount(1);
        var component = skill.Components[0] as DamageComponent;
        component.Should().NotBeNull();
        component!.BaseDamage.Should().Be(60);
        component.MinDamage.Should().Be(40);
        component.MaxDamage.Should().Be(80);
        component.DamageType.Should().Be("Fire");
        component.ScalingStat.Should().Be("strength");
    }

    [Fact]
    public void ToEntity_WithHealingComponent_ShouldDeserializeComponent()
    {
        // Arrange
        var healingComponent = new HealingComponent
        {
            BaseHealing = 40,
            MinHealing = 20,
            MaxHealing = 50,
            HealingType = "over-time",
            AffectsSelf = false,
            AffectsAllies = true
        };
        var componentData = new ComponentData
        {
            Type = nameof(HealingComponent),
            Data = JsonSerializer.Serialize(healingComponent)
        };

        var document = new SkillDocument
        {
            Id = Guid.NewGuid(),
            Name = "Heal",
            Description = "Desc",
            IconId = "icon",
            Tags = new List<string>(),
            Components = new List<ComponentData> { componentData }
        };

        // Act
        var skill = _mapper.ToEntity(document);

        // Assert
        skill.Components.Should().HaveCount(1);
        var component = skill.Components[0] as HealingComponent;
        component.Should().NotBeNull();
        component!.BaseHealing.Should().Be(40);
        component.HealingType.Should().Be("over-time");
        component.AffectsSelf.Should().BeFalse();
        component.AffectsAllies.Should().BeTrue();
    }

    [Fact]
    public void ToEntity_WithCooldownComponent_ShouldDeserializeComponent()
    {
        // Arrange
        var cooldownComponent = new CooldownComponent
        {
            CooldownSeconds = 45,
            UseGlobalCooldown = false,
            MaxCharges = 3,
            ChargeRecoverySeconds = 30
        };
        var componentData = new ComponentData
        {
            Type = nameof(CooldownComponent),
            Data = JsonSerializer.Serialize(cooldownComponent)
        };

        var document = new SkillDocument
        {
            Id = Guid.NewGuid(),
            Name = "Skill",
            Description = "Desc",
            IconId = "icon",
            Tags = new List<string>(),
            Components = new List<ComponentData> { componentData }
        };

        // Act
        var skill = _mapper.ToEntity(document);

        // Assert
        skill.Components.Should().HaveCount(1);
        var component = skill.Components[0] as CooldownComponent;
        component.Should().NotBeNull();
        component!.CooldownSeconds.Should().Be(45);
        component.MaxCharges.Should().Be(3);
        component.ChargeRecoverySeconds.Should().Be(30);
    }

    [Fact]
    public void ToEntity_WithCastingComponent_ShouldDeserializeComponent()
    {
        // Arrange
        var castingComponent = new CastingComponent
        {
            CastTimeMs = 2500,
            IsChanneled = false,
            CanMoveWhileCasting = true,
            IsInterruptible = false,
            RequiresTarget = true,
            MaxRange = 40.0f
        };
        var componentData = new ComponentData
        {
            Type = nameof(CastingComponent),
            Data = JsonSerializer.Serialize(castingComponent)
        };

        var document = new SkillDocument
        {
            Id = Guid.NewGuid(),
            Name = "Instant Skill",
            Description = "Desc",
            IconId = "icon",
            Tags = new List<string>(),
            Components = new List<ComponentData> { componentData }
        };

        // Act
        var skill = _mapper.ToEntity(document);

        // Assert
        skill.Components.Should().HaveCount(1);
        var component = skill.Components[0] as CastingComponent;
        component.Should().NotBeNull();
        component!.CastTimeMs.Should().Be(2500);
        component.IsChanneled.Should().BeFalse();
        component.CanMoveWhileCasting.Should().BeTrue();
        component.MaxRange.Should().Be(40.0f);
    }

    [Fact]
    public void ToEntity_WithMultipleComponents_ShouldDeserializeAll()
    {
        // Arrange
        var components = new List<ComponentData>
        {
            new() { Type = nameof(DamageComponent), Data = JsonSerializer.Serialize(new DamageComponent { BaseDamage = 50, DamageType = "Frost" }) },
            new() { Type = nameof(CooldownComponent), Data = JsonSerializer.Serialize(new CooldownComponent { CooldownSeconds = 60 }) },
            new() { Type = nameof(CastingComponent), Data = JsonSerializer.Serialize(new CastingComponent { CastTimeMs = 1500 }) }
        };

        var document = new SkillDocument
        {
            Id = Guid.NewGuid(),
            Name = "Complex Skill",
            Description = "Desc",
            IconId = "icon",
            Tags = new List<string>(),
            Components = components
        };

        // Act
        var skill = _mapper.ToEntity(document);

        // Assert
        skill.Components.Should().HaveCount(3);
        skill.Components.OfType<DamageComponent>().Should().HaveCount(1);
        skill.Components.OfType<CooldownComponent>().Should().HaveCount(1);
        skill.Components.OfType<CastingComponent>().Should().HaveCount(1);
    }

    [Fact]
    public void RoundTrip_ShouldPreserveSkillData()
    {
        // Arrange
        var skill = Skill.Create("Test Skill", "Test Description");
        skill.IconId = "test_icon";
        skill.Tags = new HashSet<string> { "test", "skill" };
        skill.Components.Add(new DamageComponent { BaseDamage = 100, MinDamage = 80, MaxDamage = 120, DamageType = "Physical" });
        skill.Components.Add(new CooldownComponent { CooldownSeconds = 15 });

        // Act
        var document = _mapper.ToDocument(skill);
        var roundTrippedSkill = _mapper.ToEntity(document);

        // Assert
        roundTrippedSkill.Name.Should().Be(skill.Name);
        roundTrippedSkill.Description.Should().Be(skill.Description);
        roundTrippedSkill.IconId.Should().Be(skill.IconId);
        roundTrippedSkill.Components.Should().HaveCount(2);
    }
}
