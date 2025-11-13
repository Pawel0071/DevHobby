using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using RPG.Abstractions;
using RPG.Abstractions.Interfaces;
using RPG.Application.Events;
using RPG.Core.Interfaces;
using RPG.Core.Interfaces.NpcServices;
using RPG.Domain.Models;
using RPG.Domain.Models.Interaction;
using RPG.Domain.Models.MapObjects;
using RPG.Domain.Models.Npcs;
using RPG.GameServer.Protos;
using RPG.Infrastructure.Interfaces;
using DomainWorldState = RPG.Domain.Models.WorldState;
using Location = RPG.Domain.Models.Location;
using ProtoWorldState = RPG.GameServer.Protos.WorldState;

namespace RPG.GameServer.Controllers;

public class WorldServiceImpl : WorldService.WorldServiceBase
{
		private readonly ICharacterStateBroadcaster _stateBroadcaster;
		private readonly INpcAiService _npcAiService;
		private readonly IWorldSessionManager _worldSessionManager;
		private readonly IModelRepository _modelRepository;
		private readonly RPG.Infrastructure.Interfaces.ILogger<WorldServiceImpl> _logger;

	public WorldServiceImpl(
        ICharacterStateBroadcaster stateBroadcaster,
        INpcAiService npcAiService,
			IWorldSessionManager worldSessionManager,
	        IModelRepository modelRepository,
			RPG.Infrastructure.Interfaces.ILogger<WorldServiceImpl> logger)
	{
			_stateBroadcaster = stateBroadcaster;
			_npcAiService = npcAiService;
			_worldSessionManager = worldSessionManager;
			_modelRepository = modelRepository;
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
				Snapshot = await ToSnapshotAsync(joinResult.World, context.CancellationToken).ConfigureAwait(false),
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
				State = await ConvertToLegacyStateAsync(world, context.CancellationToken).ConfigureAwait(false)
			};
		}
		catch (InvalidOperationException ex)
		{
			throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
		}
	}

	public override async Task<WorldSnapshotReply> GetWorldSnapshot(WorldSnapshotRequest request, ServerCallContext context)
	{
		var cancellationToken = context.CancellationToken;
		DomainWorldState world;
		if (!string.IsNullOrWhiteSpace(request.WorldId))
		{
			if (!Guid.TryParse(request.WorldId, out var worldId))
			{
				throw new RpcException(new Status(StatusCode.InvalidArgument, "worldId must be a valid GUID."));
			}

			world = await _worldSessionManager.GetWorldAsync(worldId, cancellationToken).ConfigureAwait(false);
		}
		else if (!string.IsNullOrWhiteSpace(request.SessionId))
		{
			if (!Guid.TryParse(request.SessionId, out var sessionId))
			{
				throw new RpcException(new Status(StatusCode.InvalidArgument, "sessionId must be a valid GUID."));
			}

			world = await _worldSessionManager.GetWorldForSessionAsync(sessionId, cancellationToken).ConfigureAwait(false);
		}
		else
		{
			throw new RpcException(new Status(StatusCode.InvalidArgument, "Either worldId or sessionId must be provided."));
		}

		var snapshot = await ToSnapshotAsync(world, cancellationToken).ConfigureAwait(false);
		_logger.Info($"World snapshot resolved: {DescribeSnapshot(snapshot)}");
		return new WorldSnapshotReply
		{
			Snapshot = snapshot
		};
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
					Snapshot = await ToSnapshotAsync(world, context.CancellationToken).ConfigureAwait(false)
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
			Position = ToProtoLocation(snapshot.Location),
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
			Position = ToProtoLocation(snapshot.Location),
			IsMoving = snapshot.IsMoving,
			IsRotating = snapshot.IsRotating,
			Rotation = snapshot.Rotation
		};

		return new PlayerCharacter
		{
			BaseCharacter = baseCharacter
		};
	}

	private async Task<WorldSnapshot> ToSnapshotAsync(DomainWorldState world, CancellationToken cancellationToken)
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

		var characterTasks = world.Characters
			.Select(id => _modelRepository.GetByIdAsync<Character>(id, cancellationToken))
			.ToList();
		var npcTasks = world.Npcs
			.Select(id => _modelRepository.GetByIdAsync<Npc>(id, cancellationToken))
			.ToList();
		var mapObjectTasks = world.MapObjects
			.Select(id => _modelRepository.GetByIdAsync<MapObject>(id, cancellationToken))
			.ToList();

		var characters = await Task.WhenAll(characterTasks).ConfigureAwait(false);
		var npcs = await Task.WhenAll(npcTasks).ConfigureAwait(false);
		var mapObjects = await Task.WhenAll(mapObjectTasks).ConfigureAwait(false);

		snapshot.Characters.AddRange(characters
			.Where(character => character != null)
			.Select(character => ToProtoCharacter(character!)));

		snapshot.Npcs.AddRange(npcs
			.Where(npc => npc != null)
			.Select(npc => ToProtoNpc(npc!)));

		snapshot.MapObjects.AddRange(mapObjects
			.Where(mapObject => mapObject != null)
			.Select(mapObject => ToProtoMapObject(mapObject!)));
		return snapshot;
	}

	private async Task<ProtoWorldState> ConvertToLegacyStateAsync(DomainWorldState world, CancellationToken cancellationToken)
	{
		var legacyState = new ProtoWorldState
		{
			Timestamp = new DateTimeOffset(world.LastUpdated).ToUnixTimeMilliseconds()
		};

		var characterTasks = world.Characters
			.Select(id => _modelRepository.GetByIdAsync<Character>(id, cancellationToken))
			.ToList();
		var npcTasks = world.Npcs
			.Select(id => _modelRepository.GetByIdAsync<Npc>(id, cancellationToken))
			.ToList();

		var characters = await Task.WhenAll(characterTasks).ConfigureAwait(false);
		var npcs = await Task.WhenAll(npcTasks).ConfigureAwait(false);

		legacyState.VisibleCharacters.AddRange(characters
			.Where(character => character != null)
			.Select(character => new PlayerCharacter
			{
				BaseCharacter = new BaseCharacter
				{
					Id = character!.Id.ToString(),
					Name = character.Name,
					Position = ToProtoLocation(character.CurrentLocation)
				}
			}));

		legacyState.VisibleNPCs.AddRange(npcs
			.Where(npc => npc != null)
			.Select(npc => new PlayerCharacter
			{
				BaseCharacter = new BaseCharacter
				{
					Id = npc!.Id.ToString(),
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
			Name = string.IsNullOrWhiteSpace(npc.DisplayName) ? npc.Name : npc.DisplayName,
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

	private static Protos.Location ToProtoLocation(Location location)
	{
		return new Protos.Location
		{
			X = location.Position.X,
			Y = location.Position.Y,
			Z = location.Position.Z,
			WorldId = location.WorldId.HasValue ? location.WorldId.Value.ToString() : string.Empty,
			MapId = location.MapId ?? string.Empty,
			ZoneName = location.ZoneName ?? string.Empty,
			Rotation = location.Rotation
		};
	}

	private static string DescribeSnapshot(WorldSnapshot snapshot)
	{
		var worldName = snapshot.Metadata?.WorldName ?? "?";
		var worldId = snapshot.Metadata?.WorldId ?? "?";
		var characterCount = snapshot.Characters?.Count ?? 0;
		var npcCount = snapshot.Npcs?.Count ?? 0;
		var mapObjectCount = snapshot.MapObjects?.Count ?? 0;
		return FormattableString.Invariant($"świat={worldName} ({worldId}), gracze={characterCount}, npc={npcCount}, obiekty={mapObjectCount}");
	}
}
