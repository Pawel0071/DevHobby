using RPG.GameServer.Domain.Entities;
using RPG.GameServer.Domain.Entities.Common;
using RPG.GameServer.Domain.Types;

namespace RPG.GameServer.Application.Combat;

public static class CombatSystem
{
    public static CombatResult ExecuteSkill(this BaseCharacter attacker, BaseCharacter target, int skillId)
    {
        // Sprawdź czy umiejętność istnieje i może być użyta
        if (!attacker.CanUseSkill(skillId, out var skill) || skill == null)
            return CombatResult.Failed("Skill not available or insufficient mana.");

        // Sprawdź zasięg
        if (skill.RequiresTarget && !IsInRange(attacker.Position, target.Position, skill.Range))
            return CombatResult.Failed("Target out of range.");

        // Użyj umiejętności (zużycie many)
        attacker.UseSkill(skillId);

        // Oblicz obrażenia
        var damage = CalculateDamage(attacker, target, skill);

        // Zadaj obrażenia
        target.ReceiveDamage(damage);

        // Nałóż efekty statusowe
        foreach (var effect in skill.AppliedEffects)
        {
            target.ActiveEffects.Add(effect);
        }

        // Zwróć wynik
        return CombatResult.Success(skill.Name, damage, skill.AppliedEffects, target.CurrentHealth <= 0);
    }

    private static bool IsInRange(Location a, Location b, float range)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        var dz = a.Z - b.Z;
        var distance = Math.Sqrt(dx * dx + dy * dy + dz * dz);
        return distance <= range;
    }

    private static int CalculateDamage(BaseCharacter attacker, BaseCharacter target, Skill skill)
    {
        var stats = attacker.Stats;
        var targetStats = target.Stats;
        var rng = new Random();

        // 1. Trafienie (dla fizycznych)
        if (skill.Type is SkillType.MeleeAttack or SkillType.RangedAttack)
        {
            var hitChance = CalculateHitChance(stats.AttackRating, targetStats.Defense);
            if (rng.NextDouble() > hitChance)
                return 0;
        }

        // 2. Bazowe obrażenia zależne od typu
        int baseDamage = skill.Power;
        switch (skill.Type)
        {
            case SkillType.MeleeAttack:
                baseDamage += stats.Strength;
                baseDamage -= targetStats.DamageReduction * baseDamage / 100;
                break;

            case SkillType.RangedAttack:
                baseDamage += stats.Dexterity;
                baseDamage -= targetStats.DamageReduction * baseDamage / 100;
                break;

            case SkillType.MagicDamage:
                baseDamage += stats.Energy / 2;
                baseDamage += stats.FasterCastRate / 5;
                baseDamage -= targetStats.MagicFind / 10; // proxy dla magic resist
                break;

            case SkillType.AreaOfEffect:
                baseDamage += stats.Energy;
                baseDamage -= targetStats.FireResist * baseDamage / 100;
                break;

            case SkillType.DamageOverTime:
                baseDamage -= targetStats.PoisonResist * baseDamage / 100;
                break;

            case SkillType.Freeze:
                baseDamage -= targetStats.ColdResist * baseDamage / 100;
                break;

            case SkillType.Stun:
                baseDamage -= targetStats.FasterHitRecovery / 10;
                break;

            case SkillType.Curse:
            case SkillType.Debuff:
                baseDamage += skill.Power; // nie fizyczne, ale wpływające na status
                break;

            default:
                baseDamage += skill.Power;
                break;
        }

        return Math.Max(0, (int)Math.Round((double)baseDamage));
    }
    private static double CalculateHitChance(int attackRating, int defense)
    {
        return Math.Clamp((double)attackRating / (attackRating + defense + 1), 0.1, 0.95);
    }
}