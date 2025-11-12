using Grpc.Core;
using RPG.Application.Commands;
using RPG.Application.Interfaces;
using RPG.Domain.Containers;
using RPG.Domain.Enums;
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

    public CharacterServiceImpl(
        ICommandHandler<CreateCharacterCommand> createCharacterHandler,
        ICommandHandler<StartMovementCommand> startMovementHandler,
        ICommandHandler<StopMovementCommand> stopMovementHandler,
        ICommandHandler<StartRotationCommand> startRotationHandler,
        ICommandHandler<StopRotationCommand> stopRotationHandler, Infrastructure.Interfaces.ILogger<CharacterServiceImpl> logger)
    {
        _createCharacterHandler = createCharacterHandler;
        _startMovementHandler = startMovementHandler;
        _stopMovementHandler = stopMovementHandler;
        _startRotationHandler = startRotationHandler;
        _stopRotationHandler = stopRotationHandler;
        _logger = logger;
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

        var cmd = new CreateCharacterCommand(
            CharacterId: characterId,
            SessionId: sessionId,
            Name: baseCharacter.Name,
            CharacterClass: characterClass,
            Level: baseCharacter.Level > 0 ? baseCharacter.Level : 1,
            MaxHealth: baseCharacter.MaxHealth > 0 ? baseCharacter.MaxHealth : 100,
            MaxResource: baseCharacter.MaxMana > 0 ? baseCharacter.MaxMana : 60,
            X: (float?)baseCharacter.Position?.X,
            Y: (float?)baseCharacter.Position?.Y,
            Z: (float?)baseCharacter.Position?.Z,
            WorldId: Guid.TryParse(baseCharacter.Position?.WorldId, out var parsedWorldId) ? parsedWorldId : null,
            MapId: baseCharacter.Position?.MapId,
            ZoneName: baseCharacter.Position?.ZoneName,
            Rotation: baseCharacter.Position?.Rotation,
            IsMoving: baseCharacter.IsMoving,
            IsRotating: baseCharacter.IsRotating,
            Stats: baseCharacter.Stats is null ? null : new StatsContainer(new Dictionary<StatsProperty, int>
            {
                [StatsProperty.Strength] = baseCharacter.Stats.Strength,
                [StatsProperty.Agility] = baseCharacter.Stats.Agility,
                [StatsProperty.Intelligence] = baseCharacter.Stats.Intelligence,
                [StatsProperty.Wisdom] = baseCharacter.Stats.Wisdom,
                [StatsProperty.Dexterity] = baseCharacter.Stats.Dexterity,
                [StatsProperty.Vitality] = baseCharacter.Stats.Vitality,
                [StatsProperty.MagicResist] = baseCharacter.Stats.MagicResist,
                [StatsProperty.NatureResist] = baseCharacter.Stats.NatureResist,
                [StatsProperty.MisticResist] = baseCharacter.Stats.MisticResist,
                [StatsProperty.Armor] = baseCharacter.Stats.Armor,
                [StatsProperty.CritChance] = baseCharacter.Stats.CritChance,
                [StatsProperty.HitChance] = baseCharacter.Stats.HitChance,
                [StatsProperty.AttackSpeed] = baseCharacter.Stats.AttackSpeed,
                [StatsProperty.MoveSpeed] = baseCharacter.Stats.MoveSpeed
            })
        );

        var result = await _createCharacterHandler.HandleAsync(cmd);
        if (!result.Success)
            throw new RpcException(new Status(StatusCode.Internal, result.Message ?? "Failed to create character"));

        _logger.Info($"Created character {characterId} ({baseCharacter.Name})");
        return new CharacterIdReply { CharacterId = characterId.ToString() };
    }

    public override async Task<CharacterActionReply> StartMovement(MovementCommandRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.CharacterId, out var characterId))
        {
            return Failure("InvalidCharacterId", "CharacterId is invalid.");
        }

        try
        {
            var result = await _startMovementHandler.HandleAsync(new StartMovementCommand(characterId, request.Direction, request.PreserveFacing));
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
        {
            return Failure("InvalidCharacterId", "CharacterId is invalid.");
        }

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
        {
            return Failure("InvalidCharacterId", "CharacterId is invalid.");
        }

        try
        {
            var result = await _startRotationHandler.HandleAsync(new StartRotationCommand(characterId, request.Direction));
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
        {
            return Failure("InvalidCharacterId", "CharacterId is invalid.");
        }

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
