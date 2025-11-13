// filepath: /Volumes/Data/Repositories/DevHobby/RPG.GameServer/Dtos/ItemDto.cs
namespace RPG.GameServer.Dtos;

public sealed class ItemDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string TypeCode { get; init; } = string.Empty;
    public int RequiredLevel { get; init; }
    public int StackSize { get; init; }
    public IReadOnlyCollection<string> Tags { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, int>? Modifiers { get; init; }
    public int? SocketNo { get; init; }
    public IReadOnlyCollection<Guid>? SkillIds { get; init; }
    public Guid? QuestId { get; init; }
    public Guid? StepId { get; init; }
    public IReadOnlyCollection<RPG.Domain.Enums.EquipmentSlot>? EquipmentSlots { get; init; }
    public bool? IsTwoHanded { get; init; }
    public bool? SupportsDualWield { get; init; }
    public bool? IsUniqueEquip { get; init; }
    public IReadOnlyCollection<string>? UsedInItemIds { get; init; }
}
