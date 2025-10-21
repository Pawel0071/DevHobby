using System.Numerics;
using RPG.Core.Interfaces;
using RPG.Core.Domain.Entities.Common;
using RPG.Core.Domain.Entities.Enums;
using RPG.Core.MovementService;

namespace RPG.Core.Domain.Entities;

public abstract class BaseCharacter  : IMovable, IAttackable, ISkill
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public int Level { get; set; } = 1;
    public int MaxHealth { get; set; }
    public int CurrentHealth { get; set; }
    public int MaxMana { get; set; }
    public int CurrentMana { get; set; }
    public Stats? Stats { get; set; } 
    public List<Skill> Skills { get; set; } = [];
    public Dictionary<int, DateTime> SkillCooldowns { get; set; } = new();
    public Vector3 Position { get; set; } = new();
    
    public bool CanMove { get; set; }
    public List<Effect> ActiveEffects { get; set; } = [];

    public abstract float GetMovementSpeed();
    
    public abstract void Move(MoveType moveType, int angle);

    public abstract void ReceiveDamage(int amount);

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