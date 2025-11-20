using FluentAssertions;
using Moq;
using RPG.Core.Services.InventoryService;
using RPG.Domain.Common;
using RPG.Domain.Interfaces;
using RPG.Domain.Models.Items;
using RPG.Infrastructure.Interfaces;

namespace RPG.UnitTest.Core.InventoryServiceTests;

public class InventoryServiceEdgeCasesTests
{
    private readonly InventoryService _service;

    public InventoryServiceEdgeCasesTests()
    {
        _service = new InventoryService(new Mock<ILogger<InventoryService>>().Object);
    }

    [Fact]
    public void AddItem_ShouldReturnFail_WhenContainerIsNull()
    {
        var item = new Item(Guid.NewGuid(), "Misc") { Id = Guid.NewGuid(), Name = "X" };

        var result = _service.AddItem(null!, item);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public void FreeSpace_ShouldReturnZero_WhenContainerHasNoSlots()
    {
        var mock = new Mock<IInventoryContainer>();
        mock.Setup(c => c.Inventory).Returns(new List<InventorySlot>());

        var result = _service.FreeSpace(mock.Object.Inventory);

        result.Result.Should().Be(0);
    }
}
