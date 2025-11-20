using RPG.Domain.Containers;
using RPG.Domain.Enums;
using RPG.Domain.Models.Skills;

namespace RPG.Domain.Models.Npcs.NpcComponents;

/// <summary>
///     Component for NPCs that teach skills or train characters.
///     Uses SkillsContainer to store teachable skills.
/// </summary>
public class TrainerComponent : NpcComponentBase
{
    private SkillsContainer TeachableSkillsContainer { get; } = new();

    /// <summary>
    ///     Public access to teachable skills (like Character's skills)
    /// </summary>
    public IDictionary<Skill, SkillAvailability> TeachableSkills => TeachableSkillsContainer.Skills;

    /// <summary>
    ///     Trainer's specialization (e.g., "Combat", "Magic", "Crafting")
    /// </summary>
    public string Specialization { get; set; } = string.Empty;

    /// <summary>
    ///     Get the skills container (for services that need full container interface)
    /// </summary>
    public SkillsContainer GetSkillsContainer()
    {
        return TeachableSkillsContainer;
    }

    public override string ComponentName => "Trainer";
    public override string ComponentType => "Trainer";
}
