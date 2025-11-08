using System;
using FluentAssertions;
using Moq;
using RPG.Domain.Entities;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Mappers;
using Xunit;

namespace RPG.UnitTest.Infrastructure.Mappers;

public class WorldStateDocumentMapperTests
{
    private readonly Mock<ILogger<WorldStateDocumentMapper>> _logger = new();
    private readonly WorldStateDocumentMapper _mapper;

    public WorldStateDocumentMapperTests()
    {
        _mapper = new WorldStateDocumentMapper(_logger.Object);
    }

    [Fact]
    public void ToDocument_ShouldMapAllFields()
    {
        var worldId = Guid.NewGuid();
        var expectedId = Guid.NewGuid();
        var entity = WorldState.Create(worldId, "Eora");
        typeof(WorldState).GetProperty("Id")!.SetValue(entity, expectedId);
        entity.LastUpdated = new DateTime(2024, 5, 1, 12, 0, 0, DateTimeKind.Utc);

        var document = _mapper.ToDocument(entity);

        document.Id.Should().Be(expectedId);
        document.WorldId.Should().Be(worldId);
        document.WorldName.Should().Be("Eora");
        document.LastUpdated.Should().Be(entity.LastUpdated);
        _logger.Verify(l => l.Debug(It.Is<string>(msg => msg.Contains("Converting WorldState"))), Times.Once);
    }

    [Fact]
    public void ToEntity_ShouldMapAllFields()
    {
        var document = new WorldStateDocument
        {
            Id = Guid.NewGuid(),
            WorldId = Guid.NewGuid(),
            WorldName = "Eora",
            LastUpdated = new DateTime(2024, 6, 2, 8, 30, 0, DateTimeKind.Utc)
        };

        var entity = _mapper.ToEntity(document);

        entity.Id.Should().Be(document.Id);
        entity.WorldId.Should().Be(document.WorldId);
        entity.WorldName.Should().Be("Eora");
        entity.LastUpdated.Should().Be(document.LastUpdated);
        _logger.Verify(l => l.Debug(It.Is<string>(msg => msg.Contains("WorldStateDocument"))), Times.Once);
    }
}
