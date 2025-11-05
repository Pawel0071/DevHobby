using FluentAssertions;
using Moq;
using RPG.Core.Interfaces;
using RPG.Core.Services.EquipmentService;
using RPG.Core.Services.InventoryService;
using RPG.Domain.Common;
using RPG.Domain.Entities;
using RPG.Domain.Entities.Items;
using RPG.Domain.Enums;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Logger;

namespace RPG.UnitTest.Core.EquipmentSerivce;

public class EquipmentServiceTests
{
    private readonly Mock<IInventoryService> _inventoryMock = new();
    private readonly Mock<ISkillService> _skillMock = new();
    private readonly Mock<ILogger<EquipmentService>> _loggerMock = new();
    private readonly EquipmentService _service;

    public EquipmentServiceTests()
    {
        _service = new EquipmentService(_inventoryMock.Object, _skillMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Equip_ShouldSucceed_WhenItemIsInInventoryAndSlotIsEmpty()
    {
        var character = CreateCharacter();
        var item = CreateItem("Sword");
        const EquipmentSlot slot = EquipmentSlot.Weapon1;

        _inventoryMock.Setup(i => i.Contains(character.BackpackInventory, item)).Returns(true);
        _inventoryMock.Setup(i => i.RemoveItem(character.BackpackInventory, item)).Returns(InventoryResult.Ok());

        var result = _service.Equip(character, slot, item);

        result.Should().BeEquivalentTo(EquipmentResult.Ok());
        character.Equipments[slot].Should().Be(item);
    }

    [Fact]
    public void Equip_ShouldFail_WhenItemNotInInventory()
    {
        var character = CreateCharacter();
        var item = CreateItem("Shield");
        const EquipmentSlot slot = EquipmentSlot.Weapon2;

        _inventoryMock.Setup(i => i.Contains(character.BackpackInventory, item)).Returns(false);

        var result = _service.Equip(character, slot, item);

        result.Success.Should().BeFalse();
        result.Result.Should().Be(EquipmentError.ItemCannotBeEquip);
        character.Equipments[slot].Should().BeNull();
    }

    [Fact]
    public void Unequip_ShouldSucceed_WhenSlotHasItem()
    {
        var character = CreateCharacter();
        var item = CreateItem("Helmet");
        const EquipmentSlot slot = EquipmentSlot.Head;
        character.Equipments[slot] = item;

        _inventoryMock.Setup(i => i.AddItem(character.BackpackInventory, item)).Returns(InventoryResult.Ok());

        var result = _service.Unequip(character, slot);

        result.Should().BeEquivalentTo(EquipmentResult.Ok());
        character.Equipments[slot].Should().BeNull();
    }

    [Fact]
    public void Unequip_ShouldFail_WhenSlotIsEmpty()
    {
        var character = CreateCharacter();
        const EquipmentSlot slot = EquipmentSlot.Head;

        var result = _service.Unequip(character, slot);

        result.Success.Should().BeFalse();
        result.Result.Should().Be(EquipmentError.InvalidOperation);
    }

    [Fact]
    public void Swap_ShouldEquip_WhenSlotIsEmpty()
    {
        var character = CreateCharacter();
        var item = CreateItem("Bow");
        const EquipmentSlot slot = EquipmentSlot.Weapon1;

        _inventoryMock.Setup(i => i.Contains(character.BackpackInventory, item)).Returns(true);
        _inventoryMock.Setup(i => i.RemoveItem(character.BackpackInventory, item)).Returns(InventoryResult.Ok());

        var result = _service.Swap(character, slot, item);

        result.Should().BeEquivalentTo(EquipmentResult.Ok());
        character.Equipments[slot].Should().Be(item);
    }

    [Fact]
    public void IsEquipped_ShouldReturnTrue_WhenSlotHasItem()
    {
        var character = CreateCharacter();
        const EquipmentSlot slot = EquipmentSlot.Weapon1;
        character.Equipments[slot] = CreateItem("Axe");

        var result = _service.IsEquipped(character, slot);

        result.Should().BeTrue();
    }

    [Fact]
    public void GetAllEquippedItems_ShouldReturnCorrectItems()
    {
        var character = CreateCharacter();
        character.Equipments[EquipmentSlot.Head] = CreateItem("Helmet");
        character.Equipments[EquipmentSlot.Weapon1] = CreateItem("Sword");

        var result = _service.GetAllEquippedItems(character);

        var enumerable = result as Item[] ?? result.ToArray();
        enumerable.Should().HaveCount(2);
        enumerable.Should().Contain(i => i.Name == "Helmet");
        enumerable.Should().Contain(i => i.Name == "Sword");
    }

    private static Character CreateCharacter() => new( Guid.NewGuid(), CharacterClass.Monk, null, null )
    {
        Id = Guid.NewGuid(),
        Name = "Rogue",
    };

    private static Item CreateItem(string name) => new(Guid.NewGuid(),"Weapon 1H")
    {
        Id = Guid.NewGuid(),
        Name = name,
        TypeCode = "Weapon 1H" 
    };
}
