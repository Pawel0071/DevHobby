using Grpc.Core;
using RPG.Abstractions;
using RPG.Abstractions.Interfaces;
using RPG.Core.Interfaces.NpcServices;
using RPG.Application.Events;
using RPG.Domain.Models;
using RPG.GameServer.Protos;

namespace RPG.GameServer.Controllers;

public class WorldServiceImpl : WorldService.WorldServiceBase
{
	private readonly ICharacterStateBroadcaster _stateBroadcaster;
	private readonly INpcAiService _npcAiService;

	public WorldServiceImpl(ICharacterStateBroadcaster stateBroadcaster, INpcAiService npcAiService)
	{
		_stateBroadcaster = stateBroadcaster;
		_npcAiService = npcAiService;
	}

	public override Task<WorldReply> GetWorldState(WorldRequest request, ServerCallContext context)
	{
		var snapshots = _stateBroadcaster.GetSnapshots();
		var npcSnapshots = _npcAiService.GetNpcSnapshots();

		var worldState = new WorldState
		{
			Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
		};

		worldState.VisibleCharacters.AddRange(snapshots.Select(ToPlayerCharacter));
		worldState.VisibleNPCs.AddRange(npcSnapshots.Select(ToNpcCharacter));

		return Task.FromResult(new WorldReply { State = worldState });
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
}
