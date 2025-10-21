using RPG.Core.Application.Commands;
using RPG.Core.Application.Events;
using RPG.Core.Application.Interfaces;
using RPG.Core.Domain.Entities.Common;
using RPG.Core.Infrastructure.Repositories;
using RPG.Core.Infrastructure.Services.EquipmentService;
using RPG.Core.Infrastructure.Services.InventoryService;
using RPG.Core.Interfaces;

namespace RPG.Core.Application.Handlers;

public class CharacterCommandHandler : ICommandHandler<EquipItemCommand>,
        ICommandHandler<UnequipItemCommand>,
        ICommandHandler<PutItemToBankCommand>,
        ICommandHandler<GetItemFromBankCommand>,
        ICommandHandler<UseItemCommand>,
        ICommandHandler<DropItemCommand>,
        ICommandHandler<PickUpItemCommand>
        
{
    private readonly ICharacterRepository _characterRepo;
    private readonly IInventoryService _inventoryService;
    private readonly IEquipmentService _equipmentService;
    private readonly IGameEventDispatcher _eventDispatcher;

    public CharacterCommandHandler(
        ICharacterRepository characterRepo,
        IInventoryService inventoryService,
        IEquipmentService equipmentService,
        IGameEventDispatcher eventDispatcher)
    {
        _characterRepo = characterRepo;
        _inventoryService = inventoryService;
        _equipmentService = equipmentService;
        _eventDispatcher = eventDispatcher;
    }

    public bool Handle(EquipItemCommand command)
    {
        var character = _characterRepo.GetById(command.CharacterId);
        if (!_inventoryService.Contains(character.BackpackInventory, command.Item)) return false;
        
        var success = (_inventoryService.Contains(character.BackpackInventory, command.Item)) 
            ? _equipmentService.Swap(character.Equipment, character.BackpackInventory, command.Slot, command.Item)
            : _equipmentService.Equip(character.Equipment, character.BackpackInventory, command.Slot, command.Item);
        
        if (success)
            _eventDispatcher.Dispatch(new ItemEquippedEvent(command.CharacterId, command.Slot, command.Item));

        return success;
    }

    public bool Handle(UnequipItemCommand command)
    {
        var character = _characterRepo.GetById(command.CharacterId);
        var item = character.Equipment.GetEquippedItem(command.Slot);
        if (_inventoryService.IsFull(character.BankStorage))
        {
            _eventDispatcher.Dispatch(new InventoryFullEvent(command.CharacterId, item));
            return false;
        }
        var success = _equipmentService.Unequip(character.Equipment, character.BackpackInventory, command.Slot);

        if (success && item != null)
            _eventDispatcher.Dispatch(new ItemUnequippedEvent(command.CharacterId, command.Slot, item));

        return success;
    }

    public bool Handle(PutItemToBankCommand command)
    {
        var character = _characterRepo.GetById(command.CharacterId);
        if (!_inventoryService.Contains(character.BackpackInventory, command.Item)) return false;
        if (_inventoryService.IsFull(character.BankStorage))
        {
            _eventDispatcher.Dispatch(new InventoryFullEvent(command.CharacterId, command.Item));
            return false;
        }

        _inventoryService.RemoveItem(character.BackpackInventory, command.Item);
        var success = _inventoryService.AddItem(character.BankStorage, command.Item);

        if (success)
            _eventDispatcher.Dispatch(new ItemPutToBankEvent(command.CharacterId, command.Item));

        return success;
    }

    public bool Handle(GetItemFromBankCommand command)
    {
        var character = _characterRepo.GetById(command.CharacterId);
        if (!_inventoryService.Contains(character.BankStorage, command.Item)) return false;
        if (_inventoryService.IsFull(character.BackpackInventory))
        {
            _eventDispatcher.Dispatch(new InventoryFullEvent(command.CharacterId, command.Item));
            return false;
        }

        _inventoryService.RemoveItem(character.BankStorage, command.Item);
        var success = _inventoryService.AddItem(character.BackpackInventory, command.Item);

        if (success)
            _eventDispatcher.Dispatch(new ItemGottenFromBankEvent(command.CharacterId, command.Item));

        return success;
    }

    public bool Handle(DropItemCommand command)
    {
        var character = _characterRepo.GetById(command.CharacterId);
        if (!_inventoryService.Contains(character.BackpackInventory, command.Item)) return false;

        var success = _inventoryService.RemoveItem(character.BackpackInventory, command.Item);

        if (success)
            _eventDispatcher.Dispatch(new ItemDroppedEvent(command.CharacterId, command.Item));

        return success;
    }

    public bool Handle(PickUpItemCommand command)
    {
        var character = _characterRepo.GetById(command.CharacterId);
        
        var success = _inventoryService.AddItem(character.BackpackInventory, command.Item);

        if (success)
            _eventDispatcher.Dispatch(new ItemPickupEvent(command.CharacterId, command.Item));

        return success;
    }
    
    public bool Handle(UseItemCommand command)
    {
        var character = _characterRepo.GetById(command.CharacterId);
        if (command.Item.Type == ItemType.Consumable);
        var success = _inventoryService.RemoveItem(character.BackpackInventory, command.Item);

        if (success)
            _eventDispatcher.Dispatch(new ItemUsedEvent(command.CharacterId, command.Item));

        return success;
    }
}