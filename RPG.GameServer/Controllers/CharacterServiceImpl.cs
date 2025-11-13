using Grpc.Core;
using RPG.Application.Commands;
using RPG.Application.Infrastructure;
using RPG.Application.Interfaces;
using RPG.GameServer.Protos;
using DomainCharacterClass = RPG.Domain.Enums.CharacterClass;
using RPG.Infrastructure.Interfaces;

namespace RPG.GameServer.Controllers;

public class CharacterServiceImpl : CharacterService.CharacterServiceBase
{
    private readonly ICommandHandler<CreateCharacterCommand> _createCharacterHandler;
    private readonly ICommandHandler<StartMovementCommand> _startMovementHandler;
    private readonly ICommandHandler<StopMovementCommand> _stopMovementHandler;
    private readonly ICommandHandler<StartRotationCommand> _startRotationHandler;
    private readonly ICommandHandler<StopRotationCommand> _stopRotationHandler;
    private readonly Infrastructure.Interfaces.ILogger<CharacterServiceImpl> _logger;
    private readonly IModelRepository _repository;

    public CharacterServiceImpl(
        ICommandHandler<CreateCharacterCommand> createCharacterHandler,
        ICommandHandler<StartMovementCommand> startMovementHandler,
        ICommandHandler<StopMovementCommand> stopMovementHandler,
        ICommandHandler<StartRotationCommand> startRotationHandler,
        ICommandHandler<StopRotationCommand> stopRotationHandler,
        Infrastructure.Interfaces.ILogger<CharacterServiceImpl> logger,
        IModelRepository repository)
    {
        _createCharacterHandler = createCharacterHandler;
        _startMovementHandler = startMovementHandler;
        _stopMovementHandler = stopMovementHandler;
        _startRotationHandler = startRotationHandler;
        _stopRotationHandler = stopRotationHandler;
        _logger = logger;
        _repository = repository;
    }

    public override async Task<CharacterIdReply> CreateCharacter(CharacterRequest request, ServerCallContext context)
    {
        if (request?.Character?.BaseCharacter == null)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Character payload is required."));

        var baseCharacter = request.Character.BaseCharacter;
        if (string.IsNullOrWhiteSpace(baseCharacter.Name))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Character name is required."));

        var sessionId = ParseGuidOrDefault(request.Character.SessionId, Guid.NewGuid());
        var characterId = ParseGuidOrDefault(baseCharacter.Id, Guid.NewGuid());
        var characterClass = MapCharacterClass(request.Character.CharacterClass);

        // Build domain Character and wrap into CreateCharacterCommand
        var character = new RPG.Domain.Models.Character(sessionId, characterClass)
        {
            Id = characterId,
            Name = baseCharacter.Name,
            Level = baseCharacter.Level > 0 ? baseCharacter.Level : 1,
            MaxHealth = baseCharacter.MaxHealth > 0 ? baseCharacter.MaxHealth : 100,
            MaxResource = baseCharacter.MaxMana > 0 ? baseCharacter.MaxMana : 60
        };

        // Stats
        if (baseCharacter.Stats is not null)
        {
            character.BaseStats[RPG.Domain.Enums.StatsProperty.Strength] = baseCharacter.Stats.Strength;
            character.BaseStats[RPG.Domain.Enums.StatsProperty.Agility] = baseCharacter.Stats.Agility;
            character.BaseStats[RPG.Domain.Enums.StatsProperty.Intelligence] = baseCharacter.Stats.Intelligence;
            character.BaseStats[RPG.Domain.Enums.StatsProperty.Wisdom] = baseCharacter.Stats.Wisdom;
            character.BaseStats[RPG.Domain.Enums.StatsProperty.Dexterity] = baseCharacter.Stats.Dexterity;
            character.BaseStats[RPG.Domain.Enums.StatsProperty.Vitality] = baseCharacter.Stats.Vitality;
            character.BaseStats[RPG.Domain.Enums.StatsProperty.MagicResist] = baseCharacter.Stats.MagicResist;
            character.BaseStats[RPG.Domain.Enums.StatsProperty.NatureResist] = baseCharacter.Stats.NatureResist;
            character.BaseStats[RPG.Domain.Enums.StatsProperty.MisticResist] = baseCharacter.Stats.MisticResist;
            character.BaseStats[RPG.Domain.Enums.StatsProperty.Armor] = baseCharacter.Stats.Armor;
            character.BaseStats[RPG.Domain.Enums.StatsProperty.CritChance] = baseCharacter.Stats.CritChance;
            character.BaseStats[RPG.Domain.Enums.StatsProperty.HitChance] = baseCharacter.Stats.HitChance;
            character.BaseStats[RPG.Domain.Enums.StatsProperty.AttackSpeed] = baseCharacter.Stats.AttackSpeed;
            character.BaseStats[RPG.Domain.Enums.StatsProperty.MoveSpeed] = baseCharacter.Stats.MoveSpeed;

            // Initialize ModifiedStats at least for MoveSpeed, so movement can proceed
            character.ModifiedStats[RPG.Domain.Enums.StatsProperty.MoveSpeed] = baseCharacter.Stats.MoveSpeed;
        }

        // Location
        var x = (float)(baseCharacter.Position?.X ?? 0);
        var y = (float)(baseCharacter.Position?.Y ?? 0);
        var z = (float)(baseCharacter.Position?.Z ?? 0);
        var worldId = Guid.TryParse(baseCharacter.Position?.WorldId, out var parsedWorldId) ? parsedWorldId : Guid.Empty;
        var location = RPG.Domain.Models.Location.Create(x, y, z, worldId,
            baseCharacter.Position?.MapId ?? string.Empty,
            baseCharacter.Position?.ZoneName ?? string.Empty);
        var rotation = baseCharacter.Position != null ? baseCharacter.Position.Rotation : baseCharacter.Rotation;
        location.Rotation = rotation;
        character.SetCurrentLocation(location);

        var cmd = new CreateCharacterCommand(character);

        var result = await _createCharacterHandler.HandleAsync(cmd);
        if (!result.Success)
            throw new RpcException(new Status(StatusCode.Internal, result.Message ?? "Failed to create character"));

        // Wait until character is persisted by RequestedEventsHostedService
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var timeout = TimeSpan.FromSeconds(2);
        while (sw.Elapsed < timeout)
        {
            var persisted = await _repository.GetByIdAsync<RPG.Domain.Models.Character>(characterId, context.CancellationToken);
            if (persisted != null) break;
            await Task.Delay(25, context.CancellationToken);
        }

        _logger.Info($"Created character {characterId} ({baseCharacter.Name})");
        return new CharacterIdReply { CharacterId = characterId.ToString() };
    }

    public override async Task<CharacterActionReply> StartMovement(MovementCommandRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.CharacterId, out var characterId))
            return Failure("InvalidCharacterId", "CharacterId is invalid.");

        try
        {
            var cmd = new StartMovementCommand(characterId, request.Direction, request.PreserveFacing);
            var result = await _startMovementHandler.HandleAsync(cmd);
            return ToReply(result);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.Warn($"Character {characterId} not found when starting movement. {ex.Message}");
            return Failure("CharacterNotFound", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.Error($"Unexpected error when starting movement for {characterId}.", ex);
            return Failure("StartMovementFailed", "Unexpected error while starting movement.");
        }
    }

    public override async Task<CharacterActionReply> StopMovement(CharacterIdRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.CharacterId, out var characterId))
            return Failure("InvalidCharacterId", "CharacterId is invalid.");

        try
        {
            var result = await _stopMovementHandler.HandleAsync(new StopMovementCommand(characterId));
            return ToReply(result);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.Warn($"Character {characterId} not found when stopping movement. {ex.Message}");
            return Failure("CharacterNotFound", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.Error($"Unexpected error when stopping movement for {characterId}.", ex);
            return Failure("StopMovementFailed", "Unexpected error while stopping movement.");
        }
    }

    public override async Task<CharacterActionReply> StartRotation(MovementCommandRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.CharacterId, out var characterId))
            return Failure("InvalidCharacterId", "CharacterId is invalid.");

        try
        {
            var cmd = new StartRotationCommand(characterId, request.Direction);
            var result = await _startRotationHandler.HandleAsync(cmd);
            return ToReply(result);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.Warn($"Character {characterId} not found when starting rotation. {ex.Message}");
            return Failure("CharacterNotFound", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.Error($"Unexpected error when starting rotation for {characterId}.", ex);
            return Failure("StartRotationFailed", "Unexpected error while starting rotation.");
        }
    }

    public override async Task<CharacterActionReply> StopRotation(CharacterIdRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.CharacterId, out var characterId))
            return Failure("InvalidCharacterId", "CharacterId is invalid.");

        try
        {
            var result = await _stopRotationHandler.HandleAsync(new StopRotationCommand(characterId));
            return ToReply(result);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.Warn($"Character {characterId} not found when stopping rotation. {ex.Message}");
            return Failure("CharacterNotFound", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.Error($"Unexpected error when stopping rotation for {characterId}.", ex);
            return Failure("StopRotationFailed", "Unexpected error while stopping rotation.");
        }
    }

    private static CharacterActionReply ToReply(CommandResult result)
    {
        if (result.Success)
        {
            return new CharacterActionReply { Success = true };
        }

        return new CharacterActionReply
        {
            Success = false,
            ErrorCode = result.Result.ToString(),
            Message = result.Message ?? string.Empty
        };
    }

    private static CharacterActionReply Failure(string error, string message)
    {
        return new CharacterActionReply
        {
            Success = false,
            ErrorCode = error,
            Message = message
        };
    }

    private static DomainCharacterClass MapCharacterClass(Protos.CharacterClass protoClass)
    {
        var name = protoClass.ToString();
        return Enum.TryParse<DomainCharacterClass>(name, true, out var parsed)
            ? parsed
            : DomainCharacterClass.Warrior;
    }

    private static Guid ParseGuidOrDefault(string? value, Guid fallback)
    {
        return Guid.TryParse(value, out var parsed) ? parsed : fallback;
    }
}
