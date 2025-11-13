namespace RPG.Domain.Models.Quests.QuestComponents;

/// <summary>
///     Component that adds time limit to the quest.
/// </summary>
public class TimeLimitComponent : IQuestComponent
{
    public int TimeLimitMinutes { get; set; }
    public DateTime? StartTime { get; set; }
}
