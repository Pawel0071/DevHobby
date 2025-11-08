namespace RPG.Domain.Entities.Quests.QuestComponents;

/// <summary>
///     Component that defines reputation rewards.
/// </summary>
public class ReputationRewardsComponent : IQuestComponent
{
    /// <summary>
    ///     Faction name -> reputation amount
    /// </summary>
    public Dictionary<string, int> FactionReputations { get; set; } = new();
}
