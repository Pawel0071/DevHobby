using RPG.Domain.Enums;

namespace RPG.Domain.Models.MapObjects.MapObjectComponents;

/// <summary>
///     Component for resource nodes (mining, herbalism, fishing, logging).
///     Pure data - harvest/respawn logic handled by services.
/// </summary>
public class ResourceNodeComponent : IMapObjectComponent
{
    public ResourceNodeType ResourceType { get; set; }
    public int MinYield { get; set; }
    public int MaxYield { get; set; }
    public int RespawnTimeSeconds { get; set; }
    public DateTime? LastHarvestTime { get; set; }
    public bool IsHarvested { get; set; }
    public int RequiredSkillLevel { get; set; }
    public Guid? RequiredToolId { get; set; }
    public List<Guid> PossibleLootIds { get; set; } = new();
}
