namespace RPG.Domain.Entities.Items.ItemComponent;

public class SkillGrantComponent : IItemComponent
{
    public IList<Guid> SkillIds { get; init; } = [];
}