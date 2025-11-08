using System;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using RPG.GameServer.Interfaces;
using RPG.GameServer.Models;
using RPG.GameServer.Protos;

namespace RPG.GameServer.Controllers;

public class WorldServiceImpl : WorldService.WorldServiceBase
{
	private readonly ICharacterStateBroadcaster _stateBroadcaster;

	public WorldServiceImpl(ICharacterStateBroadcaster stateBroadcaster)
	{
		_stateBroadcaster = stateBroadcaster;
	}

	public override Task<WorldReply> GetWorldState(WorldRequest request, ServerCallContext context)
	{
		var snapshots = _stateBroadcaster.GetSnapshots();

		var worldState = new WorldState
		{
			Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
		};

		worldState.VisibleCharacters.AddRange(snapshots.Select(ToPlayerCharacter));

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
}
