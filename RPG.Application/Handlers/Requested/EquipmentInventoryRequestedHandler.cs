// filepath: /Volumes/Data/Repositories/DevHobby/RPG.Application/Handlers/Requested/EquipmentInventoryRequestedHandler.cs
using RPG.Abstractions.Interfaces;
using RPG.Application.Events;
using RPG.Application.Interfaces;
using RPG.Core.Interfaces;
using RPG.Domain.Common;
using RPG.Domain.Models;
using RPG.Domain.Models.Items;
using RPG.Domain.Models.Items.ItemComponent;
using RPG.Infrastructure.Interfaces;

namespace RPG.Application.Handlers.Requested;

public sealed class EquipmentInventoryRequestedHandler : IRequestedEventHandler
{
    private readonly IModelRepository _repository;
    private readonly IInventoryService _inventoryService;
    private readonly IEquipmentService _equipmentService;
    private readonly IStatsService _statsService;
    private readonly IDictionaryRegistry<TagDefinition> _tagRegistry;
    private readonly IGameEventDispatcher _dispatcher;
    private readonly ILogger<EquipmentInventoryRequestedHandler> _logger;

    public EquipmentInventoryRequestedHandler(
        IModelRepository repository,
        IInventoryService inventoryService,
        IEquipmentService equipmentService,
        IStatsService statsService,
        IDictionaryRegistry<TagDefinition> tagRegistry,
        IGameEventDispatcher dispatcher,
        ILogger<EquipmentInventoryRequestedHandler> logger)
    {
        _repository = repository;
        _inventoryService = inventoryService;
        _equipmentService = equipmentService;
        _statsService = statsService;
        _tagRegistry = tagRegistry;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public bool CanHandle(IGameEvent evt) => evt is ItemEquipRequestedEvent or ItemUnequipRequestedEvent or ItemPickupRequestedEvent or DropItemRequestedEvent or PutItemToBankRequestedEvent or GetItemFromBankRequestedEvent or UseItemRequestedEvent;

    public async Task HandleAsync(IGameEvent evt, CancellationToken ct)
    {
        switch (evt)
        {
            case ItemEquipRequestedEvent e: await HandleEquip(e, ct); break;
            case ItemUnequipRequestedEvent ue: await HandleUnequip(ue, ct); break;
            case ItemPickupRequestedEvent pu: await HandlePickup(pu, ct); break;
            case DropItemRequestedEvent dr: await HandleDrop(dr, ct); break;
            case PutItemToBankRequestedEvent pb: await HandlePutBank(pb, ct); break;
            case GetItemFromBankRequestedEvent gb: await HandleGetBank(gb, ct); break;
            case UseItemRequestedEvent ur: await HandleUse(ur, ct); break;
        }
    }

    private static Item? FindItemInContainers(Character c, Guid itemId)
    {
        return c.BackpackInventory.Select(s => s.Item).Concat(c.BankStorage.Select(s => s.Item)).FirstOrDefault(i => i?.Id == itemId);
    }

    private async Task HandleEquip(ItemEquipRequestedEvent e, CancellationToken ct)
    {
        var character = await _repository.GetByIdAsync<Character>(e.CharacterId, ct);
        if (character == null) return;
        var item = FindItemInContainers(character, e.ItemId);
        if (item is null) return;
        if (!_inventoryService.Contains(character.GetBackpackInventoryContainer(), item).Result) return;
        if (character.Level < item.RequiredLevel) return;
        var previously = character.Equipments[e.Slot];
        var equipResult = previously is not null ? _equipmentService.Swap(character, e.Slot, item) : _equipmentService.Equip(character, e.Slot, item);
        if (!equipResult.Success) return;
        var statsContainer = item.GetComponent<StatsComponent>()?.Stats;
        if (statsContainer == null) return;
        if (previously is not null) _statsService.UnModifyStats(character, statsContainer);
        _statsService.ModifyStats(character, statsContainer);
        await _repository.UpsertAsync(character, ct);
        await _dispatcher.DispatchAsync(new ItemEquippedEvent(e.Meta, e.CharacterId, e.Slot, item), ct);
    }

    private async Task HandleUnequip(ItemUnequipRequestedEvent e, CancellationToken ct)
    {
        var character = await _repository.GetByIdAsync<Character>(e.CharacterId, ct);
        var item = character?.Equipments[e.Slot];
        if (item is null) return;
        var invFull = _inventoryService.IsFull(character.GetBackpackInventoryContainer()).Result; if (invFull) { await _dispatcher.DispatchAsync(new InventoryFullEvent(e.Meta, e.CharacterId, item), ct); return; }
        var result = _equipmentService.Unequip(character, e.Slot);
        if (!result.Success) return;
        var statsContainer = item.GetComponent<StatsComponent>()?.Stats; if (statsContainer != null) _statsService.UnModifyStats(character, statsContainer);
        await _repository.UpsertAsync(character, ct);
        await _dispatcher.DispatchAsync(new ItemUnequippedEvent(e.Meta, e.CharacterId, e.Slot, item), ct);
    }

    private async Task HandlePickup(ItemPickupRequestedEvent e, CancellationToken ct)
    {
        var character = await _repository.GetByIdAsync<Character>(e.CharacterId, ct);
        if (character == null) return;
        // TODO: implement with Map objects repository or drop if not yet supported
    }

    private async Task HandleDrop(DropItemRequestedEvent e, CancellationToken ct)
    {
        var character = await _repository.GetByIdAsync<Character>(e.CharacterId, ct);
        if (character == null) return;
        var item = FindItemInContainers(character, e.ItemId);
        if (item is null) return;
        if (!_inventoryService.Contains(character.GetBackpackInventoryContainer(), item).Result) return;
        var result = _inventoryService.RemoveItem(character.GetBackpackInventoryContainer(), item);
        if (!result.Success) return;
        await _repository.UpsertAsync(character, ct);
        await _dispatcher.DispatchAsync(new ItemDroppedEvent(e.Meta, e.CharacterId, item), ct);
    }

    private async Task HandlePutBank(PutItemToBankRequestedEvent e, CancellationToken ct)
    {
        var character = await _repository.GetByIdAsync<Character>(e.CharacterId, ct);
        if (character == null) return;
        var item = FindItemInContainers(character, e.ItemId);
        if (item is null) return;
        if (!_inventoryService.Contains(character.GetBackpackInventoryContainer(), item).Result) return;
        if (_inventoryService.IsFull(character.GetBankStorageContainer()).Result)
        { await _dispatcher.DispatchAsync(new InventoryFullEvent(e.Meta, e.CharacterId, item), ct); return; }
        _inventoryService.RemoveItem(character.GetBackpackInventoryContainer(), item);
        var add = _inventoryService.AddItem(character.GetBankStorageContainer(), item);
        if (!add.Success) return;
        await _repository.UpsertAsync(character, ct);
        await _dispatcher.DispatchAsync(new ItemPutToBankEvent(e.Meta, e.CharacterId, item), ct);
    }

    private async Task HandleGetBank(GetItemFromBankRequestedEvent e, CancellationToken ct)
    {
        var character = await _repository.GetByIdAsync<Character>(e.CharacterId, ct);
        if (character == null) return;
        var item = FindItemInContainers(character, e.ItemId);
        if (item is null) return;
        if (!_inventoryService.Contains(character.GetBackpackInventoryContainer(), item).Result) return;
        if (_inventoryService.IsFull(character.GetBackpackInventoryContainer()).Result)
        { await _dispatcher.DispatchAsync(new InventoryFullEvent(e.Meta, e.CharacterId, item), ct); return; }
        _inventoryService.RemoveItem(character.GetBankStorageContainer(), item);
        var add = _inventoryService.AddItem(character.GetBackpackInventoryContainer(), item);
        if (!add.Success) return;
        await _repository.UpsertAsync(character, ct);
        await _dispatcher.DispatchAsync(new ItemGottenFromBankEvent(e.Meta, e.CharacterId, item), ct);
    }

    private async Task HandleUse(UseItemRequestedEvent e, CancellationToken ct)
    {
        var character = await _repository.GetByIdAsync<Character>(e.CharacterId, ct);
        if (character == null) return;
        var item = FindItemInContainers(character, e.ItemId);
        if (item is null) return;
        const string ConsumableTag = "item:consumable";
        var hasConsumableTag = item.Tags.Contains(ConsumableTag) || item.Tags.Contains("consumable");
        if (!hasConsumableTag || !_tagRegistry.IsValid(ConsumableTag)) return;
        var removeResult = _inventoryService.RemoveItem(character.GetBackpackInventoryContainer(), item);
        if (!removeResult.Success) return;
        await _repository.UpsertAsync(character, ct);
        await _dispatcher.DispatchAsync(new ItemUsedEvent(e.Meta, e.CharacterId, item), ct);
    }
}

