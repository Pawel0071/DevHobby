using RPG.Domain.Containers;
using RPG.Domain.Enums;
using RPG.Domain.Models.Skills;

namespace RPG.Domain.Models.Quests.QuestComponents;

/// <summary>
///     Component that defines skill rewards.
///     Uses SkillsContainer like Character and TrainerComponent.
/// </summary>
public class SkillRewardsComponent : IQuestComponent
{
    private SkillsContainer RewardSkillsContainer { get; } = new();

    /// <summary>
    ///     Public access to reward skills (like Character's skills)
    /// </summary>
    public IDictionary<Skill, SkillAvailability> RewardSkills => RewardSkillsContainer.Skills;

    /// <summary>
    ///     Get the skills container (for services that need full container interface)
    /// </summary>
    public SkillsContainer GetSkillsContainer()
    {
        return RewardSkillsContainer;
    }
}
