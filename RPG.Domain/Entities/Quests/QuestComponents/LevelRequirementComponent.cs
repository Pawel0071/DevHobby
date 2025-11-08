namespace RPG.Domain.Entities.Quests.QuestComponents;

/// <summary>
///     Component that defines level requirement for the quest.
/// </summary>
public class LevelRequirementComponent : IQuestComponent
{
    public int MinLevel { get; set; }
    public int? MaxLevel { get; set; } // Optional max level for scaling
}
