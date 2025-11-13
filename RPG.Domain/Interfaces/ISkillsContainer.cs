using RPG.Domain.Enums;
using RPG.Domain.Models.Skills;

namespace RPG.Domain.Interfaces;

public interface ISkillsContainer
{
    IDictionary<Skill, SkillAvailability> Skills { get; }

    IDictionary<Skill, DateTime> ActiveSkills { get; }
}
