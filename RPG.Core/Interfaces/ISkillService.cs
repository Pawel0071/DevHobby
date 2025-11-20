using RPG.Core.Common;
using RPG.Domain.Interfaces;
using RPG.Domain.Models;
using RPG.Domain.Models.Items;
using RPG.Domain.Models.Skills;

namespace RPG.Core.Interfaces;

public interface ISkillService
{
    ServiceResult<bool> AddSkillsAfterLevelUp(Character character);
    ServiceResult<bool> AddSkillsAfterEquipItem(Character character, Item item);
    ServiceResult<bool> RemoveSkillsAfterUnEquipItem(Character character, Item item);
    // Uzycie umiejętności przez postać lub przeciwnika
    ServiceResult<bool> UseSkill(ISkillAndCombat character, Skill skill);
    ServiceResult<bool> LearnSkill(Character character, Skill skill);
    ServiceResult<bool> UnlearnSkill(Character character, Skill skill);
}
