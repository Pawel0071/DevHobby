namespace RPG.Domain.Models.Quests.QuestComponents;

/// <summary>
///     Component that defines quest chain relationship.
/// </summary>
public class QuestChainComponent : IQuestComponent
{
    public Guid? NextQuestId { get; set; }
    public Guid? PreviousQuestId { get; set; }
    public int ChainPosition { get; set; } // 1 = first, 2 = second, etc.
}
