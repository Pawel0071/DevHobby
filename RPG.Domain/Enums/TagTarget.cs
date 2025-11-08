namespace RPG.Domain.Enums;

/// <summary>
///     Identifies which domain entity type a tag definition targets.
/// </summary>
public enum TagTarget
{
    Unknown = 0,
    Item,
    Skill,
    Quest,
    Npc,
    MapObject
}
