using FluentAssertions;
using Moq;
using RPG.Domain.Common;
using RPG.Domain.Containers;
using RPG.Domain.Entities.Items;
using RPG.Domain.Entities.Items.ItemComponent;
using RPG.Domain.Enums;
using RPG.Infrastructure.Common;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Factories;
using RPG.Infrastructure.Interfaces;

namespace RPG.UnitTest.Infrastructure;

public class ItemFactoryTests
{
    private readonly Mock<IDictionaryRegistry<ItemTagDefinition>> _mockTagRegistry;
    private readonly Mock<ILogger<ItemFactory>> _mockLogger;
    private readonly ItemFactory _factory;

    public ItemFactoryTests()
    {
        _mockTagRegistry = new Mock<IDictionaryRegistry<ItemTagDefinition>>();
        _mockLogger = new Mock<ILogger<ItemFactory>>();
        _factory = new ItemFactory(_mockTagRegistry.Object, _mockLogger.Object);
    }

    [Fact]
    public void Create_ShouldCreateItemWithoutComponents_WhenNoTagsRequireComponents()
    {
        // Arrange
        var doc = new ItemDocument
        {
            Id = Guid.NewGuid(),
            Name = "Simple Item",
            TypeCode = "misc_item",
            Tags = new List<string> { "misc" }
        };

        var def = new ItemTypeDefinition
        {
            Code = "misc_item",
            DisplayName = "Miscellaneous Item"
        };

        _mockTagRegistry.Setup(x => x.IsValid("misc")).Returns(false); // no components needed

        // Act
        var item = _factory.Create(doc, def);

        // Assert
        item.Should().NotBeNull();
        item.Id.Should().Be(doc.Id);
        item.Name.Should().Be(doc.Name);
        item.Components.Should().BeEmpty();
        
        _mockLogger.Verify(x => x.Debug(It.Is<string>(s => s.Contains("Creating item"))), Times.Once);
        _mockLogger.Verify(x => x.Debug(It.Is<string>(s => s.Contains("created successfully"))), Times.Once);
    }

    [Fact]
    public void Create_ShouldAddComponents_WhenTagsRequireComponents()
    {
        // Arrange
        var doc = new ItemDocument
        {
            Id = Guid.NewGuid(),
            Name = "Stat Item",
            TypeCode = "weapon",
            Tags = new List<string> { "stats" }, // "stats" triggers StatsComponent
            Modifiers = new Dictionary<StatsProperty, int>
            {
                { StatsProperty.Strength, 10 }
            }
        };

        var def = new ItemTypeDefinition
        {
            Code = "weapon",
            DisplayName = "Weapon"
        };

        _mockTagRegistry.Setup(x => x.IsValid("stats")).Returns(true);

        // Act
        var item = _factory.Create(doc, def);

        // Assert
        item.Should().NotBeNull();
        item.Components.Should().HaveCount(1);
        item.Components.Should().ContainSingle(c => c is StatsComponent);
        
        var statsComponent = item.GetComponent<StatsComponent>();
        statsComponent.Should().NotBeNull();
        statsComponent!.Stats.Should().NotBeNull();
        statsComponent.Stats!.Stats[StatsProperty.Strength].Should().Be(10);
        
        _mockLogger.Verify(x => x.Debug(It.Is<string>(s => s.Contains("Added component"))), Times.Once);
    }

    [Fact]
    public void Create_ShouldHandleMultipleComponents()
    {
        // Arrange
        var doc = new ItemDocument
        {
            Id = Guid.NewGuid(),
            Name = "Complex Item",
            TypeCode = "weapon",
            Tags = new List<string> { "stats", "socketable" },
            Modifiers = new Dictionary<StatsProperty, int>
            {
                { StatsProperty.Strength, 10 }
            },
            SocketNo = 3
        };

        var def = new ItemTypeDefinition
        {
            Code = "weapon",
            DisplayName = "Weapon"
        };

        _mockTagRegistry.Setup(x => x.IsValid("stats")).Returns(true);
        _mockTagRegistry.Setup(x => x.IsValid("socketable")).Returns(true);

        // Act
        var item = _factory.Create(doc, def);

        // Assert
        item.Should().NotBeNull();
        item.Components.Should().HaveCount(2);
        item.Components.Should().Contain(c => c is StatsComponent);
        item.Components.Should().Contain(c => c is SocketComponent);
        
        _mockLogger.Verify(x => x.Debug(It.Is<string>(s => s.Contains("Added component"))), Times.Exactly(2));
    }

    [Fact]
    public void Create_ShouldLogDebugMessages()
    {
        // Arrange
        var doc = new ItemDocument
        {
            Id = Guid.NewGuid(),
            Name = "Test Item",
            TypeCode = "test",
            Tags = new List<string>()
        };

        var def = new ItemTypeDefinition
        {
            Code = "test",
            DisplayName = "Test"
        };

        // Act
        _factory.Create(doc, def);

        // Assert
        _mockLogger.Verify(x => x.Debug(It.Is<string>(s => s.Contains("Creating item from document"))), Times.Once);
        _mockLogger.Verify(x => x.Debug(It.Is<string>(s => s.Contains("Item created successfully"))), Times.Once);
    }
}
