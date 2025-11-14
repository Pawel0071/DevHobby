namespace RPG.Application.Queries;

public sealed class InventorySlotDto
{
    public Guid? ItemId { get; init; }
    public int Quantity { get; init; }
}

