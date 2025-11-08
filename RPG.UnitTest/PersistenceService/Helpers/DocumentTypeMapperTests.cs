using FluentAssertions;
using RPG.Domain.Entities;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;
using RPG.PersistenceService.Helpers;

namespace RPG.UnitTest.PersistenceService.Helpers;

public class DocumentTypeMapperTests
{
    [Theory]
    [InlineData("player.created", "Players")]
    [InlineData("item.updated", "Items")]
    [InlineData("npc.deleted", "Npcs")]
    [InlineData("quest.created", "Quests")]
    public void GetCollectionNameFromRoutingKey_KnownKey_ReturnsCollection(string routingKey, string expectedCollection)
    {
        DocumentTypeMapper.GetCollectionNameFromRoutingKey(routingKey).Should().Be(expectedCollection);
    }

    [Fact]
    public void GetCollectionNameFromRoutingKey_UnknownKey_Throws()
    {
        var act = () => DocumentTypeMapper.GetCollectionNameFromRoutingKey("unknown.created");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*unknown.created*");
    }

    [Theory]
    [InlineData("Players", typeof(PlayerDocument))]
    [InlineData("Items", typeof(ItemDocument))]
    [InlineData("Skills", typeof(SkillDocument))]
    [InlineData("Quests", typeof(QuestDocument))]
    public void GetDocumentTypeFromCollectionName_KnownCollection_ReturnsMappedType(string collectionName, Type expectedType)
    {
        var result = DocumentTypeMapper.GetDocumentTypeFromCollectionName(collectionName);

        result.Should().Be(expectedType);
    }

    [Fact]
    public void GetDocumentTypeFromCollectionName_UnknownCollection_ReturnsNull()
    {
        var result = DocumentTypeMapper.GetDocumentTypeFromCollectionName("UnknownCollection");

        result.Should().BeNull();
    }

    [Fact]
    public void GetEntityTypeFromCollectionName_KnownCollection_ReturnsDomainType()
    {
        var result = DocumentTypeMapper.GetEntityTypeFromCollectionName(PlayerDocument.CollectionName);

        result.Should().Be(typeof(Player));
    }

    [Fact]
    public void TryGetMappingFromRoutingKey_KnownKey_ExposesFullMapping()
    {
        var mapping = DocumentTypeMapper.TryGetMappingFromRoutingKey("character.updated");

        mapping.Should().NotBeNull();
        mapping!.EntityType.Should().Be(typeof(Character));
        mapping.DocumentType.Should().Be(typeof(CharacterDocument));
        mapping.MapperServiceType.Should().Be(typeof(IDocumentMapper<Character, CharacterDocument>));
    }

    [Fact]
    public void TryGetMappingFromRoutingKey_UnknownKey_ReturnsNull()
    {
        var mapping = DocumentTypeMapper.TryGetMappingFromRoutingKey("unknown.created");

        mapping.Should().BeNull();
    }
}
