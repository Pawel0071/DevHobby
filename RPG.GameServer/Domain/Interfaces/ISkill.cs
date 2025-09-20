using RPG.GameServer.Domain.Entities;

namespace RPG.GameServer.Interfaces;

public interface ISkill
{
    int CurrentMana { get; set; }
    int MaxMana { get; set; }
    bool CanUseSkill(int skillId, out Skill? skill);
    Skill? UseSkill(int skillId);
}