using System.Collections.Generic;
using FluentAssertions;
using Moq;
using RPG.Domain.Common;
using RPG.Domain.Entities.Items.ItemComponent;
using RPG.Infrastructure.Common;
using RPG.Infrastructure.Interfaces;
using Xunit;

namespace RPG.UnitTest.Infrastructure.Common;

public class ItemTagRegistryExtensionsTests
{
    [Fact]
    public void GetRequiredComponents_ShouldReturnOnlyValidMappings()
    {
        var registry = new Mock<IDictionaryRegistry<ItemTagDefinition>>();
        registry.Setup(r => r.IsValid("equippable")).Returns(true);
        registry.Setup(r => r.IsValid("grants:skill")).Returns(false);

        var tags = new[] { "equippable", "grants:skill", "unknown" };

        var components = registry.Object.GetRequiredComponents(tags);

        components.Should().Contain(typeof(EquippableComponent));
        components.Should().NotContain(typeof(SkillGrantComponent));
        registry.Verify(r => r.IsValid("equippable"), Times.Once);
        registry.Verify(r => r.IsValid("grants:skill"), Times.Once);
    }

    [Theory]
    [InlineData("equippable", true, true)]
    [InlineData("socketable", false, false)]
    [InlineData("socketable", true, true)]
    [InlineData("material", false, false)]
    public void IsTagMapped_ShouldRespectRegistryValidation(string tag, bool isValid, bool expected)
    {
        var registry = new Mock<IDictionaryRegistry<ItemTagDefinition>>();
        registry.Setup(r => r.IsValid(tag)).Returns(isValid);

        var mapped = registry.Object.IsTagMapped(tag);

        mapped.Should().Be(expected);
    }
}
