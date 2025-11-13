using System.Collections.Generic;

namespace RPG.Infrastructure.Mappers.Common;

internal sealed class ContainerComponentDto
{
    public int Capacity { get; init; }
    public List<InventorySlotDto>? Items { get; init; }
}
