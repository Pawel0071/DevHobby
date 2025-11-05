using RPG.Domain.Enums;

namespace RPG.Domain.Entities.Items.ItemComponent;

public class EquippableComponent  : IItemComponent
{
    public IList<EquipmentSlot> ValidSlots { get; init; } = [];
    public bool IsTwoHanded { get; init; }
    public bool SupportsDualWield { get; init; }
    public bool IsUniqueEquip { get; init; }
}