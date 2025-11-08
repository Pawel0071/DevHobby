using RPG.Domain.Enums;

namespace RPG.Domain.Entities.Items.ItemComponent;

public class EquippableComponent : IItemComponent
{
    public IList<EquipmentSlot> ValidSlots { get; init; } = new List<EquipmentSlot>();
    public bool IsTwoHanded { get; init; }
    public bool SupportsDualWield { get; init; }
    public bool IsUniqueEquip { get; init; }
}
