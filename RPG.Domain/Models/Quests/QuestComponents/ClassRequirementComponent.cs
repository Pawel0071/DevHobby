namespace RPG.Domain.Models.Quests.QuestComponents;

/// <summary>
///     Component that defines class restrictions for the quest.
/// </summary>
public class ClassRequirementComponent : IQuestComponent
{
    public List<string> AllowedClasses { get; set; } = new();
}
