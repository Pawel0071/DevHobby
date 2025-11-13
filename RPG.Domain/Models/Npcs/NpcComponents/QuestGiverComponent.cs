namespace RPG.Domain.Models.Npcs.NpcComponents;

/// <summary>
///     Component for NPCs that offer quests.
/// </summary>
public class QuestGiverComponent : INpcComponent
{
    public List<Guid> AvailableQuests { get; set; } = new();
    public List<Guid> CompletedQuests { get; set; } = new();
}
