using RPG.Domain.Common;
using RPG.Domain.Entities;

namespace RPG.Core.Interfaces;

public interface ISkillService
{
    SkillResult AddSkillsAfterLevelUp(Character character);
    SkillResult AddSkillsAfterEquipItem(Character character, Item item);
    SkillResult RemoveSkillsAfterUnEquipItem(Character character, Item item);
    SkillResult UseSkill(Character character, Skill skill);
    SkillResult LearnSkill(Character character, Skill skill);
    SkillResult UnlearnSkill(Character character, Skill skill);
}

