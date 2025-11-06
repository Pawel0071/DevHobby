using FluentAssertions;
using Moq;
using RPG.Core.Services.InventoryService;
using RPG.Domain.Common;
using RPG.Domain.Entities.Items;
using RPG.Domain.Interfaces;
using RPG.Infrastructure.Interfaces;

namespace RPG.UnitTest.Core.InventoryServiceTests;

public class InventoryServiceTests
{
    private readonly Mock<ILogger<InventoryService>> _loggerMock = new();
    private readonly InventoryService _service;

    public InventoryServiceTests()
    {
        _service = new InventoryService(_loggerMock.Object);
    }

    [Fact]
    public void AddItem_ShouldStack_WhenSameItemExistsAndHasSpace()
    {
        var item = CreateItem("Potion", stackSize: 10);
        var container = CreateContainer(new InventorySlot
        {
            Item = item,
            Quantity = 5
        });

    var result = _service.AddItem(container, item);

    result.Success.Should().BeTrue();
        container.Inventory[0].Quantity.Should().Be(6);
    }

    [Fact]
    public void AddItem_ShouldUseEmptySlot_WhenNoStackableSlotExists()
    {
        var item = CreateItem("Scroll", stackSize: 5);
        var container = CreateContainer(new InventorySlot());

    var result = _service.AddItem(container, item);

    result.Success.Should().BeTrue();
        container.Inventory[0].Item.Should().Be(item);
        container.Inventory[0].Quantity.Should().Be(1);
    }

    [Fact]
    public void AddItem_ShouldFail_WhenNoFreeSlotAvailable()
    {
        var item = CreateItem("Gem", stackSize: 1);
        var container = CreateContainer(new InventorySlot
        {
            Item = item,
            Quantity = 1,
        });

    var result = _service.AddItem(container, item);

    result.Success.Should().BeFalse();
    result.Error.Should().Be(ErrorCodeDefinition.NoFreeSlot);
    result.Message.Should().Be("Brak wolnych slotów w ekwipunku.");
    }

    [Fact]
    public void RemoveItem_ShouldDecreaseQuantity_WhenItemExists()
    {
        var item = CreateItem("Arrow", stackSize: 20);
        var container = CreateContainer(new InventorySlot
        {
            Item = item,
            Quantity = 5
        });

    var result = _service.RemoveItem(container, item);

    result.Success.Should().BeTrue();
        container.Inventory[0].Quantity.Should().Be(4);
        container.Inventory[0].Item.Should().Be(item);
    }

    [Fact]
    public void RemoveItem_ShouldClearSlot_WhenQuantityBecomesZero()
    {
        var item = CreateItem("Key", stackSize: 1);
        var container = CreateContainer(new InventorySlot
        {
            Item = item,
            Quantity = 1
        });

    var result = _service.RemoveItem(container, item);

    result.Success.Should().BeTrue();
        container.Inventory[0].Quantity.Should().Be(0);
        container.Inventory[0].Item.Should().BeNull();
    }

    [Fact]
    public void RemoveItem_ShouldFail_WhenItemNotFound()
    {
        var item = CreateItem("Map");
        var container = CreateContainer(new InventorySlot());

    var result = _service.RemoveItem(container, item);

    result.Success.Should().BeFalse();
    result.Error.Should().Be(ErrorCodeDefinition.ItemNotFound);
    result.Message.Should().Be("Nie znaleziono przedmiotu w ekwipunku.");
    }

    [Fact]
    public void Contains_ShouldReturnTrue_WhenItemExists()
    {
        var item = CreateItem("Torch");
        var container = CreateContainer(new InventorySlot
        {
            Item = item,
            Quantity = 1
        });

    var result = _service.Contains(container, item);

    result.Success.Should().BeTrue();
    }

    [Fact]
    public void IsFull_ShouldReturnTrue_WhenAllSlotsAreFull()
    {
        var item = CreateItem("Coin", stackSize: 5);
        var container = CreateContainer(new InventorySlot
        {
            Item = item,
            Quantity = 5
        });

    var result = _service.IsFull(container);

    result.Success.Should().BeTrue();
    }

    [Fact]
    public void FreeSpace_ShouldReturnCorrectCount()
    {
        var item = CreateItem("Herb", stackSize: 10);
        var container = CreateContainer(
            new InventorySlot(),
            new InventorySlot { Item = item, Quantity = 5 },
            new InventorySlot { Item = item, Quantity = 10 }
        );

    var result = _service.FreeSpace(container);

    result.Result.Should().Be(2); // 1 empty + 1 partially filled
    }

    // Helpers

    private Item CreateItem(string name, int stackSize = 1) => new(Guid.NewGuid(),"Miscellaneous")
    {
        Id = Guid.NewGuid(),
        Name = name,
        TypeCode = "Miscellaneous",
        StackSize = stackSize
    };

    private IInventoryContainer CreateContainer(params InventorySlot[] slots)
    {
        var mock = new Mock<IInventoryContainer>();
        mock.Setup(c => c.Inventory).Returns(slots.ToList());
        return mock.Object;
    }
}