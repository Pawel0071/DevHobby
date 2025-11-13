namespace RPG.Domain.Models.Quests.QuestComponents;

/// <summary>
///     Component for quests that require interacting with an object/NPC.
/// </summary>
public class InteractObjectiveComponent : IQuestComponent
{
    public Guid TargetObjectId { get; set; }
    public string TargetObjectName { get; set; } = string.Empty;
    public int RequiredInteractions { get; set; }
    public int CurrentInteractions { get; set; }
}
