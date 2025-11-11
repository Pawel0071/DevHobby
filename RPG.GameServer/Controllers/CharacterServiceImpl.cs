using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using RPG.Application.Commands;
using RPG.Application.Handlers;
using RPG.Application.Interfaces;
using RPG.Domain.Entities;
using RPG.Domain.Enums;
using RPG.Domain.Interfaces;
using RPG.GameServer.Protos;
using DomainCharacterClass = RPG.Domain.Enums.CharacterClass;
using DomainLocation = RPG.Domain.Entities.Location;

namespace RPG.GameServer.Controllers;

public class CharacterServiceImpl : CharacterService.CharacterServiceBase
{
	private readonly ICharacterRepository _characterRepository;
	private readonly ICommandHandler<StartMovementCommand> _startMovementHandler;
	private readonly ICommandHandler<StopMovementCommand> _stopMovementHandler;
	private readonly ICommandHandler<StartRotationCommand> _startRotationHandler;
	private readonly ICommandHandler<StopRotationCommand> _stopRotationHandler;
	private readonly ILogger<CharacterServiceImpl> _logger;

	public CharacterServiceImpl(
		ICharacterRepository characterRepository,
		ICommandHandler<StartMovementCommand> startMovementHandler,
		ICommandHandler<StopMovementCommand> stopMovementHandler,
		ICommandHandler<StartRotationCommand> startRotationHandler,
		ICommandHandler<StopRotationCommand> stopRotationHandler,
		ILogger<CharacterServiceImpl> logger)
	{
		_characterRepository = characterRepository;
		_startMovementHandler = startMovementHandler;
		_stopMovementHandler = stopMovementHandler;
		_startRotationHandler = startRotationHandler;
		_stopRotationHandler = stopRotationHandler;
		_logger = logger;
	}

	public override async Task<CharacterIdReply> CreateCharacter(CharacterRequest request, ServerCallContext context)
	{
		if (request?.Character?.BaseCharacter == null)
		{
			throw new RpcException(new Status(StatusCode.InvalidArgument, "Character payload is required."));
		}

		var baseCharacter = request.Character.BaseCharacter;

		if (string.IsNullOrWhiteSpace(baseCharacter.Name))
		{
			throw new RpcException(new Status(StatusCode.InvalidArgument, "Character name is required."));
		}

		var sessionId = ParseGuidOrDefault(request.Character.SessionId, Guid.NewGuid());
		var characterId = ParseGuidOrDefault(baseCharacter.Id, Guid.NewGuid());
		var characterClass = MapCharacterClass(request.Character.CharacterClass);

		var character = new Character(sessionId, characterClass)
		{
			Id = characterId,
			Name = baseCharacter.Name,
			PlayerId = Guid.NewGuid(),
			SessionId = sessionId,
			Level = baseCharacter.Level > 0 ? baseCharacter.Level : 1,
			Experience = 0,
			ExperienceToNextLevel = 100,
			CurrentHealth = baseCharacter.CurrentHealth > 0 ? baseCharacter.CurrentHealth : baseCharacter.MaxHealth,
			MaxHealth = baseCharacter.MaxHealth > 0 ? baseCharacter.MaxHealth : 100,
			CurrentResource = baseCharacter.CurrentMana,
			MaxResource = baseCharacter.MaxMana
		};

		if (baseCharacter.Position != null)
		{
			var position = baseCharacter.Position;
			var hasWorldId = Guid.TryParse(position.WorldId, out var parsedWorldId);
			var worldId = hasWorldId ? parsedWorldId : Guid.NewGuid();
			var mapId = position.MapId ?? string.Empty;
			var zoneName = position.ZoneName ?? string.Empty;

			var location = DomainLocation.Create(
				(float)position.X,
				(float)position.Y,
				(float)position.Z,
				worldId,
				mapId,
				zoneName);

			location.WorldId = hasWorldId ? parsedWorldId : null;
			location.Rotation = position.Rotation != 0 ? position.Rotation : baseCharacter.Rotation;
			character.SetCurrentLocation(location);
		}

		character.SetMovementState(baseCharacter.IsMoving);
		character.SetRotationState(baseCharacter.IsRotating);

		MapStats(baseCharacter.Stats, character);
		EnsureMovementStats(character);

		await _characterRepository.SaveAsync(character);

		_logger.LogInformation("Created character {CharacterId} ({Name})", character.Id, character.Name);

		return new CharacterIdReply { CharacterId = character.Id.ToString() };
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
			_logger.LogWarning(ex, "Character {CharacterId} not found when starting movement.", characterId);
			return Failure("CharacterNotFound", ex.Message);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Unexpected error when starting movement for {CharacterId}.", characterId);
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
			_logger.LogWarning(ex, "Character {CharacterId} not found when stopping movement.", characterId);
			return Failure("CharacterNotFound", ex.Message);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Unexpected error when stopping movement for {CharacterId}.", characterId);
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
			_logger.LogWarning(ex, "Character {CharacterId} not found when starting rotation.", characterId);
			return Failure("CharacterNotFound", ex.Message);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Unexpected error when starting rotation for {CharacterId}.", characterId);
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
			_logger.LogWarning(ex, "Character {CharacterId} not found when stopping rotation.", characterId);
			return Failure("CharacterNotFound", ex.Message);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Unexpected error when stopping rotation for {CharacterId}.", characterId);
			return Failure("StopRotationFailed", "Unexpected error while stopping rotation.");
		}
	}

	private static void MapStats(Stats? stats, Character character)
	{
		if (stats == null)
		{
			return;
		}

		var assignments = new Dictionary<StatsProperty, int>
		{
			{ StatsProperty.Strength, stats.Strength },
			{ StatsProperty.Agility, stats.Agility },
			{ StatsProperty.Intelligence, stats.Intelligence },
			{ StatsProperty.Wisdom, stats.Wisdom },
			{ StatsProperty.Dexterity, stats.Dexterity },
			{ StatsProperty.Vitality, stats.Vitality },
			{ StatsProperty.MagicResist, stats.MagicResist },
			{ StatsProperty.NatureResist, stats.NatureResist },
			{ StatsProperty.MisticResist, stats.MisticResist },
			{ StatsProperty.Armor, stats.Armor },
			{ StatsProperty.CritChance, stats.CritChance },
			{ StatsProperty.HitChance, stats.HitChance },
			{ StatsProperty.AttackSpeed, stats.AttackSpeed },
			{ StatsProperty.MoveSpeed, stats.MoveSpeed }
		};

		foreach (var (property, value) in assignments)
		{
			if (value <= 0)
			{
				continue;
			}

			character.BaseStats[property] = value;
			character.ModifiedStats[property] = value;
		}
	}

	private static void EnsureMovementStats(Character character)
	{
		if (!character.ModifiedStats.TryGetValue(StatsProperty.MoveSpeed, out var moveSpeed) || moveSpeed <= 0)
		{
			character.BaseStats[StatsProperty.MoveSpeed] = 5;
			character.ModifiedStats[StatsProperty.MoveSpeed] = 5;
		}
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
