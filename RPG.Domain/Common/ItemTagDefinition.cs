using RPG.Domain.Common.Interfaces;

namespace RPG.Domain.Common;

public sealed class ItemTagDefinition : IDictionaryEntry<ItemTagDefinition>
{
    public string? DisplayName { get; init; }
    public string? Category { get; init; }
    public string? Description { get; init; }
    public required string Code { get; init; } // np. "consumable"

    public static IEnumerable<ItemTagDefinition> Predefined => [];
}
