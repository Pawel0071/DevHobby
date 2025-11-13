namespace RPG.Domain.Models.Items.ItemComponent;

public class SkillGrantComponent : IItemComponent
{
    public IList<Guid> SkillIds { get; init; } = new List<Guid>();
}
