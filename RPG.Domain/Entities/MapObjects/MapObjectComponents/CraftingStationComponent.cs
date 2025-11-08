using RPG.Domain.Enums;

namespace RPG.Domain.Entities.MapObjects.MapObjectComponents;

/// <summary>
///     Component for crafting stations (blacksmith, alchemy, enchanting, etc.).
///     Pure data - crafting logic handled by services.
/// </summary>
public class CraftingStationComponent : IMapObjectComponent
{
    public CraftingStationType StationType { get; set; }
    public List<string> AvailableRecipes { get; set; } = new();
    public int MinimumSkillLevel { get; set; }
    public bool RequiresTools { get; set; }
    public List<Guid> RequiredToolIds { get; set; } = new();
}
