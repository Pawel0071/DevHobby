using RPG.Domain.Common.Interfaces;
using RPG.Domain.Enums;

namespace RPG.Domain.Common;

public class ItemTypeDefinition : IDictionaryEntry<ItemTypeDefinition>
{
    public string Code { get; init; } = string.Empty;
    public required string DisplayName { get; init; }   
    public IList<string> Tags { get; init; } = [];
    public IList<EquipmentSlot> ValidSlots { get; init; } = [];
    public IList<Type> RequiredComponents { get; init; } = [];
    public IList<Type> OptionalComponents { get; init; } = [];
    public static IEnumerable<ItemTypeDefinition> Predefined => [];
}
