using Grpc.Core;
using RPG.Application.Commands;
using RPG.Application.Infrastructure;
using RPG.Application.Interfaces;
using RPG.GameServer.Protos;
using RPG.Infrastructure.Interfaces;
using RPG.GameServer.Mappers;

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
        ICommandHandler<StopRotationCommand> stopRotationHandler,
        Infrastructure.Interfaces.ILogger<CharacterServiceImpl> logger)
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

        var character = request.ToDomainCharacter();
        var cmd = new CreateCharacterCommand(character);
        var result = await _createCharacterHandler.HandleAsync(cmd, context.CancellationToken);
        if (!result.Success)
            throw new RpcException(new Status(StatusCode.Internal, result.Message ?? "Failed to create character"));

        _logger.Info($"CreateCharacter command accepted for {character.Id} ({character.Name})");
        return new CharacterIdReply { CharacterId = character.Id.ToString() };
    }

    public override async Task<CharacterActionReply> StartMovement(MovementCommandRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.CharacterId, out var characterId))
            return Failure("InvalidCharacterId", "CharacterId is invalid.");

        try
        {
            var cmd = new StartMovementCommand(characterId, request.Direction, request.PreserveFacing);
            var result = await _startMovementHandler.HandleAsync(cmd, context.CancellationToken);
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
            var result = await _stopMovementHandler.HandleAsync(new StopMovementCommand(characterId), context.CancellationToken);
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
            var result = await _startRotationHandler.HandleAsync(cmd, context.CancellationToken);
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
            var result = await _stopRotationHandler.HandleAsync(new StopRotationCommand(characterId), context.CancellationToken);
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
}
