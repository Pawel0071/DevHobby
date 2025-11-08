namespace RPG.Domain.Entities.Quests.QuestComponents;

/// <summary>
///     Component that makes quest repeatable.
/// </summary>
public class RepeatableQuestComponent : IQuestComponent
{
    public int CooldownHours { get; set; }
    public DateTime? LastCompletedTime { get; set; }
}
