using System.Numerics;
using RPG.Core.Common;
using RPG.Domain.Interfaces;

namespace RPG.Core.Interfaces;

public interface ICombatService
{

    Task<ServiceResult<bool>> MeleeAttackAsync(ISkillAndCombat attacker, ISkillAndCombat target);

    Task<ServiceResult<bool>> RangeAttackAsync(ISkillAndCombat attacker, ISkillAndCombat target);

    Task<ServiceResult<bool>> SkillAttackAsync(ISkillAndCombat attacker, ISkillAndCombat target, Guid skillId);

    Task<ServiceResult<IDictionary<Guid, int>>> AreaAttackAsync(ISkillAndCombat attacker, IList<ISkillAndCombat> targets, Guid skillId);

    Task<ServiceResult<IList<ISkillAndCombat>>> InAreaAttackAsync(Guid skillId, Vector3 point, float range);
}

