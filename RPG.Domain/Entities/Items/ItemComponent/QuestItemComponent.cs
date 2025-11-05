namespace RPG.Domain.Entities.Items.ItemComponent;

public class QuestItemComponent : IItemComponent
{
    public Guid QuestId { get; init; }
    public Guid StepId { get; init; }
}