using System;
using System.Collections.Generic;
using System.Linq;
using RPG.Domain.Common.Interfaces;
using RPG.Domain.Enums;

namespace RPG.Domain.Common;

public class ItemTypeDefinition : IDictionaryEntry<ItemTypeDefinition>
{
    public string Code { get; init; } = string.Empty;
    public required string DisplayName { get; init; }   
    public IList<string> Tags { get; init; } = new List<string>();
    public IList<EquipmentSlot> ValidSlots { get; init; } = new List<EquipmentSlot>();
    public IList<Type> RequiredComponents { get; init; } = new List<Type>();
    public IList<Type> OptionalComponents { get; init; } = new List<Type>();
    public static IEnumerable<ItemTypeDefinition> Predefined => Enumerable.Empty<ItemTypeDefinition>();
}
