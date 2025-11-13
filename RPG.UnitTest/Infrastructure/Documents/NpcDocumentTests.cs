using System;
using System.Collections.Generic;
using FluentAssertions;
using RPG.Infrastructure.Models;
using Xunit;

namespace RPG.UnitTest.Infrastructure.Documents;

public class NpcDocumentTests
{
    [Fact]
    public void CollectionName_ShouldBeNpcs()
    {
        NpcDocument.CollectionName.Should().Be("Npcs");
    }

    [Fact]
    public void DefaultConstructor_ShouldInitialiseCollections()
    {
        var document = new NpcDocument
        {
            Id = Guid.NewGuid(),
            Name = "Goblin"
        };

        document.Tags.Should().NotBeNull().And.BeEmpty();
        document.Components.Should().NotBeNull().And.BeEmpty();
        document.SpawnLocation.Should().NotBeNull();
        document.CurrentLocation.Should().NotBeNull();
        document.BaseStats.Should().NotBeNull().And.BeEmpty();
        document.ModifiedStats.Should().NotBeNull().And.BeEmpty();
        document.DisplayName.Should().BeEmpty();
        document.Description.Should().BeEmpty();
    document.IsMoving.Should().BeFalse();
    document.IsRotating.Should().BeFalse();
    }

    [Fact]
    public void ShouldAllowPopulatingAllFields()
    {
        var location = new LocationData
        {
            X = 10,
            Y = 20,
            Z = 30,
            WorldId = "world-1",
            MapId = "map-42",
            ZoneName = "starter-zone",
            Rotation = 90
        };

        var currentLocation = new LocationData
        {
            X = 11,
            Y = 21,
            Z = 31,
            WorldId = "world-2",
            MapId = "map-43",
            ZoneName = "combat-zone",
            Rotation = 45
        };

        var document = new NpcDocument
        {
            Id = Guid.NewGuid(),
            Name = "Arthas",
            DisplayName = "The Lich King",
            Description = "Raid boss",
            Level = 80,
            CurrentHealth = 5000,
            MaxHealth = 5000,
            BaseStats = new Dictionary<string, int> { { "Strength", 100 } },
            ModifiedStats = new Dictionary<string, int> { { "Strength", 120 } },
            Tags = new List<string> { "boss", "undead" },
            Components = new List<ComponentData>
            {
                new() { Type = "ai", Data = "smart" }
            },
            SpawnLocation = location,
            CurrentLocation = currentLocation,
            WorldId = Guid.NewGuid(),
            IsMoving = true,
            IsRotating = false
        };

        document.DisplayName.Should().Be("The Lich King");
        document.Description.Should().Be("Raid boss");
        document.Level.Should().Be(80);
        document.CurrentHealth.Should().Be(5000);
        document.MaxHealth.Should().Be(5000);
        document.Tags.Should().BeEquivalentTo("boss", "undead");
        document.Components.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new ComponentData { Type = "ai", Data = "smart" });
        document.SpawnLocation.Should().BeSameAs(location);
        document.CurrentLocation.Should().BeSameAs(currentLocation);
        document.BaseStats.Should().ContainSingle().And.ContainKey("Strength").WhoseValue.Should().Be(100);
        document.ModifiedStats.Should().ContainSingle().And.ContainKey("Strength").WhoseValue.Should().Be(120);
        document.IsMoving.Should().BeTrue();
        document.IsRotating.Should().BeFalse();
    }
}
