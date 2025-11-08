using RPG.Domain.Entities;
using RPG.Domain.Entities.Npcs;

namespace RPG.Domain.AI;

/// <summary>
///     Context for dialogue behavior trees.
///     Contains information about the player and conversation state.
/// </summary>
public class DialogueContext
{
    public Npc Self { get; set; } = null!;
    public Character Player { get; set; } = null!;

    // Player state
    public int PlayerLevel { get; set; }
    public List<Guid> PlayerActiveQuests { get; set; } = new();
    public List<Guid> PlayerCompletedQuests { get; set; } = new();
    public int PlayerReputation { get; set; }

    // Conversation state
    public string? CurrentDialogueNodeId { get; set; }
    public List<string> DialogueHistory { get; set; } = new(); // Track visited nodes
    public int ConversationTurn { get; set; } = 0;

    // Result of dialogue tree execution
    public string? SelectedDialogueText { get; set; }
    public List<DialogueChoice> AvailableChoices { get; set; } = new();

    // Blackboard for custom data
    public Dictionary<string, object> Blackboard { get; set; } = new();

    public void SetBlackboardValue(string key, object value)
    {
        Blackboard[key] = value;
    }

    public T? GetBlackboardValue<T>(string key)
    {
        return Blackboard.TryGetValue(key, out var value) && value is T typedValue ? typedValue : default;
    }
}

/// <summary>
///     Represents a dialogue choice available to the player.
/// </summary>
public class DialogueChoice
{
    public string ChoiceText { get; set; } = string.Empty;
    public string NextNodeId { get; set; } = string.Empty;
    public Action? OnSelected { get; set; } // Callback when choice is selected
}
