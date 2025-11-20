using RPG.Domain.Common.Interfaces;

namespace RPG.Domain.Interfaces;

public interface ISkillAndCombat : ISkillsContainer, IStats
{
    Guid Id { get; }
    string Name { get; }
    public bool IsInCombat { get; set; }
    bool IsAlive => CurrentHealth > 0;
}


