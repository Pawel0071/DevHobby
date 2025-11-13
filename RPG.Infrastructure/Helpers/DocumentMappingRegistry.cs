using System;
using System.Collections.Generic;
using System.Linq;
using RPG.Domain.Models;
using RPG.Domain.Models.Items;
using RPG.Domain.Models.MapObjects;
using RPG.Domain.Models.Npcs;
using RPG.Domain.Models.Quests;
using RPG.Domain.Models.Skills;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Models;

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
            MapperServiceType: typeof(IModelMapper<Character, CharacterDocument>)),
        new(
            EntityKey: "item",
            CollectionName: ItemDocument.CollectionName,
            EntityType: typeof(Item),
            DocumentType: typeof(ItemDocument),
            MapperServiceType: typeof(IModelMapper<Item, ItemDocument>)),
        new(
            EntityKey: "skill",
            CollectionName: SkillDocument.CollectionName,
            EntityType: typeof(Skill),
            DocumentType: typeof(SkillDocument),
            MapperServiceType: typeof(IModelMapper<Skill, SkillDocument>)),
        new(
            EntityKey: "quest",
            CollectionName: QuestDocument.CollectionName,
            EntityType: typeof(Quest),
            DocumentType: typeof(QuestDocument),
            MapperServiceType: typeof(IModelMapper<Quest, QuestDocument>)),
        new(
            EntityKey: "npc",
            CollectionName: NpcDocument.CollectionName,
            EntityType: typeof(Npc),
            DocumentType: typeof(NpcDocument),
            MapperServiceType: typeof(IModelMapper<Npc, NpcDocument>)),
        new(
            EntityKey: "player",
            CollectionName: PlayerDocument.CollectionName,
            EntityType: typeof(Player),
            DocumentType: typeof(PlayerDocument),
            MapperServiceType: typeof(IModelMapper<Player, PlayerDocument>)),
        new(
            EntityKey: "gamesession",
            CollectionName: GameSessionDocument.CollectionName,
            EntityType: typeof(GameSession),
            DocumentType: typeof(GameSessionDocument),
            MapperServiceType: typeof(IModelMapper<GameSession, GameSessionDocument>)),
        new(
            EntityKey: "mapobject",
            CollectionName: MapObjectDocument.CollectionName,
            EntityType: typeof(MapObject),
            DocumentType: typeof(MapObjectDocument),
            MapperServiceType: typeof(IModelMapper<MapObject, MapObjectDocument>)),
        new(
            EntityKey: "worldstate",
            CollectionName: WorldStateDocument.CollectionName,
            EntityType: typeof(WorldState),
            DocumentType: typeof(WorldStateDocument),
            MapperServiceType: typeof(IModelMapper<WorldState, WorldStateDocument>))
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
