using RPG.Domain.Common;
using RPG.Domain.Enums;

namespace RPG.Domain.Interfaces;

public interface ISkillsContainer
{
    IDictionary<Skill, SkillAvailability> Skills { get; }
    
    IDictionary<Skill, DateTime> ActiveSkills { get; }
}

