using FluentAssertions;
using Moq;
using RPG.Domain.Common;
using RPG.Domain.Containers;
using RPG.Domain.Entities.Items;
using RPG.Domain.Entities.Items.ItemComponent;
using RPG.Domain.Enums;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Mappers;

namespace RPG.UnitTest.Infrastructure;

public class ItemDocumentMapperTests
{
    private readonly Mock<ILogger<ItemDocumentMapper>> _mockLogger;

    public ItemDocumentMapperTests()
    {
        _mockLogger = new Mock<ILogger<ItemDocumentMapper>>();
    }

    [Fact]
    public void ToDocument_ShouldMapBasicProperties()
    {
        // Arrange
        var mapper = new ItemDocumentMapper(logger: _mockLogger.Object);
        var item = new Item(Guid.NewGuid(), "weapon_1h")
        {
            Name = "Test Sword",
            Rarity = ItemRarity.Common,
            RequiredLevel = 5,
            StackSize = 1,
            Tags = new HashSet<string> { "weapon", "melee" }
        };

        // Act
        var doc = mapper.ToDocument(item);

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
        var mapper = new ItemDocumentMapper(logger: _mockLogger.Object);
        var item = new Item(Guid.NewGuid(), "weapon")
        {
            Name = "Stat Weapon"
        };
        
        var statsComponent = new StatsComponent
        {
            Stats = new StatsContainer(new Dictionary<StatsProperty, int>
            {
                { StatsProperty.Strength, 10 },
                { StatsProperty.Dexterity, 5 }
            })
        };
        item.Components.Add(statsComponent);

        // Act
        var doc = mapper.ToDocument(item);

        // Assert
        doc.Modifiers.Should().NotBeNull();
        doc.Modifiers.Should().ContainKey(StatsProperty.Strength);
        doc.Modifiers![StatsProperty.Strength].Should().Be(10);
        doc.Modifiers.Should().ContainKey(StatsProperty.Dexterity);
        doc.Modifiers[StatsProperty.Dexterity].Should().Be(5);
    }

    [Fact]
    public void ToDocument_ShouldMapSocketComponent()
    {
        // Arrange
        var mapper = new ItemDocumentMapper(logger: _mockLogger.Object);
        var item = new Item(Guid.NewGuid(), "weapon")
        {
            Name = "Socketed Weapon"
        };
        
        var socketComponent = new SocketComponent { SocketNo = 3 };
        item.Components.Add(socketComponent);

        // Act
        var doc = mapper.ToDocument(item);

        // Assert
        doc.SocketNo.Should().Be(3);
    }

    [Fact]
    public void ToDomain_ShouldMapBasicProperties()
    {
        // Arrange
        var mapper = new ItemDocumentMapper(logger: _mockLogger.Object);
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
    public void ToDomain_ShouldCreateComponentsFromDefinition()
    {
        // Arrange
        var def = new ItemTypeDefinition
        {
            Code = "weapon",
            DisplayName = "Weapon",
            RequiredComponents = new[] { typeof(StatsComponent) }
        };
        
        var mapper = new ItemDocumentMapper(def, _mockLogger.Object);
        var doc = new ItemDocument
        {
            Id = Guid.NewGuid(),
            Name = "Test Weapon",
            TypeCode = "weapon",
            Modifiers = new Dictionary<StatsProperty, int>
            {
                { StatsProperty.Strength, 15 }
            }
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
        var mapper = new ItemDocumentMapper(logger: _mockLogger.Object);
        var originalItem = new Item(Guid.NewGuid(), "weapon")
        {
            Name = "Roundtrip Test",
            Rarity = ItemRarity.Epic,
            RequiredLevel = 20,
            Tags = new HashSet<string> { "weapon", "legendary" }
        };

        // Act
        var doc = mapper.ToDocument(originalItem);
        var resultItem = mapper.ToDomain(doc);

        // Assert
        resultItem.Id.Should().Be(originalItem.Id);
        resultItem.Name.Should().Be(originalItem.Name);
        resultItem.TypeCode.Should().Be(originalItem.TypeCode);
        resultItem.Rarity.Should().Be(originalItem.Rarity);
        resultItem.RequiredLevel.Should().Be(originalItem.RequiredLevel);
        resultItem.Tags.Should().BeEquivalentTo(originalItem.Tags);
    }
}
