using RPG.Core.Common;
using RPG.Domain.Common;
using RPG.Domain.Entities;
using RPG.Domain.Entities.Items;

namespace RPG.Core.Interfaces;

public interface ISkillService
{
    ServiceResult<bool> AddSkillsAfterLevelUp(Character character);
    ServiceResult<bool> AddSkillsAfterEquipItem(Character character, Item item);
    ServiceResult<bool> RemoveSkillsAfterUnEquipItem(Character character, Item item);
    ServiceResult<bool> UseSkill(Character character, Skill skill);
    ServiceResult<bool> LearnSkill(Character character, Skill skill);
    ServiceResult<bool> UnlearnSkill(Character character, Skill skill);
}

