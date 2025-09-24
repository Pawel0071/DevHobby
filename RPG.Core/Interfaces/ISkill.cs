using RPG.Core.Domain.Entities;

namespace RPG.Core.Interfaces;

public interface ISkill
{
    int CurrentMana { get; set; }
    int MaxMana { get; set; }
    bool CanUseSkill(int skillId, out Skill? skill);
    Skill? UseSkill(int skillId);
}