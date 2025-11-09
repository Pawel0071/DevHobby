using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using RPG.Abstractions;
using RPG.Abstractions.Interfaces;
using RPG.Application.Events;
using RPG.Core.Interfaces;
using RPG.Core.Interfaces.NpcServices;
using RPG.Domain.Entities;
using RPG.Domain.Entities.MapObjects;
using RPG.Domain.Entities.Npcs;
using RPG.Domain.Models;
using RPG.GameServer.Protos;
using DomainWorldState = RPG.Domain.Entities.WorldState;
using ProtoWorldState = RPG.GameServer.Protos.WorldState;

namespace RPG.GameServer.Controllers;

public class WorldServiceImpl : WorldService.WorldServiceBase
{
	private readonly ICharacterStateBroadcaster _stateBroadcaster;
	private readonly INpcAiService _npcAiService;
    private readonly IWorldSessionManager _worldSessionManager;
    private readonly RPG.Infrastructure.Interfaces.ILogger<WorldServiceImpl> _logger;

	public WorldServiceImpl(
        ICharacterStateBroadcaster stateBroadcaster,
        INpcAiService npcAiService,
        IWorldSessionManager worldSessionManager,
		RPG.Infrastructure.Interfaces.ILogger<WorldServiceImpl> logger)
	{
		_stateBroadcaster = stateBroadcaster;
		_npcAiService = npcAiService;
        _worldSessionManager = worldSessionManager;
        _logger = logger;
	}

	public override Task<WorldReply> GetWorldState(WorldRequest request, ServerCallContext context)
	{
		var snapshots = _stateBroadcaster.GetSnapshots();
		var npcSnapshots = _npcAiService.GetNpcSnapshots();

		var worldState = new ProtoWorldState
		{
			Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
		};

		worldState.VisibleCharacters.AddRange(snapshots.Select(ToPlayerCharacter));
		worldState.VisibleNPCs.AddRange(npcSnapshots.Select(ToNpcCharacter));

		return Task.FromResult(new WorldReply { State = worldState });
	}

	public override async Task<JoinWorldReply> JoinWorld(JoinWorldRequest request, ServerCallContext context)
	{
		if (!Guid.TryParse(request.SessionId, out var sessionId))
		{
			throw new RpcException(new Status(StatusCode.InvalidArgument, "sessionId must be a valid GUID."));
		}

		Guid? preferredWorldId = null;
		if (!string.IsNullOrWhiteSpace(request.PreferredWorldId))
		{
			if (!Guid.TryParse(request.PreferredWorldId, out var parsedWorldId))
			{
				throw new RpcException(new Status(StatusCode.InvalidArgument, "preferredWorldId must be a valid GUID."));
			}

			preferredWorldId = parsedWorldId;
		}

		try
		{
			var joinResult = await _worldSessionManager.JoinWorldAsync(sessionId, preferredWorldId, context.CancellationToken).ConfigureAwait(false);
			_logger.Info($"Session {sessionId} joined world {joinResult.World.WorldId}.");

			return new JoinWorldReply
			{
				Snapshot = ToSnapshot(joinResult.World),
				SpawnLocation = ToProtoLocation(joinResult.SpawnLocation)
			};
		}
		catch (InvalidOperationException ex)
		{
			throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
		}
	}

	public override async Task<WorldReply> LeaveWorld(WorldMembershipRequest request, ServerCallContext context)
	{
		if (!Guid.TryParse(request.SessionId, out var sessionId))
		{
			throw new RpcException(new Status(StatusCode.InvalidArgument, "sessionId must be a valid GUID."));
		}

		try
		{
			await _worldSessionManager.LeaveWorldAsync(sessionId, context.CancellationToken).ConfigureAwait(false);
			_logger.Info($"Session {sessionId} left its active world.");

			var world = await _worldSessionManager.GetWorldForSessionAsync(sessionId, context.CancellationToken).ConfigureAwait(false);
			return new WorldReply
			{
				State = ConvertToLegacyState(world)
			};
		}
		catch (InvalidOperationException ex)
		{
			throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
		}
	}

	public override async Task StreamWorldState(WorldStreamRequest request, IServerStreamWriter<WorldUpdate> responseStream, ServerCallContext context)
	{
		if (!Guid.TryParse(request.SessionId, out var sessionId))
		{
			throw new RpcException(new Status(StatusCode.InvalidArgument, "sessionId must be a valid GUID."));
		}

		Guid resolvedWorldId;
		if (!string.IsNullOrWhiteSpace(request.WorldId))
		{
			if (!Guid.TryParse(request.WorldId, out resolvedWorldId))
			{
				throw new RpcException(new Status(StatusCode.InvalidArgument, "worldId must be a valid GUID."));
			}
		}
		else
		{
			var world = await _worldSessionManager.GetWorldForSessionAsync(sessionId, context.CancellationToken).ConfigureAwait(false);
			resolvedWorldId = world.WorldId;
		}

		var interval = request.IntervalMilliseconds > 0 ? request.IntervalMilliseconds : 1000;

		try
		{
			while (!context.CancellationToken.IsCancellationRequested)
			{
				var world = await _worldSessionManager.GetWorldAsync(resolvedWorldId, context.CancellationToken).ConfigureAwait(false);
				var update = new WorldUpdate
				{
					Snapshot = ToSnapshot(world)
				};

				await responseStream.WriteAsync(update).ConfigureAwait(false);
				await Task.Delay(interval, context.CancellationToken).ConfigureAwait(false);
			}
		}
		catch (InvalidOperationException ex)
		{
			throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
		}
		catch (OperationCanceledException)
		{
			// Stream cancelled by client - no action required.
		}
	}

	private static PlayerCharacter ToPlayerCharacter(CharacterStateSnapshot snapshot)
	{
		var baseCharacter = new BaseCharacter
		{
			Id = snapshot.CharacterId.ToString(),
			Name = string.Empty,
			Level = 0,
			MaxHealth = 0,
			CurrentHealth = 0,
			MaxMana = 0,
			CurrentMana = 0,
			Stats = new Stats(),
			Position = new Protos.Location
			{
				X = snapshot.Location.Position.X,
				Y = snapshot.Location.Position.Y,
				Z = snapshot.Location.Position.Z
			},
			IsMoving = snapshot.IsMoving,
			IsRotating = snapshot.IsRotating,
			Rotation = snapshot.Rotation
		};

		return new PlayerCharacter
		{
			BaseCharacter = baseCharacter
		};
	}

	private static PlayerCharacter ToNpcCharacter(NpcStateSnapshot snapshot)
	{
		var baseCharacter = new BaseCharacter
		{
			Id = snapshot.NpcId.ToString(),
			Name = snapshot.Name,
			Level = 0,
			MaxHealth = 0,
			CurrentHealth = 0,
			MaxMana = 0,
			CurrentMana = 0,
			Stats = new Stats(),
			Position = new Protos.Location
			{
				X = snapshot.Location.Position.X,
				Y = snapshot.Location.Position.Y,
				Z = snapshot.Location.Position.Z
			},
			IsMoving = snapshot.IsMoving,
			IsRotating = snapshot.IsRotating,
			Rotation = snapshot.Rotation
		};

		return new PlayerCharacter
		{
			BaseCharacter = baseCharacter
		};
	}

	private static WorldSnapshot ToSnapshot(DomainWorldState world)
	{
		var snapshot = new WorldSnapshot
		{
			Metadata = new WorldMetadata
			{
				WorldId = world.WorldId.ToString(),
				WorldName = world.WorldName
			},
			LastUpdated = new DateTimeOffset(world.LastUpdated).ToUnixTimeMilliseconds()
		};

		snapshot.Characters.AddRange(world.Characters.Select(ToProtoCharacter));
		snapshot.Npcs.AddRange(world.Npcs.Select(ToProtoNpc));
		snapshot.MapObjects.AddRange(world.MapObjects.Select(ToProtoMapObject));
		return snapshot;
	}

	private static ProtoWorldState ConvertToLegacyState(DomainWorldState world)
	{
		var legacyState = new ProtoWorldState
		{
			Timestamp = new DateTimeOffset(world.LastUpdated).ToUnixTimeMilliseconds()
		};

		legacyState.VisibleCharacters.AddRange(world.Characters.Select(character => new PlayerCharacter
		{
			BaseCharacter = new BaseCharacter
			{
				Id = character.Id.ToString(),
				Name = character.Name,
				Position = ToProtoLocation(character.CurrentLocation)
			}
		}));

		legacyState.VisibleNPCs.AddRange(world.Npcs.Select(npc => new PlayerCharacter
		{
			BaseCharacter = new BaseCharacter
			{
				Id = npc.Id.ToString(),
				Name = npc.Name,
				Position = ToProtoLocation(npc.CurrentLocation)
			}
		}));

		return legacyState;
	}

	private static WorldCharacter ToProtoCharacter(Character character)
	{
		var proto = new WorldCharacter
		{
			CharacterId = character.Id.ToString(),
			SessionId = character.SessionId.ToString(),
			DisplayName = character.Name,
			Location = ToProtoLocation(character.CurrentLocation),
			IsOnline = character.IsOnline,
			IsInCombat = character.IsInCombat,
			LastUpdated = new DateTimeOffset(character.LastUpdated).ToUnixTimeMilliseconds()
		};

		proto.StatusEffects.AddRange(character.StatusEffects);
		return proto;
	}

	private static WorldNpc ToProtoNpc(Npc npc)
	{
		var proto = new WorldNpc
		{
			NpcId = npc.Id.ToString(),
			Name = npc.Name,
			Location = ToProtoLocation(npc.CurrentLocation),
			IsAlive = npc.IsAlive,
			LastUpdated = new DateTimeOffset(npc.LastUpdated).ToUnixTimeMilliseconds(),
			RespawnAt = npc.RespawnAt.HasValue ? new DateTimeOffset(npc.RespawnAt.Value).ToUnixTimeMilliseconds() : 0
		};

		proto.Tags.AddRange(npc.Tags);
		return proto;
	}

	private static WorldMapObject ToProtoMapObject(MapObject mapObject)
	{
		var proto = new WorldMapObject
		{
			MapObjectId = mapObject.Id.ToString(),
			Name = mapObject.Name,
			DisplayName = mapObject.DisplayName,
			Location = ToProtoLocation(mapObject.Location),
			IsActive = mapObject.IsActive,
			LastUpdated = new DateTimeOffset(mapObject.LastUpdated).ToUnixTimeMilliseconds()
		};

		proto.Tags.AddRange(mapObject.Tags);
		foreach (var kvp in mapObject.State)
		{
			proto.State.Add(kvp.Key, kvp.Value);
		}

		return proto;
	}

	private static Protos.Location ToProtoLocation(RPG.Domain.Entities.Location location)
	{
		return new Protos.Location
		{
			X = location.Position.X,
			Y = location.Position.Y,
			Z = location.Position.Z
		};
	}
}
