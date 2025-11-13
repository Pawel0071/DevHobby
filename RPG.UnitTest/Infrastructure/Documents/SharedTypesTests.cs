using System;
using FluentAssertions;
using RPG.Infrastructure.Models;
using Xunit;

namespace RPG.UnitTest.Infrastructure.Documents;

public class SharedTypesTests
{
    [Fact]
    public void LocationData_ShouldStoreValues()
    {
        var location = new LocationData
        {
            X = 1.5f,
            Y = 2.5f,
            Z = 3.5f,
            WorldId = "world",
            MapId = "map",
            ZoneName = "zone",
            Rotation = 45f
        };

        location.X.Should().Be(1.5f);
        location.Y.Should().Be(2.5f);
        location.Z.Should().Be(3.5f);
        location.WorldId.Should().Be("world");
        location.MapId.Should().Be("map");
        location.ZoneName.Should().Be("zone");
        location.Rotation.Should().Be(45f);
    }

    [Fact]
    public void InventorySlot_ShouldTrackItemQuantityAndSlot()
    {
        var itemId = Guid.NewGuid();
        var slot = new InventorySlot
        {
            ItemId = itemId,
            Quantity = 5,
            Slot = 3
        };

        slot.ItemId.Should().Be(itemId);
        slot.Quantity.Should().Be(5);
        slot.Slot.Should().Be(3);
    }

    [Fact]
    public void LootEntry_ShouldTrackDropConfig()
    {
        var itemId = Guid.NewGuid();
        var loot = new LootEntry
        {
            ItemId = itemId,
            DropChance = 0.5f,
            MinQuantity = 1,
            MaxQuantity = 4
        };

        loot.ItemId.Should().Be(itemId);
        loot.DropChance.Should().Be(0.5f);
        loot.MinQuantity.Should().Be(1);
        loot.MaxQuantity.Should().Be(4);
    }
}
