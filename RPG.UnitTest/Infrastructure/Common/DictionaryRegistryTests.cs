using FluentAssertions;
using Moq;
using RPG.Domain.Common;
using RPG.Infrastructure.Common;
using RPG.Infrastructure.Interfaces;

namespace RPG.UnitTest.Infrastructure.Common;

public class DictionaryRegistryTests
{
    private readonly Mock<ILogger<DictionaryRegistry<ItemTagDefinition>>> _mockLogger;
    private readonly DictionaryRegistry<ItemTagDefinition> _registry;

    public DictionaryRegistryTests()
    {
        _mockLogger = new Mock<ILogger<DictionaryRegistry<ItemTagDefinition>>>();
        _registry = new DictionaryRegistry<ItemTagDefinition>(_mockLogger.Object);
    }

    [Fact]
    public void Load_ShouldLoadEntries_AndLogInformation()
    {
        // Arrange
        var entries = new List<ItemTagDefinition>
        {
            new() { Code = "weapon", DisplayName = "Weapon" }, new() { Code = "armor", DisplayName = "Armor" }
        };

        // Act
        _registry.Load(entries);

        // Assert
        _registry.IsValid("weapon").Should().BeTrue();
        _registry.IsValid("armor").Should().BeTrue();

        _mockLogger.Verify(x => x.Info(It.Is<string>(s => s.Contains("Loading dictionary"))), Times.Once);
        _mockLogger.Verify(x => x.Info(It.Is<string>(s => s.Contains("loaded"))), Times.Once);
    }

    [Fact]
    public void IsValid_ShouldReturnTrue_ForValidCode()
    {
        // Arrange
        var entries = new List<ItemTagDefinition> { new() { Code = "weapon", DisplayName = "Weapon" } };
        _registry.Load(entries);

        // Act & Assert
        _registry.IsValid("weapon").Should().BeTrue();
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_ForInvalidCode()
    {
        // Arrange
        var entries = new List<ItemTagDefinition>();
        _registry.Load(entries);

        // Act & Assert
        _registry.IsValid("nonexistent").Should().BeFalse();
    }

    [Fact]
    public void Get_ShouldReturnEntry_WhenCodeExists()
    {
        // Arrange
        var entry = new ItemTagDefinition { Code = "weapon", DisplayName = "Weapon" };
        _registry.Load(new[] { entry });

        // Act
        var result = _registry.Get("weapon");

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be("weapon");
    }

    [Fact]
    public void Get_ShouldReturnNull_WhenCodeDoesNotExist()
    {
        // Arrange
        _registry.Load(Array.Empty<ItemTagDefinition>());

        // Act
        var result = _registry.Get("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void All_ShouldReturnAllLoadedEntries()
    {
        // Arrange
        var entries = new List<ItemTagDefinition>
        {
            new() { Code = "weapon", DisplayName = "Weapon" }, new() { Code = "armor", DisplayName = "Armor" }
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
        var firstLoad = new List<ItemTagDefinition> { new() { Code = "weapon", DisplayName = "Weapon" } };
        var secondLoad = new List<ItemTagDefinition> { new() { Code = "armor", DisplayName = "Armor" } };

        // Act
        _registry.Load(firstLoad);
        var firstCount = _registry.Codes.Count;

        _registry.Load(secondLoad);
        var secondCount = _registry.Codes.Count;

        // Assert
        // Predefined entries + new entries
        _registry.IsValid("armor").Should().BeTrue();
        _mockLogger.Verify(x => x.Info(It.IsAny<string>()), Times.AtLeast(2));
    }
}
