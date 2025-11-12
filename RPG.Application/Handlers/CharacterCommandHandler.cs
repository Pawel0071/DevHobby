using System.Diagnostics;
using System.Numerics;
using RPG.Application.Commands;
using RPG.Application.Events;
using RPG.Application.Diagnostics;
using RPG.Application.Interfaces;
using RPG.Core.Interfaces;
using RPG.Domain.Common;
using RPG.Domain.Entities;
using RPG.Domain.Entities.Items.ItemComponent;
using RPG.Domain.Enums;
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
    ICommandHandler<LevelUpCommand>,
    ICommandHandler<StartMovementCommand>,
    ICommandHandler<StopMovementCommand>,
    ICommandHandler<StartRotationCommand>,
    ICommandHandler<StopRotationCommand>,
    ICommandHandler<CreateCharacterCommand>

{
    private const float DefaultMovementDeltaSeconds = 0.1f;
    private readonly IModelRepository _characterRepo;
    private readonly IEquipmentService _equipmentService;
    private readonly IGameEventDispatcher _eventDispatcher;
    private readonly IInventoryService _inventoryService;
    private readonly IMovementService _movementService;
    private readonly IStatsService _statsService;
    private readonly IDictionaryRegistry<TagDefinition> _tagRegistry;

    public CharacterCommandHandler(
        IModelRepository characterRepo,
        IInventoryService inventoryService,
        IEquipmentService equipmentService,
        IStatsService statsService,
        IMovementService movementService,
        IGameEventDispatcher eventDispatcher,
        IDictionaryRegistry<TagDefinition> tagRegistry)
    {
        _characterRepo = characterRepo;
        _inventoryService = inventoryService;
        _equipmentService = equipmentService;
        _statsService = statsService;
        _movementService = movementService;
        _eventDispatcher = eventDispatcher;
        _tagRegistry = tagRegistry;
    }

    private async Task<Character> GetCharacterOrThrow(Guid characterId)
    {
        var character = await _characterRepo.GetByIdAsync<Character>(characterId);
        if (character is null)
        {
            throw new KeyNotFoundException($"Character {characterId} not found");
        }
        return character;
    }

    public async Task<CommandResult> HandleAsync(StartMovementCommand command)
    {
        using var activity = StartCommandActivity("CharacterCommandHandler.StartMovement", command.CharacterId);
        activity?.SetTag("rpg.movement.direction", command.Direction);

        var character = await GetCharacterOrThrow(command.CharacterId);

        if (!TryGetDirectionVector(command.Direction, out var direction))
        {
            return CommandResult.Fail(CommandError.InvalidOperation, "Niepoprawny kierunek ruchu.");
        }

        if (character.ModifiedStats.TryGetValue(StatsProperty.MoveSpeed, out var currentMoveSpeed) && currentMoveSpeed <= 0)
        {
            return CommandResult.Fail(CommandError.InvalidOperation, "Postać nie posiada prędkości ruchu.");
        }

    var moveResult = _movementService.Move(character, direction, DefaultMovementDeltaSeconds, preserveFacing: command.PreserveFacing);
        if (!moveResult.Success)
        {
            return CommandResult.Fail(CommandError.InvalidOperation, moveResult.Message, moveResult);
        }

        await _characterRepo.UpsertAsync(character);
        await _eventDispatcher.DispatchAsync(new CharacterMovedEvent(command.CharacterId, character.CurrentLocation));

        return CommandResult.Ok();
    }

    public async Task<CommandResult> HandleAsync(StopMovementCommand command)
    {
        using var activity = StartCommandActivity("CharacterCommandHandler.StopMovement", command.CharacterId);

        var character = await GetCharacterOrThrow(command.CharacterId);

        var stopResult = _movementService.Stop(character);
        if (!stopResult.Success)
        {
            return CommandResult.Fail(CommandError.InvalidOperation, stopResult.Message, stopResult);
        }

        var location = stopResult.Result ?? character.CurrentLocation;
        await _eventDispatcher.DispatchAsync(new CharacterMovementStoppedEvent(command.CharacterId, location));

        return CommandResult.Ok();
    }

    public async Task<CommandResult> HandleAsync(StartRotationCommand command)
    {
        using var activity = StartCommandActivity("CharacterCommandHandler.StartRotation", command.CharacterId);
        activity?.SetTag("rpg.movement.direction", command.Direction);

        var character = await GetCharacterOrThrow(command.CharacterId);

        if (!TryGetDirectionVector(command.Direction, out var direction))
        {
            return CommandResult.Fail(CommandError.InvalidOperation, "Niepoprawny kierunek rotacji.");
        }

        var rotationResult = _movementService.Rotate(character, direction);
        if (!rotationResult.Success)
        {
            return CommandResult.Fail(CommandError.InvalidOperation, rotationResult.Message, rotationResult);
        }

        var rotation = rotationResult.Result;

        await _characterRepo.UpsertAsync(character);
        await _eventDispatcher.DispatchAsync(new CharacterRotationStartedEvent(command.CharacterId, rotation, character.CurrentLocation));

        return CommandResult.Ok();
    }

    public async Task<CommandResult> HandleAsync(StopRotationCommand command)
    {
        using var activity = StartCommandActivity("CharacterCommandHandler.StopRotation", command.CharacterId);

        var character = await GetCharacterOrThrow(command.CharacterId);

        var stopRotationResult = _movementService.StopRotation(character);
        if (!stopRotationResult.Success)
        {
            return CommandResult.Fail(CommandError.InvalidOperation, stopRotationResult.Message, stopRotationResult);
        }

        var rotation = stopRotationResult.Result;
        await _eventDispatcher.DispatchAsync(new CharacterRotationStoppedEvent(command.CharacterId, rotation, character.CurrentLocation));

        return CommandResult.Ok();
    }

    public async Task<CommandResult> HandleAsync(DropItemCommand command)
    {
        var character = await GetCharacterOrThrow(command.CharacterId);
        if (!_inventoryService.Contains(character.GetBackpackInventoryContainer(), command.Item).Result)
            return CommandResult.Fail(CommandError.ItemNotFound, "");

        var result = _inventoryService.RemoveItem(character.GetBackpackInventoryContainer(), command.Item);

        if (result.Success)
            await _eventDispatcher.DispatchAsync(new ItemDroppedEvent(command.CharacterId, command.Item));

        return CommandResult.Ok();
    }

    public async Task<CommandResult> HandleAsync(EquipItemCommand command)
    {
        var character = await GetCharacterOrThrow(command.CharacterId);

        if (!_inventoryService.Contains(character.GetBackpackInventoryContainer(), command.Item).Result)
            return CommandResult.Fail(CommandError.ItemNotFound, "Przedmiot nie znajduje się w ekwipunku.");

        if (character.Level < command.Item.RequiredLevel)
            return CommandResult.Fail(CommandError.LevelToLow, "Poziom postaci jest zbyt niski.");

        var previouslyEquipped = character.Equipments[command.Slot];

        var equipResult = previouslyEquipped is not null
            ? _equipmentService.Swap(character, command.Slot, command.Item)
            : _equipmentService.Equip(character, command.Slot, command.Item);

        if (!equipResult.Success)
            return CommandResult.Fail(CommandError.InvalidOperation, equipResult.Message, equipResult);

        await _eventDispatcher.DispatchAsync(new ItemEquippedEvent(command.CharacterId, command.Slot, command.Item));
        var statsContainer = command.Item.GetComponent<StatsComponent>()?.Stats;
        if (statsContainer == null)
            return CommandResult.Fail(CommandError.ItemNotHaveStatsDef, equipResult.Message, equipResult);
        if (previouslyEquipped is not null) _statsService.UnModifyStats(character, statsContainer);

        _statsService.ModifyStats(character, statsContainer);

        return CommandResult.Ok();
    }

    public async Task<CommandResult> HandleAsync(GainExperienceCommand command)
    {
        var character = await GetCharacterOrThrow(command.CharacterId);
        character.ExperienceToNextLevel -= command.Amount;
        if (character.ExperienceToNextLevel <= 0)
        {
            var levelUpResult = await HandleAsync(new LevelUpCommand(command.CharacterId));
            var newAmount = -character.ExperienceToNextLevel;
            character.Experience = newAmount;
            return !levelUpResult.Success
                ? CommandResult.Ok()
                : CommandResult.Fail(CommandError.InvalidOperation, "");
        }

        character.Experience += command.Amount;
        return CommandResult.Ok();
    }

    public async Task<CommandResult> HandleAsync(GetItemFromBankCommand command)
    {
        var character = await GetCharacterOrThrow(command.CharacterId);
        if (!_inventoryService.Contains(character.GetBackpackInventoryContainer(), command.Item).Result)
            return CommandResult.Fail(CommandError.ItemNotFound, "");
        if (_inventoryService.IsFull(character.GetBankStorageContainer()).Result)
        {
            await _eventDispatcher.DispatchAsync(new InventoryFullEvent(command.CharacterId, command.Item));
            return CommandResult.Fail(CommandError.InventoryFull, "");
        }

        _inventoryService.RemoveItem(character.GetBankStorageContainer(), command.Item);
        var result = _inventoryService.AddItem(character.GetBackpackInventoryContainer(), command.Item);

        if (result.Success)
            await _eventDispatcher.DispatchAsync(new ItemGottenFromBankEvent(command.CharacterId, command.Item));

        return CommandResult.Ok();
    }

    public async Task<CommandResult> HandleAsync(LevelUpCommand command)
    {
        var character = await GetCharacterOrThrow(command.CharacterId);
        character.Level++;
        // get data from static table
        return CommandResult.Ok();
    }

    public async Task<CommandResult> HandleAsync(PickUpItemCommand command)
    {
        var character = await GetCharacterOrThrow(command.CharacterId);

        var result = _inventoryService.AddItem(character.GetBackpackInventoryContainer(), command.Item);

        if (result.Success)
            await _eventDispatcher.DispatchAsync(new ItemPickupEvent(command.CharacterId, command.Item));

        return CommandResult.Ok();
    }

    public async Task<CommandResult> HandleAsync(PutItemToBankCommand command)
    {
        var character = await GetCharacterOrThrow(command.CharacterId);
        if (!_inventoryService.Contains(character.GetBackpackInventoryContainer(), command.Item).Result)
            return CommandResult.Fail(CommandError.ItemNotFound, "");

        if (_inventoryService.IsFull(character.GetBankStorageContainer()).Result)
        {
            await _eventDispatcher.DispatchAsync(new InventoryFullEvent(command.CharacterId, command.Item));
            return CommandResult.Fail(CommandError.InventoryFull, "");
        }

        _inventoryService.RemoveItem(character.GetBackpackInventoryContainer(), command.Item);
        var result = _inventoryService.AddItem(character.GetBankStorageContainer(), command.Item);

        if (result.Success)
            await _eventDispatcher.DispatchAsync(new ItemPutToBankEvent(command.CharacterId, command.Item));

        return CommandResult.Ok();
    }

    public async Task<CommandResult> HandleAsync(UnequipItemCommand command)
    {
        var character = await GetCharacterOrThrow(command.CharacterId);
        var item = character.Equipments[command.Slot];

        if (_inventoryService.IsFull(character.GetBankStorageContainer()).Result)
        {
            await _eventDispatcher.DispatchAsync(new InventoryFullEvent(command.CharacterId, item));
            return CommandResult.Fail(CommandError.InventoryFull, "");
        }

        var result = _equipmentService.Unequip(character, command.Slot);

        if (result.Success)
            await _eventDispatcher.DispatchAsync(new ItemUnequippedEvent(command.CharacterId, command.Slot, item));
        else
            return CommandResult.Fail(CommandError.InvalidOperation, result.Message, result);
        var statsContainer = item.GetComponent<StatsComponent>()?.Stats;
        if (statsContainer == null) return CommandResult.Fail(CommandError.ItemNotHaveStatsDef, result.Message, result);

        _statsService.UnModifyStats(character, statsContainer);

        return CommandResult.Ok();
    }

    public async Task<CommandResult> HandleAsync(UseItemCommand command)
    {
        var character = await GetCharacterOrThrow(command.CharacterId);

        const string ConsumableTag = "item:consumable";
        var hasConsumableTag = command.Item.Tags.Contains(ConsumableTag) || command.Item.Tags.Contains("consumable");

        if (!hasConsumableTag || !_tagRegistry.IsValid(ConsumableTag))
            return CommandResult.Fail(CommandError.InvalidOperation, "Przedmiot nie jest typu consumable.");

        var result = _inventoryService.RemoveItem(character.GetBackpackInventoryContainer(), command.Item);

        if (result.Success)
        {
            await _eventDispatcher.DispatchAsync(new ItemUsedEvent(command.CharacterId, command.Item));
        }

        return CommandResult.Ok();
    }

    public async Task<CommandResult> HandleAsync(CreateCharacterCommand command)
    {
        var character = new Character(command.SessionId, command.CharacterClass)
        {
            Id = command.CharacterId,
            Name = command.Name,
            PlayerId = Guid.NewGuid(),
            SessionId = command.SessionId,
            Level = command.Level > 0 ? command.Level : 1,
            Experience = 0,
            ExperienceToNextLevel = 100,
            CurrentHealth = command.MaxHealth,
            MaxHealth = command.MaxHealth,
            CurrentResource = command.MaxResource,
            MaxResource = command.MaxResource
        };

        if (command.X.HasValue && command.Y.HasValue && command.Z.HasValue)
        {
            var location = Location.Create(
                command.X.Value,
                command.Y.Value,
                command.Z.Value,
                command.WorldId ?? Guid.NewGuid(),
                command.MapId ?? string.Empty,
                command.ZoneName ?? string.Empty);

            location.WorldId = command.WorldId;
            location.Rotation = command.Rotation ?? 0f;
            character.SetCurrentLocation(location);
        }

        character.SetMovementState(command.IsMoving);
        character.SetRotationState(command.IsRotating);

        if (command.Stats is not null)
        {
            var s = command.Stats;

            foreach (var kv in s.Stats)
            {
                var prop = kv.Key;
                var val = kv.Value;
                if (val > 0)
                {
                    character.BaseStats[prop] = val;
                    character.ModifiedStats[prop] = val;
                }
            }
        }

        if (!character.ModifiedStats.TryGetValue(StatsProperty.MoveSpeed, out var moveSpeed) || moveSpeed <= 0)
        {
            character.BaseStats[StatsProperty.MoveSpeed] = 5;
            character.ModifiedStats[StatsProperty.MoveSpeed] = 5;
        }

        await _characterRepo.UpsertAsync(character);
        return CommandResult.Ok();
    }

    private static bool TryGetDirectionVector(int direction, out Vector3 vector)
    {
        vector = direction switch
        {
            1 => new Vector3(0f, 1f, 0f), // forward
            2 => new Vector3(1f, 1f, 0f), // forward-right
            3 => new Vector3(1f, 0f, 0f), // right
            4 => new Vector3(1f, -1f, 0f), // backward-right
            5 => new Vector3(0f, -1f, 0f), // backward
            6 => new Vector3(-1f, -1f, 0f), // backward-left
            7 => new Vector3(-1f, 0f, 0f), // left
            8 => new Vector3(-1f, 1f, 0f), // forward-left
            _ => Vector3.Zero
        };

        return vector != Vector3.Zero;
    }

    private static Activity? StartCommandActivity(string operation, Guid characterId)
    {
        var activity = ApplicationDiagnostics.ActivitySource.StartActivity(operation);
        if (activity is null)
        {
            return null;
        }

        activity.SetTag("rpg.character.id", characterId);
        return activity;
    }
}
