namespace RPG.Domain.Entities.Quests.QuestComponents;

/// <summary>
///     Component for quests that require exploring/visiting a location.
/// </summary>
public class ExploreObjectiveComponent : IQuestComponent
{
    public Location TargetLocation { get; set; } = new();
    public string LocationName { get; set; } = string.Empty;
    public float ProximityRadius { get; set; } // How close player needs to be
    public bool IsVisited { get; set; }
}
