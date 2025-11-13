using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using RPG.Domain.Common;
using RPG.Domain.Containers;
using RPG.Domain.Enums;
using RPG.Domain.Models.Items;
using RPG.Domain.Models.Items.ItemComponent;
using RPG.Infrastructure.Common;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Mappers;
using RPG.Infrastructure.Models;

namespace RPG.UnitTest.Infrastructure.Mappers;

public class ItemModelMapperTests
{
    private readonly Mock<ILogger<ItemModelMapper>> _mockLogger;

    public ItemModelMapperTests()
    {
        _mockLogger = new Mock<ILogger<ItemModelMapper>>();
    }

    private ItemModelMapper CreateMapper()
    {
        return new ItemModelMapper(_mockLogger.Object);
    }

    [Fact]
    public void ToDocument_ShouldMapBasicProperties()
    {
        // Arrange
        var mapper = CreateMapper();
        var item = new Item(Guid.NewGuid(), "weapon_1h")
        {
            Name = "Test Sword",
            Rarity = ItemRarity.Common,
            RequiredLevel = 5,
            StackSize = 1,
            Tags = new HashSet<string> { "weapon", "melee" }
        };

        // Act
        var doc = mapper.ToPersistence(item);

        // Assert
        doc.Id.Should().Be(item.Id);
        doc.Name.Should().Be(item.Name);
        doc.TypeCode.Should().Be(item.TypeCode);
        doc.Rarity.Should().Be(item.Rarity);
        doc.RequiredLevel.Should().Be(item.RequiredLevel);
        doc.StackSize.Should().Be(item.StackSize);
        doc.Tags.Should().Contain("weapon");
        doc.Tags.Should().Contain("melee");

        _mockLogger.Verify(x => x.Debug(It.Is<string>(s => s.Contains("Converting Item to ItemDocument"))), Times.Once);
        _mockLogger.Verify(x => x.Debug(It.Is<string>(s => s.Contains("ItemDocument created"))), Times.Once);
    }

    [Fact]
    public void ToDocument_ShouldMapStatsComponent()
    {
        // Arrange
        var mapper = CreateMapper();
        var item = new Item(Guid.NewGuid(), "weapon") { Name = "Stat Weapon" };

        var statsComponent = new StatsComponent
        {
            Stats = new StatsContainer(new Dictionary<StatsProperty, int>
            {
                { StatsProperty.Strength, 10 }, { StatsProperty.Dexterity, 5 }
            })
        };
        item.Components.Add(statsComponent);

        // Act
        var doc = mapper.ToPersistence(item);

        // Assert
        doc.Modifiers.Should().NotBeNull();
        doc.Modifiers.Should().ContainKey(StatsProperty.Strength.ToString());
        doc.Modifiers![StatsProperty.Strength.ToString()].Should().Be(10);
        doc.Modifiers.Should().ContainKey(StatsProperty.Dexterity.ToString());
        doc.Modifiers[StatsProperty.Dexterity.ToString()].Should().Be(5);
    }

    [Fact]
    public void ToDocument_ShouldMapSocketComponent()
    {
        // Arrange
        var mapper = CreateMapper();
        var item = new Item(Guid.NewGuid(), "weapon") { Name = "Socketed Weapon" };

        var socketComponent = new SocketComponent { SocketNo = 3 };
        item.Components.Add(socketComponent);

        // Act
        var doc = mapper.ToPersistence(item);

        // Assert
        doc.SocketNo.Should().Be(3);
    }

    [Fact]
    public void ToDomain_ShouldMapBasicProperties()
    {
        // Arrange
        var mapper = CreateMapper();
        var doc = new ItemDocument
        {
            Id = Guid.NewGuid(),
            Name = "Test Item",
            TypeCode = "misc",
            Rarity = ItemRarity.Rare,
            RequiredLevel = 10,
            StackSize = 5,
            Tags = new List<string> { "misc", "stackable" }
        };

        // Act
        var item = mapper.ToDomain(doc);

        // Assert
        item.Id.Should().Be(doc.Id);
        item.Name.Should().Be(doc.Name);
        item.TypeCode.Should().Be(doc.TypeCode);
        item.Rarity.Should().Be(doc.Rarity);
        item.RequiredLevel.Should().Be(doc.RequiredLevel);
        item.StackSize.Should().Be(doc.StackSize);
        item.Tags.Should().Contain("misc");
        item.Tags.Should().Contain("stackable");

        _mockLogger.Verify(x => x.Debug(It.Is<string>(s => s.Contains("Converting ItemDocument to Item"))), Times.Once);
        _mockLogger.Verify(x => x.Debug(It.Is<string>(s => s.Contains("Item domain entity created"))), Times.Once);
    }

    [Fact]
    public void ToDomain_ShouldCreateComponentsFromTags()
    {
        // Arrange
        var mapper = CreateMapper();
        var doc = new ItemDocument
        {
            Id = Guid.NewGuid(),
            Name = "Test Weapon",
            TypeCode = "weapon",
            Tags = new List<string> { "item:stats" },
            Modifiers = new Dictionary<string, int> { { StatsProperty.Strength.ToString(), 15 } }
        };

        // Act
        var item = mapper.ToDomain(doc);

        // Assert
        item.Components.Should().HaveCount(1);
        var statsComponent = item.GetComponent<StatsComponent>();
        statsComponent.Should().NotBeNull();
        statsComponent!.Stats.Should().NotBeNull();
    }

    [Fact]
    public void Roundtrip_ShouldPreserveData()
    {
        // Arrange
        var mapper = CreateMapper();
        var originalItem = new Item(Guid.NewGuid(), "weapon")
        {
            Name = "Roundtrip Test",
            Rarity = ItemRarity.Epic,
            RequiredLevel = 20,
            Tags = new HashSet<string> { "weapon", "legendary" }
        };

        // Act
        var doc = mapper.ToPersistence(originalItem);
        var resultItem = mapper.ToDomain(doc);

        // Assert
        resultItem.Id.Should().Be(originalItem.Id);
        resultItem.Name.Should().Be(originalItem.Name);
        resultItem.TypeCode.Should().Be(originalItem.TypeCode);
        resultItem.Rarity.Should().Be(originalItem.Rarity);
        resultItem.RequiredLevel.Should().Be(originalItem.RequiredLevel);
        resultItem.Tags.Should().BeEquivalentTo(originalItem.Tags);
    }

    [Fact]
    public void ToDocument_ShouldMapSkillGrantComponent()
    {
        // Arrange
        var mapper = CreateMapper();
        var item = new Item(Guid.NewGuid(), "item_skill") { Name = "Skill Item" };
        var skill1 = Guid.NewGuid();
        var skill2 = Guid.NewGuid();

        var skillComponent = new SkillGrantComponent { SkillIds = new List<Guid> { skill1, skill2 } };
        item.Components.Add(skillComponent);

        // Act
        var doc = mapper.ToPersistence(item);

        // Assert
        doc.SkillIds.Should().NotBeNull();
        doc.SkillIds.Should().HaveCount(2);
        doc.SkillIds.Should().Contain(skill1);
        doc.SkillIds.Should().Contain(skill2);
    }

    [Fact]
    public void ToDocument_ShouldMapQuestItemComponent()
    {
        // Arrange
        var mapper = CreateMapper();
        var item = new Item(Guid.NewGuid(), "quest_item") { Name = "Quest Item" };
        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();

        var questComponent = new QuestItemComponent { QuestId = questId, StepId = stepId };
        item.Components.Add(questComponent);

        // Act
        var doc = mapper.ToPersistence(item);

        // Assert
        doc.QuestId.Should().Be(questId);
        doc.StepId.Should().Be(stepId);
    }

    [Fact]
    public void ToDocument_ShouldMapEquippableComponent()
    {
        // Arrange
        var mapper = CreateMapper();
        var item = new Item(Guid.NewGuid(), "equip_item") { Name = "Equippable Item" };

        var equippable = new EquippableComponent
        {
            ValidSlots = new List<EquipmentSlot> { EquipmentSlot.Weapon1, EquipmentSlot.Weapon2 },
            IsTwoHanded = true,
            SupportsDualWield = false,
            IsUniqueEquip = true
        };

        item.Components.Add(equippable);

        // Act
        var doc = mapper.ToPersistence(item);

        // Assert
    doc.EquipmentSlots.Should().NotBeNull().And.Contain(EquipmentSlot.Weapon1);
        doc.IsTwoHanded.Should().BeTrue();
        doc.SupportsDualWield.Should().BeFalse();
        doc.IsUniqueEquip.Should().BeTrue();
    }

    [Fact]
    public void ToDocument_ShouldMapCraftMaterialComponent()
    {
        // Arrange
        var mapper = CreateMapper();
        var item = new Item(Guid.NewGuid(), "material_item") { Name = "Material" };

        var material = new CraftMaterialComponent
        {
            UsedInItemIds = new List<string> { "recipe-1", "recipe-2" }
        };

        item.Components.Add(material);

        // Act
        var doc = mapper.ToPersistence(item);

        // Assert
        doc.UsedInItemIds.Should().NotBeNull().And.HaveCount(2);
        doc.UsedInItemIds.Should().Contain("recipe-1");
        doc.UsedInItemIds.Should().Contain("recipe-2");
    }

    [Fact]
    public void ToDocument_ShouldMapAllComponents()
    {
        // Arrange
        var mapper = CreateMapper();
        var item = new Item(Guid.NewGuid(), "legendary_weapon") { Name = "Ultimate Weapon" };

        // Add all component types
        item.Components.Add(new StatsComponent
        {
            Stats = new StatsContainer(new Dictionary<StatsProperty, int>
            {
                { StatsProperty.Strength, 50 },
                { StatsProperty.Intelligence, 30 }
            })
        });
        item.Components.Add(new SocketComponent { SocketNo = 6 });
        item.Components.Add(new SkillGrantComponent { SkillIds = new List<Guid> { Guid.NewGuid() } });
        item.Components.Add(new QuestItemComponent { QuestId = Guid.NewGuid(), StepId = Guid.NewGuid() });
        item.Components.Add(new EquippableComponent
        {
            ValidSlots = new List<EquipmentSlot> { EquipmentSlot.Weapon1 },
            IsTwoHanded = true,
            SupportsDualWield = false,
            IsUniqueEquip = false
        });
        item.Components.Add(new CraftMaterialComponent { UsedInItemIds = new List<string> { "legendary-recipe" } });

        // Act
        var doc = mapper.ToPersistence(item);

        // Assert
        doc.Modifiers.Should().NotBeNull().And.HaveCount(2);
        doc.SocketNo.Should().Be(6);
        doc.SkillIds.Should().NotBeNull().And.HaveCount(1);
        doc.QuestId.Should().NotBeNull();
        doc.StepId.Should().NotBeNull();
    doc.EquipmentSlots.Should().NotBeNull().And.Contain(EquipmentSlot.Weapon1);
        doc.IsTwoHanded.Should().BeTrue();
        doc.SupportsDualWield.Should().BeFalse();
        doc.UsedInItemIds.Should().NotBeNull().And.Contain("legendary-recipe");
    }

    [Fact]
    public void ToDocument_WithNoComponents_ShouldNotThrow()
    {
        // Arrange
        var mapper = CreateMapper();
        var item = new Item(Guid.NewGuid(), "simple_item") { Name = "Simple Item" };

        // Act
        var doc = mapper.ToPersistence(item);

        // Assert
        doc.Should().NotBeNull();
        doc.Modifiers.Should().BeNullOrEmpty();
        doc.SocketNo.Should().BeNull();
        doc.SkillIds.Should().BeNullOrEmpty();
        doc.QuestId.Should().BeNull();
        doc.StepId.Should().BeNull();
    doc.EquipmentSlots.Should().BeNullOrEmpty();
    doc.IsTwoHanded.Should().BeNull();
    doc.SupportsDualWield.Should().BeNull();
    doc.IsUniqueEquip.Should().BeNull();
    doc.UsedInItemIds.Should().BeNullOrEmpty();
    }

    [Fact]
    public void ToDomain_WithNullTags_ShouldCreateEmptyHashSet()
    {
        // Arrange
        var mapper = CreateMapper();
        var doc = new ItemDocument
        {
            Id = Guid.NewGuid(),
            Name = "No Tags",
            TypeCode = "misc",
            Tags = new List<string>()
        };

        // Act
        var item = mapper.ToDomain(doc);

        // Assert
        item.Tags.Should().NotBeNull();
        item.Tags.Should().BeEmpty();
    }

    [Fact]
    public void ToDomain_ShouldCreateEquippableComponentFromDocument()
    {
        // Arrange
        var mapper = CreateMapper();
        var doc = new ItemDocument
        {
            Id = Guid.NewGuid(),
            Name = "Equippable Doc",
            TypeCode = "equip",
            EquipmentSlots = new List<EquipmentSlot> { EquipmentSlot.Weapon1 },
            IsTwoHanded = true,
            SupportsDualWield = true,
            IsUniqueEquip = false
        };

        // Act
        var item = mapper.ToDomain(doc);

        // Assert
        var equippable = item.GetComponent<EquippableComponent>();
        equippable.Should().NotBeNull();
        equippable!.ValidSlots.Should().Contain(EquipmentSlot.Weapon1);
        equippable.IsTwoHanded.Should().BeTrue();
        equippable.SupportsDualWield.Should().BeTrue();
        equippable.IsUniqueEquip.Should().BeFalse();
    }

    [Fact]
    public void ToDomain_ShouldCreateCraftMaterialComponentFromDocument()
    {
        // Arrange
        var mapper = CreateMapper();
        var doc = new ItemDocument
        {
            Id = Guid.NewGuid(),
            Name = "Material Doc",
            TypeCode = "material",
            UsedInItemIds = new List<string> { "recipe-1", "recipe-2" }
        };

        // Act
        var item = mapper.ToDomain(doc);

        // Assert
        var material = item.GetComponent<CraftMaterialComponent>();
        material.Should().NotBeNull();
        material!.UsedInItemIds.Should().Contain("recipe-1");
        material.UsedInItemIds.Should().Contain("recipe-2");
    }

    [Fact]
    public void CreateComponent_StatsComponent_ShouldCreateFromModifiers()
    {
        // Arrange
        var doc = new ItemDocument
        {
            Modifiers = new Dictionary<string, int>
            {
                { StatsProperty.Strength.ToString(), 20 },
                { StatsProperty.Vitality.ToString(), 15 }
            }
        };

        // Act
        var component = ItemModelMapper.CreateComponent(typeof(StatsComponent), doc);

        // Assert
        component.Should().NotBeNull();
        component.Should().BeOfType<StatsComponent>();
        var statsComp = (StatsComponent)component!;
        statsComp.Stats.Should().NotBeNull();
        statsComp.Stats.Stats[StatsProperty.Strength].Should().Be(20);
        statsComp.Stats.Stats[StatsProperty.Vitality].Should().Be(15);
    }

    [Fact]
    public void CreateComponent_SocketComponent_ShouldCreateFromSocketNo()
    {
        // Arrange
        var doc = new ItemDocument { SocketNo = 4 };

        // Act
        var component = ItemModelMapper.CreateComponent(typeof(SocketComponent), doc);

        // Assert
        component.Should().NotBeNull();
        component.Should().BeOfType<SocketComponent>();
        ((SocketComponent)component!).SocketNo.Should().Be(4);
    }

    [Fact]
    public void CreateComponent_SkillGrantComponent_ShouldCreateFromSkillIds()
    {
        // Arrange
        var skill1 = Guid.NewGuid();
        var skill2 = Guid.NewGuid();
        var doc = new ItemDocument { SkillIds = new List<Guid> { skill1, skill2 } };

        // Act
        var component = ItemModelMapper.CreateComponent(typeof(SkillGrantComponent), doc);

        // Assert
        component.Should().NotBeNull();
        component.Should().BeOfType<SkillGrantComponent>();
        var skillComp = (SkillGrantComponent)component!;
        skillComp.SkillIds.Should().Contain(skill1);
        skillComp.SkillIds.Should().Contain(skill2);
    }

    [Fact]
    public void CreateComponent_EquippableComponent_ShouldCreateFromDocument()
    {
        // Arrange
        var doc = new ItemDocument
        {
            EquipmentSlots = new List<EquipmentSlot> { EquipmentSlot.Weapon1 },
            IsTwoHanded = false,
            SupportsDualWield = true,
            IsUniqueEquip = true
        };

        // Act
        var component = ItemModelMapper.CreateComponent(typeof(EquippableComponent), doc);

        // Assert
        component.Should().NotBeNull();
        component.Should().BeOfType<EquippableComponent>();
        var equipComp = (EquippableComponent)component!;
        equipComp.ValidSlots.Should().Contain(EquipmentSlot.Weapon1);
        equipComp.IsTwoHanded.Should().BeFalse();
        equipComp.SupportsDualWield.Should().BeTrue();
        equipComp.IsUniqueEquip.Should().BeTrue();
    }

    [Fact]
    public void CreateComponent_CraftMaterialComponent_ShouldCreateFromDocument()
    {
        // Arrange
        var doc = new ItemDocument { UsedInItemIds = new List<string> { "recipe-1" } };

        // Act
        var component = ItemModelMapper.CreateComponent(typeof(CraftMaterialComponent), doc);

        // Assert
        component.Should().NotBeNull();
        component.Should().BeOfType<CraftMaterialComponent>();
        var materialComp = (CraftMaterialComponent)component!;
        materialComp.UsedInItemIds.Should().Contain("recipe-1");
    }

    [Fact]
    public void CreateComponent_QuestItemComponent_ShouldCreateFromQuestData()
    {
        // Arrange
        var questId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var doc = new ItemDocument { QuestId = questId, StepId = stepId };

        // Act
        var component = ItemModelMapper.CreateComponent(typeof(QuestItemComponent), doc);

        // Assert
        component.Should().NotBeNull();
        component.Should().BeOfType<QuestItemComponent>();
        var questComp = (QuestItemComponent)component!;
        questComp.QuestId.Should().Be(questId);
        questComp.StepId.Should().Be(stepId);
    }

    [Fact]
    public void CreateComponent_WithMissingData_ShouldReturnNull()
    {
        // Arrange
        var doc = new ItemDocument(); // Empty document

        // Act
        var statsComp = ItemModelMapper.CreateComponent(typeof(StatsComponent), doc);
        var socketComp = ItemModelMapper.CreateComponent(typeof(SocketComponent), doc);
        var skillComp = ItemModelMapper.CreateComponent(typeof(SkillGrantComponent), doc);
        var questComp = ItemModelMapper.CreateComponent(typeof(QuestItemComponent), doc);
    var equipComp = ItemModelMapper.CreateComponent(typeof(EquippableComponent), doc);
    var craftComp = ItemModelMapper.CreateComponent(typeof(CraftMaterialComponent), doc);

        // Assert
        statsComp.Should().BeNull();
        socketComp.Should().BeNull();
        skillComp.Should().BeNull();
        questComp.Should().BeNull();
    equipComp.Should().BeNull();
    craftComp.Should().BeNull();
    }

    [Fact]
    public void ToDomain_WithDocumentData_ShouldCreateAllAvailable()
    {
        // Arrange
        var mapper = CreateMapper();
        var doc = new ItemDocument
        {
            Id = Guid.NewGuid(),
            Name = "Epic Item",
            TypeCode = "epic_weapon",
            Tags = new List<string> { "item:stats" },
            Modifiers = new Dictionary<string, int> { { StatsProperty.Strength.ToString(), 100 } },
            SocketNo = 2,
            SkillIds = new List<Guid> { Guid.NewGuid() }
        };

        // Act
        var item = mapper.ToDomain(doc);

        // Assert
        item.Components.Should().HaveCount(3);
        item.GetComponent<StatsComponent>().Should().NotBeNull();
        item.GetComponent<SocketComponent>().Should().NotBeNull();
        item.GetComponent<SkillGrantComponent>().Should().NotBeNull();
    }

    [Fact]
    public void ToDomain_WithMixedComponents_ShouldCreateAllAvailable()
    {
        // Arrange
        var mapper = CreateMapper();
        var doc = new ItemDocument
        {
            Id = Guid.NewGuid(),
            Name = "Legendary Item",
            TypeCode = "legendary_item",
            Tags = new List<string> { "item:stats" },
            Modifiers = new Dictionary<string, int> { { StatsProperty.Strength.ToString(), 150 } },
            SocketNo = 3,
            SkillIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() }
        };

        // Act
        var item = mapper.ToDomain(doc);

        // Assert
        item.Components.Should().HaveCount(3);
        item.GetComponent<StatsComponent>().Should().NotBeNull();
        item.GetComponent<SocketComponent>().Should().NotBeNull();
        item.GetComponent<SkillGrantComponent>().Should().NotBeNull();
    }

    [Fact]
    public void ToDocument_WithDifferentRarities_ShouldMapCorrectly()
    {
        // Arrange
        var mapper = CreateMapper();

        // Act & Assert - Test all rarities
        foreach (ItemRarity rarity in Enum.GetValues(typeof(ItemRarity)))
        {
            var item = new Item(Guid.NewGuid(), "test") { Name = "Test", Rarity = rarity };
            var doc = mapper.ToPersistence(item);
            doc.Rarity.Should().Be(rarity);
        }
    }
}
