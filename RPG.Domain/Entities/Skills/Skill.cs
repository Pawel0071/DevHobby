using System.Text.Json.Serialization;
using RPG.Domain.Common;

namespace RPG.Domain.Entities.Skills;

/// <summary>
///     Domain entity representing a character skill/ability.
///     Uses tags for categorization and components for effects and behaviors.
///     Pure data entity - logic handled by services.
/// </summary>
public class Skill : IDomainEntity
{
    [JsonConstructor]
    public Skill()
    {
        Name = string.Empty;
        Description = string.Empty;
        Tags = new HashSet<string>();
        Components = new List<ISkillComponent>();
    }

    [JsonInclude]
    public Guid Id { get; private set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string IconId { get; set; } = string.Empty;

    // Tags for categorization
    public HashSet<string> Tags { get; set; }

    // Components for effects and behaviors
    public List<ISkillComponent> Components { get; set; }

    public static Skill Create(string name, string description = "")
    {
        return new Skill { Id = Guid.NewGuid(), Name = name, Description = description };
    }

    // Helper method to get component of specific type
    public T? GetComponent<T>() where T : class, ISkillComponent
    {
        return Components.OfType<T>().FirstOrDefault();
    }
}
