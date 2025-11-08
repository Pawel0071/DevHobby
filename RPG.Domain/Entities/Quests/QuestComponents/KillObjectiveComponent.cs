namespace RPG.Domain.Entities.Quests.QuestComponents;

/// <summary>
///     Component for quests that require killing NPCs.
/// </summary>
public class KillObjectiveComponent : IQuestComponent
{
    public Guid TargetNpcId { get; set; }
    public string TargetNpcName { get; set; } = string.Empty;
    public int RequiredCount { get; set; }
    public int CurrentCount { get; set; }
}
