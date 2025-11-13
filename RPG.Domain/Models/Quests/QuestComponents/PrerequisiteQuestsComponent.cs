namespace RPG.Domain.Models.Quests.QuestComponents;

/// <summary>
///     Component that defines prerequisite quests.
/// </summary>
public class PrerequisiteQuestsComponent : IQuestComponent
{
    public List<Guid> RequiredQuestIds { get; set; } = new();
}
