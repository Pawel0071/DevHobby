using RPG.Domain.Enums;
using RPG.Domain.Interfaces;
using RPG.Domain.Models.Skills;

namespace RPG.Domain.Containers;

public class SkillsContainer : ISkillsContainer
{
    public SkillsContainer()
    {
        Skills = new Dictionary<Skill, SkillAvailability>();
        ActiveSkills = new Dictionary<Skill, DateTime>();
    }

    public SkillsContainer(
        IDictionary<Skill, SkillAvailability> skills,
        IDictionary<Skill, DateTime> activeSkills)
    {
        Skills = skills;
        ActiveSkills = activeSkills;
    }

    public IDictionary<Skill, SkillAvailability> Skills { get; set; }

    public IDictionary<Skill, DateTime> ActiveSkills { get; set; }

    public void LearnSkill(Skill skill, SkillAvailability availability = SkillAvailability.Available)
    {
        if (!Skills.ContainsKey(skill)) Skills[skill] = availability;
    }

    public void ActivateSkill(Skill skill)
    {
        if (Skills.ContainsKey(skill) && Skills[skill] == SkillAvailability.Available)
            ActiveSkills[skill] = DateTime.UtcNow;
    }

    public void DeactivateSkill(Skill skill)
    {
        ActiveSkills.Remove(skill);
    }

    public bool HasSkill(Skill skill)
    {
        return Skills.ContainsKey(skill);
    }

    public bool IsSkillActive(Skill skill)
    {
        return ActiveSkills.ContainsKey(skill);
    }

    public void SetSkillAvailability(Skill skill, SkillAvailability availability)
    {
        if (Skills.ContainsKey(skill)) Skills[skill] = availability;
    }

    public SkillAvailability GetSkillAvailability(Skill skill)
    {
        return Skills.TryGetValue(skill, out var availability)
            ? availability
            : SkillAvailability.UnAvailable;
    }
}
