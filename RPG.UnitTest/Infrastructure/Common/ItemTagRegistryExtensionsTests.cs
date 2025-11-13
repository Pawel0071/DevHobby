using System;
using System.Collections.Generic;
using FluentAssertions;
using Moq;
using RPG.Domain.Common;
using RPG.Domain.Enums;
using RPG.Domain.Models.Items.ItemComponent;
using RPG.Infrastructure.Common;
using RPG.Infrastructure.Interfaces;
using Xunit;

namespace RPG.UnitTest.Infrastructure.Common;

public class TagRegistryExtensionsTests
{
    [Fact]
    public void GetRequiredComponents_ShouldResolvePrefixedAndUnprefixedTags()
    {
        var registry = new Mock<IDictionaryRegistry<TagDefinition>>();

        registry.Setup(r => r.Get("equippable")).Returns((TagDefinition?)null);
        registry.Setup(r => r.Get("item:equippable")).Returns(CreateDefinition(
            code: "item:equippable",
            componentType: typeof(EquippableComponent)));

        registry.Setup(r => r.Get("item:socketable")).Returns(CreateDefinition(
            code: "item:socketable",
            componentType: typeof(SocketComponent)));

        registry.Setup(r => r.Get("npc:combat")).Returns(CreateDefinition(
            code: "npc:combat",
            target: TagTarget.Npc,
            componentType: typeof(object))); // placeholder to keep setup simple

        var tags = new[] { "equippable", "item:socketable", "npc:combat" };

        var components = registry.Object.GetRequiredComponents(tags);

        components.Should().BeEquivalentTo(new[] { typeof(EquippableComponent), typeof(SocketComponent) });
    }

    [Fact]
    public void IsTagMapped_ShouldReturnFalse_WhenNoComponentDefined()
    {
        var registry = new Mock<IDictionaryRegistry<TagDefinition>>();
        registry.Setup(r => r.Get("item:material")).Returns(CreateDefinition(
            code: "item:material",
            componentType: null));

        var mapped = registry.Object.IsTagMapped("item:material");

        mapped.Should().BeFalse();
    }

    [Fact]
    public void IsTagMapped_ShouldNormalizeUnprefixedCodes()
    {
        var registry = new Mock<IDictionaryRegistry<TagDefinition>>();

        registry.Setup(r => r.Get("socketable")).Returns((TagDefinition?)null);
        registry.Setup(r => r.Get("item:socketable")).Returns(CreateDefinition(
            code: "item:socketable",
            componentType: typeof(SocketComponent)));

        var mapped = registry.Object.IsTagMapped("socketable");

        mapped.Should().BeTrue();
    }

    private static TagDefinition CreateDefinition(
        string code,
        Type? componentType,
        TagTarget target = TagTarget.Item)
    {
        return new TagDefinition
        {
            Code = code,
            Target = target,
            ComponentType = componentType?.AssemblyQualifiedName,
            DisplayName = code,
            Category = "Test"
        };
    }
}
