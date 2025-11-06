using System;
using System.Collections.Generic;
using FluentAssertions;
using RPG.Domain.Common;
using RPG.Domain.Entities.Items;
using RPG.Domain.Enums;
using RPG.Infrastructure.Documents;
using Xunit;

namespace RPG.UnitTest.InfrastructureTests;

public class ItemDocumentExtensionsTests
{
    [Fact]
    public void ToDocument_And_ToDomain_Roundtrip_BasicFields()
    {
        var domainItem = new Item(Guid.NewGuid(), "weapon_1h")
        {
            Name = "Test Sword",
            Rarity = ItemRarity.Common,
            RequiredLevel = 3,
            StackSize = 1,
            Tags = new HashSet<string>{ "melee", "onehand" }
        };

        var doc = domainItem.ToDocument();

        doc.Id.Should().Be(domainItem.Id);
        doc.Name.Should().Be(domainItem.Name);
        doc.TypeCode.Should().Be(domainItem.TypeCode);
        doc.Rarity.Should().Be(domainItem.Rarity);
        doc.RequiredLevel.Should().Be(domainItem.RequiredLevel);
        doc.StackSize.Should().Be(domainItem.StackSize);
        doc.Tags.Should().Contain("melee");

        // Convert back to domain (no type def => no components)
        var back = doc.ToDomain(null);

        back.Id.Should().Be(domainItem.Id);
        back.Name.Should().Be(domainItem.Name);
        back.TypeCode.Should().Be(domainItem.TypeCode);
        back.Rarity.Should().Be(domainItem.Rarity);
        back.RequiredLevel.Should().Be(domainItem.RequiredLevel);
    }
}
