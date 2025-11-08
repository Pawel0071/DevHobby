using FluentAssertions;
using RPG.Infrastructure.Common;

namespace RPG.UnitTest.Infrastructure.Common;

/// <summary>
///     Tests for Common Helper classes - CacheKeyBuilder, DictionaryWarmupService, etc.
/// </summary>
public class CommonHelpersTests
{
    #region CacheKeyBuilder Tests

    [Fact]
    public void CacheKeyBuilder_Character_BuildsCorrectKey()
    {
        // Arrange
        var characterId = Guid.Parse("12345678-1234-1234-1234-123456789012");

        // Act
        var key = CacheKeyBuilder.Character(characterId);

        // Assert
        key.Should().Be("char:12345678-1234-1234-1234-123456789012");
    }

    [Fact]
    public void CacheKeyBuilder_CharacterInventory_BuildsCorrectKey()
    {
        // Arrange
        var characterId = Guid.Parse("12345678-1234-1234-1234-123456789012");

        // Act
        var key = CacheKeyBuilder.CharacterInventory(characterId);

        // Assert
        key.Should().Be("char:12345678-1234-1234-1234-123456789012:inventory");
    }

    [Fact]
    public void CacheKeyBuilder_CharacterStats_BuildsCorrectKey()
    {
        // Arrange
        var characterId = Guid.Parse("12345678-1234-1234-1234-123456789012");

        // Act
        var key = CacheKeyBuilder.CharacterStats(characterId);

        // Assert
        key.Should().Be("char:12345678-1234-1234-1234-123456789012:stats");
    }

    [Fact]
    public void CacheKeyBuilder_Item_BuildsCorrectKey()
    {
        // Arrange
        var itemId = "sword_001";

        // Act
        var key = CacheKeyBuilder.Item(itemId);

        // Assert
        key.Should().Be("item:sword_001");
    }

    [Fact]
    public void CacheKeyBuilder_Session_BuildsCorrectKey()
    {
        // Arrange
        var sessionId = Guid.Parse("87654321-4321-4321-4321-210987654321");

        // Act
        var key = CacheKeyBuilder.Session(sessionId);

        // Assert
        key.Should().Be("session:87654321-4321-4321-4321-210987654321");
    }

    [Fact]
    public void CacheKeyBuilder_Dictionary_BuildsCorrectKey()
    {
        // Arrange
        var dictionaryName = "Items";

        // Act
        var key = CacheKeyBuilder.Dictionary(dictionaryName);

        // Assert
        key.Should().Be("dict:Items");
    }

    [Fact]
    public void CacheKeyBuilder_Custom_WithSinglePart_BuildsCorrectKey()
    {
        // Act
        var key = CacheKeyBuilder.Custom("prefix", "part1");

        // Assert
        key.Should().Be("prefix:part1");
    }

    [Fact]
    public void CacheKeyBuilder_Custom_WithMultipleParts_BuildsCorrectKey()
    {
        // Act
        var key = CacheKeyBuilder.Custom("prefix", "part1", "part2", "part3");

        // Assert
        key.Should().Be("prefix:part1:part2:part3");
    }

    [Fact]
    public void CacheKeyBuilder_Custom_WithMixedTypes_BuildsCorrectKey()
    {
        // Arrange
        var guid = Guid.Parse("12345678-1234-1234-1234-123456789012");
        var number = 42;
        var text = "test";

        // Act
        var key = CacheKeyBuilder.Custom("myprefix", guid, number, text);

        // Assert
        key.Should().Be("myprefix:12345678-1234-1234-1234-123456789012:42:test");
    }

    [Fact]
    public void CacheKeyBuilder_Custom_WithNoParts_ReturnsPrefix()
    {
        // Act
        var key = CacheKeyBuilder.Custom("prefix");

        // Assert
        key.Should().Be("prefix");
    }

    #endregion
}
