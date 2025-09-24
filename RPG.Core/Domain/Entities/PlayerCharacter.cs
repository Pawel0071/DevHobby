using RPG.Core.Domain.Entities.Common;
using RPG.Core.Domain.Entities.Enums;
using RPG.Core.Interfaces;  

namespace RPG.Core.Domain.Entities;

public class PlayerCharacter : BaseCharacter
{
    public CharacterClass Class { get; set; }
    public int Experience { get; set; }
    public EquipmentSlots Equipment { get; set; } = new();
    public List<Item> Inventory { get; set; } = [];
    public Dictionary<int, int> SkillLevels { get; set; } = new();
    public Guid? SessionId { get; set; } = null;
    public bool IsOnline => SessionId != null;
}