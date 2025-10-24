using RPG.Core.Domain.Entities.Common;
using RPG.Core.Domain.Entities.Enums;
using RPG.Core.Interfaces;

namespace RPG.Core.Domain.Interfaces;

public interface ISkillsContainer
{
    IDictionary<Skill, SkillAvailability> Skills { get; }
    
    IDictionary<Skill, DateTime> ActiveSkills { get; }
}

