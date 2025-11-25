using System.Numerics;
using RPG.Core.Common;
using RPG.Core.Interfaces;
using RPG.Domain.Common;
using RPG.Domain.Enums;
using RPG.Domain.Interfaces;
using RPG.Infrastructure.Interfaces;
using RPG.Domain.Models.Skills; // dodane

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

        attacker.ModifiedStats.TryGetValue(StatsProperty.MeleeAttackPower, out var attackPower);
        target.ModifiedStats.TryGetValue(StatsProperty.Armor, out var armor);
        target.ModifiedStats.TryGetValue(StatsProperty.Agility, out var agility); // brakujący średnik

        // Prosty wzór obrony (można później parametryzować):
        var defense = armor + (agility / 10);
        var rawDamage = attackPower - (defense / 2);
        var damage = Math.Max(1, rawDamage);

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

        await Task.CompletedTask; // zachowanie async, brak dodatkowych operacji
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> RangeAttackAsync(ISkillAndCombat attacker, ISkillAndCombat target)
    {
        if (!ValidateAlive(attacker, target, out var fail)) return fail;

        var damage = ComputeDamage(attacker, target, StatsProperty.RangedAttackPower, StatsProperty.Armor);
        ApplyDamage(attacker, target, damage, "ranged");
        await Task.CompletedTask;
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> SkillAttackAsync(ISkillAndCombat attacker, ISkillAndCombat target, Guid skillId)
    {
        if (!ValidateAlive(attacker, target, out var fail)) return fail;

        var skill = attacker.Skills.Keys.FirstOrDefault(s => s.Id == skillId);
        if (skill is null)
            return ServiceResult<bool>.Fail(ErrorCodeDefinition.Unknown, "Skill not found on attacker");

        if (!attacker.Skills.TryGetValue(skill, out var availability) || availability == SkillAvailability.UnAvailable)
            return ServiceResult<bool>.Fail(ErrorCodeDefinition.InvalidOperation, "Skill unavailable");

        var attackStat = ResolveAttackStatForSkill(skill);
        var defenseStat = StatsProperty.Armor; // docelowo zależne od typu ataku/żywiołu
        var damage = ComputeDamage(attacker, target, attackStat, defenseStat);
        ApplyDamage(attacker, target, damage, $"skill {skill.Name}");
        // prosty cooldown: rejestrujemy aktywację skilla jeśli nie istnieje
        if (!attacker.ActiveSkills.ContainsKey(skill)) attacker.ActiveSkills[skill] = DateTime.UtcNow;

        await Task.CompletedTask;
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<IDictionary<Guid, int>>> AreaAttackAsync(ISkillAndCombat attacker, IList<ISkillAndCombat> targets, Guid skillId)
    {
        var skill = attacker.Skills.Keys.FirstOrDefault(s => s.Id == skillId);
        if (skill is null)
            return ServiceResult<IDictionary<Guid, int>>.Fail(ErrorCodeDefinition.Unknown, "Skill not found on attacker");

        if (!attacker.Skills.TryGetValue(skill, out var availability) || availability == SkillAvailability.UnAvailable)
            return ServiceResult<IDictionary<Guid, int>>.Fail(ErrorCodeDefinition.InvalidOperation, "Skill unavailable");

        var attackStat = ResolveAttackStatForSkill(skill);
        var defenseStat = StatsProperty.Armor;
        var results = new Dictionary<Guid, int>();

        foreach (var t in targets)
        {
            if (!t.IsAlive) { results[t.Id] = 0; continue; }
            var dmg = ComputeDamage(attacker, t, attackStat, defenseStat);
            ApplyDamage(attacker, t, dmg, $"area skill {skill.Name}");
            results[t.Id] = dmg;
        }

        await Task.CompletedTask;
        return ServiceResult<IDictionary<Guid, int>>.Ok(results);
    }

    public async Task<ServiceResult<IList<ISkillAndCombat>>> InAreaAttackAsync(Guid skillId, Vector3 point, float range)
    {
        // Brak informacji o położeniu w ISkillAndCombat – docelowo potrzebny interfejs z Location.
        // Tymczasowa implementacja: zwracamy Fail z komunikatem.
        await Task.CompletedTask;
        return ServiceResult<IList<ISkillAndCombat>>.Fail(ErrorCodeDefinition.InvalidOperation, "Location data unavailable in ISkillAndCombat – cannot determine in-area targets");
    }

    // === Helpers ===
    private bool ValidateAlive(ISkillAndCombat attacker, ISkillAndCombat target, out ServiceResult<bool> fail)
    {
        if (!attacker.IsAlive)
        {
            fail = ServiceResult<bool>.Fail(ErrorCodeDefinition.InvalidOperation, "Attacker is dead");
            return false;
        }
        if (!target.IsAlive)
        {
            fail = ServiceResult<bool>.Fail(ErrorCodeDefinition.InvalidOperation, "Target already dead");
            return false;
        }
        fail = ServiceResult<bool>.Ok(true); // placeholder
        return true;
    }

    private StatsProperty ResolveAttackStatForSkill(Skill skill)
    {
        // Prosty mapping na podstawie tagów – można rozszerzyć o komponenty skilla
        if (skill.Tags.Any(t => t.Equals("ranged", StringComparison.OrdinalIgnoreCase))) return StatsProperty.RangedAttackPower;
        if (skill.Tags.Any(t => t.Equals("magic", StringComparison.OrdinalIgnoreCase))) return StatsProperty.MagicAttackPower;
        if (skill.Tags.Any(t => t.Equals("fire", StringComparison.OrdinalIgnoreCase))) return StatsProperty.FireAttackPower;
        if (skill.Tags.Any(t => t.Equals("frost", StringComparison.OrdinalIgnoreCase))) return StatsProperty.FrostAttackPower;
        if (skill.Tags.Any(t => t.Equals("nature", StringComparison.OrdinalIgnoreCase))) return StatsProperty.NatureAttackPower;
        return StatsProperty.MeleeAttackPower; // domyślnie melee
    }

    private int ComputeDamage(ISkillAndCombat attacker, ISkillAndCombat target, StatsProperty attackStat, StatsProperty defenseStat)
    {
        attacker.ModifiedStats.TryGetValue(attackStat, out var atk);
        target.ModifiedStats.TryGetValue(defenseStat, out var def);
        var dmg = atk - (def / 2);
        return Math.Max(1, dmg);
    }

    private void ApplyDamage(ISkillAndCombat attacker, ISkillAndCombat target, int damage, string context)
    {
        target.CurrentHealth = Math.Max(0, target.CurrentHealth - damage);
        if (target.CurrentHealth <= 0)
        {
            _logger.Info($"{attacker.Name} ({context}) killed {target.Name}");
        }
        else
        {
            _logger.Info($"{attacker.Name} ({context}) dealt {damage} to {target.Name} ({target.CurrentHealth}/{target.MaxHealth})");
        }
    }
}
