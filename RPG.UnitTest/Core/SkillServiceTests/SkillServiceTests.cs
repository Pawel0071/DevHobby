using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using RPG.Core.Services.SkillService;
using RPG.Domain.Common;
using RPG.Domain.Entities;
using RPG.Domain.Entities.Items;
using RPG.Domain.Entities.Skills;
using RPG.Domain.Entities.Skills.SkillComponents;
using RPG.Domain.Enums;
using RPG.Infrastructure.Interfaces;

namespace RPG.UnitTest.Core.SkillServiceTests;

public class SkillServiceTests
{
    private readonly SkillService _service;
    private readonly Mock<ILogger<SkillService>> _loggerMock = new();

    public SkillServiceTests()
    {
        _service = new SkillService(_loggerMock.Object);
    }

    [Fact]
    public void LearnSkill_ShouldSucceed_WhenRequirementsMet()
    {
        var character = CreateCharacter(CharacterClass.Mage, level: 10, maxResource: 50, currentResource: 50);
        var weapon = CreateWeapon("Arcane Staff", "staff", new[] { "item:weapon:staff" });
        character.Equipments[EquipmentSlot.Weapon1] = weapon;

        var skill = CreateSkill("Arcane Blast", requiredLevel: 5, requiredClass: "Mage", requiredWeaponType: "staff", manaCost: 10);

        var result = _service.LearnSkill(character, skill);

        result.Success.Should().BeTrue();
        character.Skills.Should().ContainKey(skill);
        character.Skills[skill].Should().Be(SkillAvailability.Available);
    }

    [Fact]
    public void LearnSkill_ShouldFail_WhenClassMismatch()
    {
        var character = CreateCharacter(CharacterClass.Warrior, level: 10, maxResource: 50, currentResource: 50);
    var weapon = CreateWeapon("Training Staff", "staff", new[] { "item:weapon:staff" });
        character.Equipments[EquipmentSlot.Weapon1] = weapon;

        var skill = CreateSkill("Arcane Blast", requiredLevel: 5, requiredClass: "Mage", requiredWeaponType: "staff", manaCost: 10);

        var result = _service.LearnSkill(character, skill);

        result.Success.Should().BeFalse();
        result.Error.Should().Be(ErrorCodeDefinition.SkillRequirementClassMismatch);
        character.Skills.Should().BeEmpty();
    }

    [Fact]
    public void LearnSkill_ShouldFail_WhenWeaponMissing()
    {
        var character = CreateCharacter(CharacterClass.Mage, level: 10, maxResource: 50, currentResource: 50);
        var skill = CreateSkill("Arcane Blast", requiredLevel: 5, requiredClass: "Mage", requiredWeaponType: "staff", manaCost: 10);

        var result = _service.LearnSkill(character, skill);

        result.Success.Should().BeFalse();
        result.Error.Should().Be(ErrorCodeDefinition.SkillRequirementWeaponMissing);
        character.Skills.Should().BeEmpty();
    }

    [Fact]
    public void LearnSkill_ShouldFail_WhenResourceInsufficient()
    {
        var character = CreateCharacter(CharacterClass.Mage, level: 10, maxResource: 5, currentResource: 5);
    var weapon = CreateWeapon("Arcane Staff", "staff", new[] { "item:weapon:staff" });
        character.Equipments[EquipmentSlot.Weapon1] = weapon;

        var skill = CreateSkill("Arcane Blast", requiredLevel: 5, requiredClass: "Mage", requiredWeaponType: "staff", manaCost: 10);

        var result = _service.LearnSkill(character, skill);

        result.Success.Should().BeFalse();
        result.Error.Should().Be(ErrorCodeDefinition.SkillRequirementResourceInsufficient);
        character.Skills.Should().BeEmpty();
    }

    [Fact]
    public void UseSkill_ShouldConsumeResource_WhenRequirementsMet()
    {
        var character = CreateCharacter(CharacterClass.Mage, level: 10, maxResource: 50, currentResource: 30);
    var weapon = CreateWeapon("Arcane Staff", "staff", new[] { "item:weapon:staff" });
        character.Equipments[EquipmentSlot.Weapon1] = weapon;

        var skill = CreateSkill("Arcane Blast", requiredLevel: 5, requiredClass: "Mage", requiredWeaponType: "staff", manaCost: 10);
        _service.LearnSkill(character, skill);

        var result = _service.UseSkill(character, skill);

        result.Success.Should().BeTrue();
        character.CurrentResource.Should().Be(20);
        character.ActiveSkills.Should().ContainKey(skill);
    }

    [Fact]
    public void UseSkill_ShouldFail_WhenCurrentResourceTooLow()
    {
        var character = CreateCharacter(CharacterClass.Mage, level: 10, maxResource: 50, currentResource: 15);
    var weapon = CreateWeapon("Arcane Staff", "staff", new[] { "item:weapon:staff" });
        character.Equipments[EquipmentSlot.Weapon1] = weapon;

        var skill = CreateSkill("Arcane Blast", requiredLevel: 5, requiredClass: "Mage", requiredWeaponType: "staff", manaCost: 10);
        _service.LearnSkill(character, skill);
        character.CurrentResource = 5;

        var result = _service.UseSkill(character, skill);

        result.Success.Should().BeFalse();
        result.Error.Should().Be(ErrorCodeDefinition.SkillRequirementResourceInsufficient);
    }

    [Fact]
    public void RemoveSkillsAfterUnEquipItem_ShouldSetSkillUnavailable()
    {
        var character = CreateCharacter(CharacterClass.Mage, level: 10, maxResource: 50, currentResource: 50);
    var weapon = CreateWeapon("Arcane Staff", "staff", new[] { "item:weapon:staff" });
        character.Equipments[EquipmentSlot.Weapon1] = weapon;

        var skill = CreateSkill("Arcane Blast", requiredLevel: 5, requiredClass: "Mage", requiredWeaponType: "staff", manaCost: 10);
        _service.LearnSkill(character, skill);

        character.Equipments[EquipmentSlot.Weapon1] = null!;
        _service.RemoveSkillsAfterUnEquipItem(character, weapon);

        character.Skills[skill].Should().Be(SkillAvailability.UnAvailable);
    }

    [Fact]
    public void AddSkillsAfterEquipItem_ShouldRestoreAvailability()
    {
        var character = CreateCharacter(CharacterClass.Mage, level: 10, maxResource: 50, currentResource: 50);
        var weapon = CreateWeapon("Arcane Staff", "staff", ["item:weapon:staff"]);
        character.Equipments[EquipmentSlot.Weapon1] = weapon;

        var skill = CreateSkill("Arcane Blast", requiredLevel: 5, requiredClass: "Mage", requiredWeaponType: "staff", manaCost: 10);
        _service.LearnSkill(character, skill);

        character.Equipments[EquipmentSlot.Weapon1] = null!;
        _service.RemoveSkillsAfterUnEquipItem(character, weapon);

        character.Equipments[EquipmentSlot.Weapon1] = weapon;
        _service.AddSkillsAfterEquipItem(character, weapon);

        character.Skills[skill].Should().Be(SkillAvailability.Available);
    }

    [Fact]
    public void AddSkillsAfterLevelUp_ShouldEnableSkill_WhenLevelRequirementMet()
    {
        var character = CreateCharacter(CharacterClass.Mage, level: 4, maxResource: 50, currentResource: 50);
        var weapon = CreateWeapon("Arcane Staff", "staff", ["item:weapon:staff"]);
        character.Equipments[EquipmentSlot.Weapon1] = weapon;

        var skill = CreateSkill("Arcane Blast", requiredLevel: 5, requiredClass: "Mage", requiredWeaponType: "staff", manaCost: 10);
        character.GetSkillsContainer().Skills[skill] = SkillAvailability.UnAvailable;

        character.Level = 5;
        _service.AddSkillsAfterLevelUp(character);

        character.Skills[skill].Should().Be(SkillAvailability.Available);
    }

    [Fact]
    public void UnlearnSkill_ShouldRemoveSkillFromContainer()
    {
        var character = CreateCharacter(CharacterClass.Mage, level: 10, maxResource: 50, currentResource: 50);
        var weapon = CreateWeapon("Arcane Staff", "staff", ["item:weapon:staff"]);
        character.Equipments[EquipmentSlot.Weapon1] = weapon;

        var skill = CreateSkill("Arcane Blast", requiredLevel: 5, requiredClass: "Mage", requiredWeaponType: "staff", manaCost: 10);
        _service.LearnSkill(character, skill);

        var result = _service.UnlearnSkill(character, skill);

        result.Success.Should().BeTrue();
        character.Skills.Should().NotContainKey(skill);
        character.ActiveSkills.Should().NotContainKey(skill);
    }

    private static Character CreateCharacter(CharacterClass characterClass, int level, int maxResource, int currentResource)
    {
        var character = new Character(Guid.NewGuid(), characterClass)
        {
            Id = Guid.NewGuid(),
            Name = "Tester",
            Level = level,
            MaxResource = maxResource,
            CurrentResource = currentResource
        };

        return character;
    }

    private static Item CreateWeapon(string name, string typeCode, IEnumerable<string>? tags = null)
    {
        return new Item(Guid.NewGuid(), typeCode)
        {
            Id = Guid.NewGuid(),
            Name = name,
            TypeCode = typeCode,
            Tags = new HashSet<string> { "item:equippable" }
                .Union(tags ?? Array.Empty<string>())
                .ToHashSet()
        };
    }

    private static Skill CreateSkill(
        string name,
        int requiredLevel,
        string? requiredClass,
        string? requiredWeaponType,
        int manaCost)
    {
        var skill = Skill.Create(name, $"{name} description");

        if (!string.IsNullOrWhiteSpace(requiredClass))
        {
            skill.Tags.Add($"class-{requiredClass!.ToLowerInvariant()}");
        }

        if (!string.IsNullOrWhiteSpace(requiredWeaponType))
        {
            skill.Tags.Add($"weapon-{requiredWeaponType!.ToLowerInvariant()}");
        }

        var requirement = new RequirementComponent
        {
            RequiredLevel = requiredLevel
        };

        if (!string.IsNullOrWhiteSpace(requiredClass))
        {
            requirement.RequiredClasses.Add(requiredClass);
        }

        if (!string.IsNullOrWhiteSpace(requiredWeaponType))
        {
            requirement.RequiredWeaponTypes.Add(requiredWeaponType);
        }

        skill.Components.Add(requirement);

        if (manaCost > 0)
        {
            var resource = new ResourceCostComponent
            {
                Costs = new Dictionary<string, int> { ["mana"] = manaCost },
                ConsumeOnCast = true
            };

            skill.Components.Add(resource);
            skill.Tags.Add("resource-mana");
        }

        return skill;
    }
}
