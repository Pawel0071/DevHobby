namespace RPG.Domain.Models.Npcs.NpcComponents;

/// <summary>
///     Component for NPCs that can engage in dialogue.
///     Uses dialogue behavior trees for dynamic conversations.
/// </summary>
public class DialogueComponent : INpcComponent
{
    /// <summary>
    ///     Name of the dialogue script to use (e.g., "quest-giver", "merchant", "guard")
    /// </summary>
    public string DialogueScript { get; set; } = string.Empty;

    /// <summary>
    ///     Parameters for the dialogue script (e.g., questId, requiredLevel)
    /// </summary>
    public Dictionary<string, object> ScriptParameters { get; set; } = new();

    /// <summary>
    ///     Greeting text shown when conversation starts (fallback if no script)
    /// </summary>
    public string GreetingText { get; set; } = string.Empty;

    /// <summary>
    ///     Farewell text shown when conversation ends (fallback if no script)
    /// </summary>
    public string FarewellText { get; set; } = string.Empty;
}
