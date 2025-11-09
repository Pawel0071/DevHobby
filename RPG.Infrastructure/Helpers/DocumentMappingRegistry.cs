using System;
using System.Collections.Generic;
using System.Linq;
using RPG.Domain.Entities;
using RPG.Domain.Entities.Items;
using RPG.Domain.Entities.MapObjects;
using RPG.Domain.Entities.Npcs;
using RPG.Domain.Entities.Quests;
using RPG.Domain.Entities.Skills;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.Helpers;

public sealed record DocumentMappingDefinition(
    string EntityKey,
    string CollectionName,
    Type EntityType,
    Type DocumentType,
    Type MapperServiceType);

public static class DocumentMappingRegistry
{
    private static readonly IReadOnlyList<DocumentMappingDefinition> Definitions = new List<DocumentMappingDefinition>
    {
        new(
            EntityKey: "character",
            CollectionName: CharacterDocument.CollectionName,
            EntityType: typeof(Character),
            DocumentType: typeof(CharacterDocument),
            MapperServiceType: typeof(IDocumentMapper<Character, CharacterDocument>)),
        new(
            EntityKey: "item",
            CollectionName: ItemDocument.CollectionName,
            EntityType: typeof(Item),
            DocumentType: typeof(ItemDocument),
            MapperServiceType: typeof(IDocumentMapper<Item, ItemDocument>)),
        new(
            EntityKey: "skill",
            CollectionName: SkillDocument.CollectionName,
            EntityType: typeof(Skill),
            DocumentType: typeof(SkillDocument),
            MapperServiceType: typeof(IDocumentMapper<Skill, SkillDocument>)),
        new(
            EntityKey: "quest",
            CollectionName: QuestDocument.CollectionName,
            EntityType: typeof(Quest),
            DocumentType: typeof(QuestDocument),
            MapperServiceType: typeof(IDocumentMapper<Quest, QuestDocument>)),
        new(
            EntityKey: "npc",
            CollectionName: NpcDocument.CollectionName,
            EntityType: typeof(Npc),
            DocumentType: typeof(NpcDocument),
            MapperServiceType: typeof(IDocumentMapper<Npc, NpcDocument>)),
        new(
            EntityKey: "player",
            CollectionName: PlayerDocument.CollectionName,
            EntityType: typeof(Player),
            DocumentType: typeof(PlayerDocument),
            MapperServiceType: typeof(IDocumentMapper<Player, PlayerDocument>)),
        new(
            EntityKey: "gamesession",
            CollectionName: GameSessionDocument.CollectionName,
            EntityType: typeof(GameSession),
            DocumentType: typeof(GameSessionDocument),
            MapperServiceType: typeof(IDocumentMapper<GameSession, GameSessionDocument>)),
        new(
            EntityKey: "mapobject",
            CollectionName: MapObjectDocument.CollectionName,
            EntityType: typeof(MapObject),
            DocumentType: typeof(MapObjectDocument),
            MapperServiceType: typeof(IDocumentMapper<MapObject, MapObjectDocument>)),
        new(
            EntityKey: "worldstate",
            CollectionName: WorldStateDocument.CollectionName,
            EntityType: typeof(WorldState),
            DocumentType: typeof(WorldStateDocument),
            MapperServiceType: typeof(IDocumentMapper<WorldState, WorldStateDocument>))
    };

    public static IReadOnlyCollection<DocumentMappingDefinition> All => Definitions;

    public static DocumentMappingDefinition? TryGetByEntityKey(string entityKey)
    {
        return Definitions.FirstOrDefault(mapping =>
            string.Equals(mapping.EntityKey, entityKey, StringComparison.OrdinalIgnoreCase));
    }

    public static DocumentMappingDefinition? TryGetByCollectionName(string collectionName)
    {
        return Definitions.FirstOrDefault(mapping =>
            string.Equals(mapping.CollectionName, collectionName, StringComparison.OrdinalIgnoreCase));
    }

    public static DocumentMappingDefinition? TryGetByEntityType(Type entityType)
    {
        return Definitions.FirstOrDefault(mapping => mapping.EntityType == entityType);
    }

    public static DocumentMappingDefinition? TryGetByDocumentType(Type documentType)
    {
        return Definitions.FirstOrDefault(mapping => mapping.DocumentType == documentType);
    }
}
