using System;
using FluentAssertions;
using Moq;
using RPG.Domain.Entities;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Mappers;
using Xunit;

namespace RPG.UnitTest.Infrastructure.Mappers;

public class WorldStateModelMapperTests
{
    private readonly Mock<ILogger<WorldStateModelMapper>> _logger = new();
    private readonly WorldStateModelMapper _mapper;

    public WorldStateModelMapperTests()
    {
        _mapper = new WorldStateModelMapper(_logger.Object);
    }

    [Fact]
    public void ToDocument_ShouldMapAllFields()
    {
        var worldId = Guid.NewGuid();
        var expectedId = Guid.NewGuid();
        var timestamp = new DateTime(2024, 5, 1, 12, 0, 0, DateTimeKind.Utc);

        var heroId = Guid.NewGuid();
        var npcId = Guid.NewGuid();
        var mapObjectId = Guid.NewGuid();

        var entity = WorldState.Hydrate(
            expectedId,
            worldId,
            "Eora",
            timestamp,
            new[] { heroId },
            new[] { npcId },
            new[] { mapObjectId });

        var document = _mapper.ToPersistence(entity);

        document.Id.Should().Be(expectedId);
        document.WorldId.Should().Be(worldId);
        document.WorldName.Should().Be("Eora");
        document.LastUpdated.Should().Be(entity.LastUpdated);
        document.Characters.Should().ContainSingle(id => id == heroId);
        document.Npcs.Should().ContainSingle(id => id == npcId);
        document.MapObjects.Should().ContainSingle(id => id == mapObjectId);
        _logger.Verify(l => l.Debug(It.Is<string>(msg => msg.Contains("Converting WorldState"))), Times.Once);
    }

    [Fact]
    public void ToEntity_ShouldMapAllFields()
    {
        var characterId = Guid.NewGuid();
        var mapObjectId = Guid.NewGuid();
        var npcId = Guid.NewGuid();
        var worldId = Guid.NewGuid();
        var timestamp = new DateTime(2024, 6, 2, 8, 30, 0, DateTimeKind.Utc);

        var document = new WorldStateDocument
        {
            Id = Guid.NewGuid(),
            WorldId = worldId,
            WorldName = "Eora",
            LastUpdated = timestamp,
            Characters = new List<Guid> { characterId },
            Npcs = new List<Guid> { npcId },
            MapObjects = new List<Guid> { mapObjectId }
        };

        var entity = _mapper.ToEntity(document);

        entity.Id.Should().Be(document.Id);
        entity.WorldId.Should().Be(document.WorldId);
        entity.WorldName.Should().Be("Eora");
        entity.LastUpdated.Should().Be(document.LastUpdated);
        entity.Characters.Should().ContainSingle(id => id == characterId);
        entity.Npcs.Should().ContainSingle(id => id == npcId);
        entity.MapObjects.Should().ContainSingle(id => id == mapObjectId);
        _logger.Verify(l => l.Debug(It.Is<string>(msg => msg.Contains("WorldStateDocument"))), Times.Once);
    }
}
