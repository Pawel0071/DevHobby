namespace RPG.Domain.Models.Npcs.NpcComponents;

/// <summary>
///     Component for NPCs that offer quests.
/// </summary>
public class QuestGiverComponent : NpcComponentBase
{
    public override string ComponentName => "QuestGiver";
    public override string ComponentType => "QuestGiver";

    public List<Guid> AvailableQuests { get; set; } = new();
    public List<Guid> CompletedQuests { get; set; } = new();
}
