using RPG.Application.Commands;
using RPG.Application.Events;
using RPG.Application.Interfaces;
using RPG.Core.Application.Handlers;
using RPG.Core.Interfaces;
using RPG.Domain.Common;
using RPG.Domain.Entities.Items;
using RPG.Domain.Entities.Items.ItemComponent;
using RPG.Domain.Enums;
using RPG.Domain.Interfaces;
using RPG.Infrastructure.Interfaces;

namespace RPG.Application.Handlers;

public class CharacterCommandHandler : ICommandHandler<EquipItemCommand>,
        ICommandHandler<UnequipItemCommand>,
        ICommandHandler<PutItemToBankCommand>,
        ICommandHandler<GetItemFromBankCommand>,
        ICommandHandler<UseItemCommand>,
        ICommandHandler<DropItemCommand>,
        ICommandHandler<PickUpItemCommand>,
        ICommandHandler<GainExperienceCommand>,
        ICommandHandler<LevelUpCommand>

{
    private readonly ICharacterRepository _characterRepo;
    private readonly IInventoryService _inventoryService;
    private readonly IEquipmentService _equipmentService;
    private readonly IStatsService _statsService;
    private readonly IGameEventDispatcher _eventDispatcher;
    private readonly IDictionaryRegistry<ItemTagDefinition> _tagRegistry;

    public CharacterCommandHandler(
        ICharacterRepository characterRepo,
        IInventoryService inventoryService,
        IEquipmentService equipmentService,
        IStatsService statsService,
        IGameEventDispatcher eventDispatcher,
        IDictionaryRegistry<ItemTagDefinition> tagRegistry
        )
    {
        _characterRepo = characterRepo;
        _inventoryService = inventoryService;
        _equipmentService = equipmentService;
        _statsService = statsService;
        _eventDispatcher = eventDispatcher;
        _tagRegistry = tagRegistry;
    }

    public async Task<CommandResult> HandleAsync(EquipItemCommand command)
    {
        var character = await _characterRepo.GetByIdAsync(command.CharacterId);

        if (!_inventoryService.Contains(character.BackpackInventory, command.Item).Result)
            return CommandResult.Fail(CommandError.ItemNotFound, "Przedmiot nie znajduje się w ekwipunku.");

        if (character.Level < command.Item.RequiredLevel)
            return CommandResult.Fail(CommandError.LevelToLow, "Poziom postaci jest zbyt niski.");

        var previouslyEquipped = character.Equipments[command.Slot];

        var equipResult = previouslyEquipped is not null
            ? _equipmentService.Swap(character, command.Slot, command.Item)
            : _equipmentService.Equip(character, command.Slot, command.Item);

        if (!equipResult.Success)
            return CommandResult.Fail(CommandError.InvalidOperation, equipResult.Message, equipResult);

        _eventDispatcher.Dispatch(new ItemEquippedEvent(command.CharacterId, command.Slot, command.Item));
        var statsContainer = command.Item.GetComponent<StatsComponent>()?.Stats;
        if (statsContainer == null)
        {
            return CommandResult.Fail(CommandError.ItemNotHaveStatsDef, equipResult.Message, equipResult);
        }
        if (previouslyEquipped is not null)
        {
            var unmodify = _statsService.UnModifyStats(character, statsContainer);
            if (unmodify.Success && unmodify.Stats is not null)
                character.ModifiedStats = unmodify.Stats;

        }
        
        var modify = _statsService.ModifyStats(character, statsContainer);
        if (modify.Success && modify.Stats is not null)
            character.ModifiedStats = modify.Stats;


        return CommandResult.Ok();
    }

    public async Task<CommandResult> HandleAsync(UnequipItemCommand command)
    {
        var character = await _characterRepo.GetByIdAsync(command.CharacterId);
        var item = character.Equipments[command.Slot];
        
        if (_inventoryService.IsFull(character.BankStorage).Result)
        {
            _eventDispatcher.Dispatch(new InventoryFullEvent(command.CharacterId, item));
            return CommandResult.Fail(CommandError.InventoryFull, "");
        }
        var result  = _equipmentService.Unequip(character, command.Slot);

        if (result.Success)
            _eventDispatcher.Dispatch(new ItemUnequippedEvent(command.CharacterId, command.Slot, item));
        else
            return CommandResult.Fail(CommandError.InvalidOperation, result.Message, result);
        var statsContainer = item.GetComponent<StatsComponent>()?.Stats;
        if (statsContainer == null)
        {
            return CommandResult.Fail(CommandError.ItemNotHaveStatsDef, result .Message, result );
        }
        
        var statsResult = _statsService.UnModifyStats(character, statsContainer);
        if (statsResult is { Success: true, Stats: not null })
        {
            character.ModifiedStats = statsResult.Stats;
        }

        return CommandResult.Ok() ;
    }

    public async Task<CommandResult> HandleAsync(PutItemToBankCommand command)
    {
        var character = await _characterRepo.GetByIdAsync(command.CharacterId);
        if (!_inventoryService.Contains(character.BackpackInventory, command.Item).Result)
            return CommandResult.Fail(CommandError.ItemNotFound, "");
        
        if (_inventoryService.IsFull(character.BankStorage).Result)
        {
            _eventDispatcher.Dispatch(new InventoryFullEvent(command.CharacterId, command.Item));
            return CommandResult.Fail(CommandError.InventoryFull, "");
        }

        _inventoryService.RemoveItem(character.BackpackInventory, command.Item);
        var result  = _inventoryService.AddItem(character.BankStorage, command.Item);

        if (result.Success)
            _eventDispatcher.Dispatch(new ItemPutToBankEvent(command.CharacterId, command.Item));

        return CommandResult.Ok() ;
    }

    public async Task<CommandResult> HandleAsync(GetItemFromBankCommand command)
    {
        var character = await _characterRepo.GetByIdAsync(command.CharacterId);
        if (!_inventoryService.Contains(character.BackpackInventory, command.Item).Result)
            return CommandResult.Fail(CommandError.ItemNotFound, "");
        if (_inventoryService.IsFull(character.BankStorage).Result)
        {
            _eventDispatcher.Dispatch(new InventoryFullEvent(command.CharacterId, command.Item));
            return CommandResult.Fail(CommandError.InventoryFull, "");
        }

        _inventoryService.RemoveItem(character.BankStorage, command.Item);
        var result = _inventoryService.AddItem(character.BackpackInventory, command.Item);

        if (result.Success)
            _eventDispatcher.Dispatch(new ItemGottenFromBankEvent(command.CharacterId, command.Item));

        return CommandResult.Ok() ;
    }

    public async Task<CommandResult> HandleAsync(DropItemCommand command)
    {
        var character = await _characterRepo.GetByIdAsync(command.CharacterId);
        if (!_inventoryService.Contains(character.BackpackInventory, command.Item).Result)
            return CommandResult.Fail(CommandError.ItemNotFound, "");

        var result = _inventoryService.RemoveItem(character.BackpackInventory, command.Item);

        if (result.Success)
            _eventDispatcher.Dispatch(new ItemDroppedEvent(command.CharacterId, command.Item));

        return CommandResult.Ok() ;
    }

    public async Task<CommandResult> HandleAsync(PickUpItemCommand command)
    {
        var character = await _characterRepo.GetByIdAsync(command.CharacterId);
        
        var result = _inventoryService.AddItem(character.BackpackInventory, command.Item);

        if (result.Success)
            _eventDispatcher.Dispatch(new ItemPickupEvent(command.CharacterId, command.Item));

        return CommandResult.Ok() ;
    }
    
    public async Task<CommandResult> HandleAsync(UseItemCommand command)
    {
        var character = await _characterRepo.GetByIdAsync(command.CharacterId);

        if (!command.Item.Tags.Contains("consumable") || !_tagRegistry.IsValid("consumable"))
        {
            return CommandResult.Fail(CommandError.InvalidOperation, "Przedmiot nie jest typu consumable.");
        }

        var result = _inventoryService.RemoveItem(character.BackpackInventory, command.Item);

        if (result.Success)
        {
            _eventDispatcher.Dispatch(new ItemUsedEvent(command.CharacterId, command.Item));
        }

        return CommandResult.Ok();
    }

    public async Task<CommandResult> HandleAsync(GainExperienceCommand command)
    {
        var character = await _characterRepo.GetByIdAsync(command.CharacterId);
        character.ExperienceToNextLevel -= command.Amount;
        if (character.ExperienceToNextLevel <= 0)
        {
            var levelUpResult = await HandleAsync(new LevelUpCommand(command.CharacterId));
            var newAmount = -character.ExperienceToNextLevel;
            character.Experience = newAmount;
            return !levelUpResult.Success 
                ? CommandResult.Ok() 
                : CommandResult.Fail(CommandError.InvalidOperation,"");
        }

        character.Experience += command.Amount;
        return CommandResult.Ok() ;
    }

    public async Task<CommandResult> HandleAsync(LevelUpCommand command)
    {
        var character = await _characterRepo.GetByIdAsync(command.CharacterId);
        character.Level++;
        // get data from static table
        return CommandResult.Ok() ;
    }
}
