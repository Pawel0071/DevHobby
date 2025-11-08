namespace RPG.Domain.Entities.Quests.QuestComponents;

/// <summary>
///     Component that defines experience and gold rewards.
/// </summary>
public class BasicRewardsComponent : IQuestComponent
{
    public int ExperienceReward { get; set; }
    public int GoldReward { get; set; }
}
