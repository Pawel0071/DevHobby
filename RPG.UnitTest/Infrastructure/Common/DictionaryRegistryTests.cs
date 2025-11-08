using System.Collections.Generic;
using FluentAssertions;
using Moq;
using RPG.Domain.Common;
using RPG.Domain.Enums;
using RPG.Infrastructure.Common;
using RPG.Infrastructure.Interfaces;

namespace RPG.UnitTest.Infrastructure.Common;

public class DictionaryRegistryTests
{
    private readonly Mock<ILogger<DictionaryRegistry<TagDefinition>>> _mockLogger;
    private readonly DictionaryRegistry<TagDefinition> _registry;

    public DictionaryRegistryTests()
    {
        _mockLogger = new Mock<ILogger<DictionaryRegistry<TagDefinition>>>();
        _registry = new DictionaryRegistry<TagDefinition>(_mockLogger.Object);
    }

    [Fact]
    public void Load_ShouldLoadEntries_AndLogInformation()
    {
        // Arrange
        var entries = new List<TagDefinition>
        {
            CreateDefinition("item:test-weapon", "Weapon"),
            CreateDefinition("item:test-armor", "Armor")
        };

        // Act
        _registry.Load(entries);

        // Assert
        _registry.IsValid("item:test-weapon").Should().BeTrue();
        _registry.IsValid("item:test-armor").Should().BeTrue();

        _mockLogger.Verify(x => x.Info(It.Is<string>(s => s.Contains("Loading dictionary"))), Times.Once);
        _mockLogger.Verify(x => x.Info(It.Is<string>(s => s.Contains("loaded"))), Times.Once);
    }

    [Fact]
    public void IsValid_ShouldReturnTrue_ForValidCode()
    {
        // Arrange
        var entries = new List<TagDefinition> { CreateDefinition("item:test-weapon", "Weapon") };
        _registry.Load(entries);

        // Act & Assert
        _registry.IsValid("item:test-weapon").Should().BeTrue();
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_ForInvalidCode()
    {
        // Arrange
        var entries = new List<TagDefinition>();
        _registry.Load(entries);

        // Act & Assert
        _registry.IsValid("nonexistent").Should().BeFalse();
    }

    [Fact]
    public void Get_ShouldReturnEntry_WhenCodeExists()
    {
        // Arrange
        var entry = CreateDefinition("item:test-weapon", "Weapon");
        _registry.Load(new[] { entry });

        // Act
        var result = _registry.Get("item:test-weapon");

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be("item:test-weapon");
    }

    [Fact]
    public void Get_ShouldReturnNull_WhenCodeDoesNotExist()
    {
        // Arrange
        _registry.Load(Array.Empty<TagDefinition>());

        // Act
        var result = _registry.Get("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void All_ShouldReturnAllLoadedEntries()
    {
        // Arrange
        var entries = new List<TagDefinition>
        {
            CreateDefinition("item:test-weapon", "Weapon"),
            CreateDefinition("item:test-armor", "Armor")
        };
        _registry.Load(entries);

        // Act
        var all = _registry.All;

        // Assert
        all.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void Load_ShouldClearPreviousEntries()
    {
        // Arrange
        var firstLoad = new List<TagDefinition> { CreateDefinition("item:test-weapon", "Weapon") };
        var secondLoad = new List<TagDefinition> { CreateDefinition("item:test-armor", "Armor") };

        // Act
        _registry.Load(firstLoad);
        _registry.Load(secondLoad);

        // Assert
        _registry.IsValid("item:test-armor").Should().BeTrue();
        _mockLogger.Verify(x => x.Info(It.IsAny<string>()), Times.AtLeast(2));
    }

    private static TagDefinition CreateDefinition(string code, string name)
    {
        return new TagDefinition
        {
            Code = code,
            DisplayName = name,
            Category = "Test",
            Description = string.Empty,
            Target = TagTarget.Item
        };
    }
}
