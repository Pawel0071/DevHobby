using RPG.GameServer.Domain.Types;
using RPG.GameServer.Interfaces;
using RPG.GameServer.Domain.Entities.Common;

namespace RPG.GameServer.Domain.Entities;

public abstract class BaseCharacter  : IMovable, IAttackable, ISkill
{
    public required string Id { get; set; } = Guid.NewGuid().ToString();
    public required string Name { get; set; }
    public int Level { get; set; } = 1;
    public int MaxHealth { get; set; }
    public int CurrentHealth { get; set; }
    public int MaxMana { get; set; }
    public int CurrentMana { get; set; }
    public Stats Stats { get; set; } = new();
    public List<Skill> Skills { get; set; } = [];
    public Dictionary<int, DateTime> SkillCooldowns { get; set; } = new();
    public Location Position { get; set; } = new();
    public List<Effect> ActiveEffects { get; set; } = [];

    public void Move(double dx, double dy, double dz)
    {
        Position.MoveBy(dx,dy,dz);
    }

    public void ReceiveDamage(int amount)
    {
        CurrentHealth = Math.Max(0, CurrentHealth - amount);
    }

    public bool IsAlive => CurrentHealth > 0;
    
    public bool CanUseSkill(int skillId, out Skill? skill)
    {
        skill = Skills.FirstOrDefault(s => s.Id == skillId);
        if (skill == null || CurrentMana < skill.ManaCost)
            return false;
        
        if (SkillCooldowns.TryGetValue(skillId, out var cooldownEnd))
        {
            if (DateTime.UtcNow < cooldownEnd)
                return false;
        }
        
        var blockingEffects = new[] { Effect.Silenced, Effect.Stunned, Effect.Frozen };
        if (ActiveEffects.Any<Effect>((Effect e) => blockingEffects.Contains<Effect>(e)))
            return false;

        return true;
    }

    public Skill? UseSkill(int skillId)
    {
        if (!CanUseSkill(skillId, out var skill) || skill == null)
            return null;

        CurrentMana = Math.Max(0, CurrentMana - skill.ManaCost);
        SkillCooldowns[skillId] = DateTime.UtcNow.AddSeconds(skill.CooldownSeconds);
        return skill;
    }
}