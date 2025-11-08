using RPG.Domain.Entities;
using RPG.Domain.Entities.Items;
using RPG.Domain.Enums;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.Mappers;

/// <summary>
///     Mapper for converting between Character domain entity and CharacterDocument
/// </summary>
public class CharacterDocumentMapper : IDocumentMapper<Character, CharacterDocument>
{
    private readonly IDocumentMapper<Item, ItemDocument>? _itemMapper;
    private readonly ILogger<CharacterDocumentMapper>? _logger;

    public CharacterDocumentMapper(
        ILogger<CharacterDocumentMapper>? logger = null,
        IDocumentMapper<Item, ItemDocument>? itemMapper = null)
    {
        _logger = logger;
        _itemMapper = itemMapper;
    }

    public CharacterDocument ToDocument(Character entity)
    {
        _logger?.Debug($"Converting Character to CharacterDocument. Id={entity.Id}, Name={entity.Name}");

        var doc = new CharacterDocument
        {
            Id = entity.Id,
            Name = entity.Name,
            PlayerId = entity.PlayerId,
            SessionId = entity.SessionId,
            Class = entity.Class.ToString(),
            Level = entity.Level,
            Experience = entity.Experience,
            ExperienceToNextLevel = entity.ExperienceToNextLevel,
            CurrentHealth = entity.CurrentHealth,
            MaxHealth = entity.MaxHealth,
            CurrentResource = entity.CurrentResource,
            MaxResource = entity.MaxResource,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Map Stats
        doc.BaseStats = entity.BaseStats.ToDictionary(
            kvp => kvp.Key.ToString(),
            kvp => kvp.Value
        );

        doc.ModifiedStats = entity.ModifiedStats.ToDictionary(
            kvp => kvp.Key.ToString(),
            kvp => kvp.Value
        );

        // Map Equipment
        doc.Equipment = entity.Equipments
            .Where(kvp => kvp.Value != null)
            .ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp => kvp.Value.Id
            );

        // Map Inventory
        doc.Backpack = entity.BackpackInventory
            .Select(slot => new InventorySlotDocument { ItemId = slot.Item?.Id, Quantity = slot.Quantity })
            .ToList();

        doc.Bank = entity.BankStorage
            .Select(slot => new InventorySlotDocument { ItemId = slot.Item?.Id, Quantity = slot.Quantity })
            .ToList();

        // Map Skills
        doc.Skills = entity.Skills.ToDictionary(
            kvp => kvp.Key.Id.ToString(),
            kvp => kvp.Value.ToString()
        );

        doc.ActiveSkills = entity.ActiveSkills.ToDictionary(
            kvp => kvp.Key.Id.ToString(),
            kvp => kvp.Value
        );

        _logger?.Debug(
            $"CharacterDocument created. Id={doc.Id}, Equipment count={doc.Equipment.Count}, Backpack slots={doc.Backpack.Count}");
        return doc;
    }

    public Character ToDomain(CharacterDocument document)
    {
        _logger?.Debug($"Converting CharacterDocument to Character. Id={document.Id}, Name={document.Name}");

        // Parse CharacterClass enum
        if (!Enum.TryParse<CharacterClass>(document.Class, out var characterClass))
        {
            _logger?.Warn($"Invalid CharacterClass '{document.Class}' in document. Defaulting to Warrior.");
            characterClass = CharacterClass.Warrior;
        }

        var character = new Character(document.SessionId, characterClass)
        {
            Id = document.Id,
            Name = document.Name,
            PlayerId = document.PlayerId,
            Level = document.Level,
            Experience = document.Experience,
            ExperienceToNextLevel = document.ExperienceToNextLevel,
            CurrentHealth = document.CurrentHealth,
            MaxHealth = document.MaxHealth,
            CurrentResource = document.CurrentResource,
            MaxResource = document.MaxResource
        };

        // Map BaseStats
        foreach (var stat in document.BaseStats)
            if (Enum.TryParse<StatsProperty>(stat.Key, out var statProperty))
                character.BaseStats[statProperty] = stat.Value;
            else
                _logger?.Warn($"Invalid StatsProperty '{stat.Key}' in BaseStats. Skipping.");

        // Map ModifiedStats
        foreach (var stat in document.ModifiedStats)
            if (Enum.TryParse<StatsProperty>(stat.Key, out var statProperty))
                character.ModifiedStats[statProperty] = stat.Value;
            else
                _logger?.Warn($"Invalid StatsProperty '{stat.Key}' in ModifiedStats. Skipping.");

        // Map Equipment - Note: This requires loading actual Item entities from repository
        // For now, we just store the structure. Items should be loaded separately.
        foreach (var equip in document.Equipment)
            if (Enum.TryParse<EquipmentSlot>(equip.Key, out var slot))
                // Equipment items need to be resolved from item repository
                // This is typically done by the service layer after mapping
                _logger?.Debug($"Equipment slot {slot} has item {equip.Value}. Item needs to be loaded separately.");
            else
                _logger?.Warn($"Invalid EquipmentSlot '{equip.Key}' in Equipment. Skipping.");

        // Map Inventory - Similar to equipment, actual Item entities need to be loaded
        _logger?.Debug(
            $"Character has {document.Backpack.Count} backpack slots and {document.Bank.Count} bank slots. Items need to be loaded separately.");

        // Map Skills - Skill entities need to be loaded from repository
        // Skills dictionary: Skill ID -> SkillAvailability
        foreach (var skill in document.Skills)
            if (Enum.TryParse<SkillAvailability>(skill.Value, out var availability))
            {
                _logger?.Debug(
                    $"Skill {skill.Key} has availability {availability}. Skill entity needs to be loaded separately.");
            }
            else
            {
                _logger?.Warn($"Invalid SkillAvailability '{skill.Value}' for skill {skill.Key}. Skipping.");
            }

        // Map ActiveSkills - Skill entities need to be loaded from repository
        foreach (var activeSkill in document.ActiveSkills)
            _logger?.Debug(
                $"Active skill {activeSkill.Key} activated at {activeSkill.Value}. Skill entity needs to be loaded separately.");
        // Skill entities need to be resolved from skill repository
        _logger?.Debug(
            $"Character domain entity created. Id={character.Id}, Document has {document.Skills.Count} skills, {document.ActiveSkills.Count} active skills");
        return character;
    }
}
