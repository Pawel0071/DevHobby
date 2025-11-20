using System.Numerics;
using RPG.Core.Common;
using RPG.Core.Interfaces;
using RPG.Domain.Common;
using RPG.Domain.Enums;
using RPG.Domain.Interfaces;
using RPG.Infrastructure.Interfaces;

namespace RPG.Core.Services.CombatServices;

public class CombatService : ICombatService
{
    private readonly ILogger<CombatService> _logger;

    public CombatService(ILogger<CombatService> logger)
    {
        _logger = logger;
    }

    public ServiceResult<bool> MeleeAttack(ISkillAndCombat attacker, ISkillAndCombat target)
    {
        if (!target.IsAlive)
        {
            return ServiceResult<bool>.Fail(ErrorCodeDefinition.Unknown, "Target is already dead");
        }

        attacker.ModifiedStats.TryGetValue(StatsProperty.MeleeAttackPower, out var attackPower );
        target.ModifiedStats.TryGetValue(StatsProperty.MeleeAttackPower, out var defense);
        var damage = attackPower - (defense / 2);
        damage = Math.Max(1, damage);

        target.CurrentHealth = Math.Max(0, target.CurrentHealth - damage);

        if (target.CurrentHealth <= 0)
        {
            _logger.Info($"{attacker.Name} killed {target.Name}");
        }
        else
        {
            _logger.Info($"{attacker.Name} dealt {damage} damage to {target.Name} ({target.CurrentHealth}/{target.MaxHealth})");
        }

        return ServiceResult<bool>.Ok(true);
    }


    public async Task<ServiceResult<bool>> MeleeAttackAsync(ISkillAndCombat attacker, ISkillAndCombat target)
    {
        if (!target.IsAlive)
        {
            return ServiceResult<bool>.Fail(ErrorCodeDefinition.Unknown, "Target is already dead");
        }

        attacker.ModifiedStats.TryGetValue(StatsProperty.MeleeAttackPower, out var attackPower );
        target.ModifiedStats.TryGetValue(StatsProperty.MeleeAttackPower, out var defense);
        var damage = attackPower - (defense / 2);
        damage = Math.Max(1, damage);

        target.CurrentHealth = Math.Max(0, target.CurrentHealth - damage);

        if (target.CurrentHealth <= 0)
        {
            _logger.Info($"{attacker.Name} killed {target.Name}");
        }
        else
        {
            _logger.Info($"{attacker.Name} dealt {damage} damage to {target.Name} ({target.CurrentHealth}/{target.MaxHealth})");
        }

        return ServiceResult<bool>.Ok(true);

    }

    public async Task<ServiceResult<bool>> RangeAttackAsync(ISkillAndCombat attacker, ISkillAndCombat target)
    {
        throw new NotImplementedException();
    }

    public async Task<ServiceResult<bool>> SkillAttackAsync(ISkillAndCombat attacker, ISkillAndCombat target, Guid skillId)
    {
        throw new NotImplementedException();
    }

    public async Task<ServiceResult<IDictionary<Guid, int>>> AreaAttackAsync(ISkillAndCombat attacker, IList<ISkillAndCombat> targets, Guid skillId)
    {
        throw new NotImplementedException();
    }

    public async Task<ServiceResult<IList<ISkillAndCombat>>> InAreaAttackAsync(Guid skillId, Vector3 point, float range)
    {
        throw new NotImplementedException();
    }
}

