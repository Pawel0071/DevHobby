using RPG.Domain.Common;

namespace RPG.Domain.Entities.Quests;

/// <summary>
///     Domain entity representing a quest.
///     Uses tag-based and component-based system similar to Items and NPCs.
///     Tags define quest type/difficulty (main, side, hard, etc.)
///     Components define objectives, requirements, and rewards.
/// </summary>
public class Quest : IDomainEntity
{
    private Quest()
    {
        Title = string.Empty;
        Description = string.Empty;
        QuestGiverName = string.Empty;
        StartLocation = new Location();
    }

    public Guid Id { get; private set; }
    public string Title { get; set; }
    public string Description { get; set; }

    // Quest Giver
    public string QuestGiverName { get; set; }
    public Guid? QuestGiverId { get; set; }

    // Tag system - defines quest type/category
    public HashSet<string> Tags { get; set; } = new();

    // Component system - defines objectives, requirements, rewards
    public List<IQuestComponent> Components { get; set; } = new();

    // Location
    public Location StartLocation { get; set; }
    public Location? TurnInLocation { get; set; }

    public static Quest Create(
        string title,
        string description,
        string questGiverName,
        Location startLocation,
        HashSet<string>? tags = null)
    {
        return new Quest
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            QuestGiverName = questGiverName,
            StartLocation = startLocation,
            Tags = tags ?? new HashSet<string>()
        };
    }

    // Component helper - only GetComponent stays for convenience
    public T? GetComponent<T>() where T : IQuestComponent
    {
        return Components.OfType<T>().FirstOrDefault();
    }
}
