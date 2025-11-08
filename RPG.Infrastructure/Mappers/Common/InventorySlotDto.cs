using RPG.Infrastructure.Documents;

namespace RPG.Infrastructure.Mappers.Common;

internal sealed class InventorySlotDto
{
    public ItemDocument? Item { get; init; }
    public int Quantity { get; init; }
}
