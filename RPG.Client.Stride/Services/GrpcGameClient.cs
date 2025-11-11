using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using RPG.GameServer.Protos;

namespace RPG.Client.Stride.Services;

internal sealed class GrpcGameClient : IAsyncDisposable
{
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(5);

    private readonly IConfiguration _configuration;
    private readonly CancellationTokenSource _cts = new();
    private readonly SemaphoreSlim _movementLock = new(1, 1);
    private readonly SemaphoreSlim _rotationLock = new(1, 1);

    private GrpcChannel? _channel;
    private CharacterService.CharacterServiceClient? _characterClient;
    private SessionService.SessionServiceClient? _sessionClient;
    private WorldService.WorldServiceClient? _worldClient;

    private Task? _worldStreamTask;
    private Task? _heartbeatTask;
    private int? _activeMovementDirection;
    private bool _activePreserveFacing;

    public event Action<WorldSnapshot>? SnapshotReceived;
    public event Action<string>? Log;

    public CharacterSession? Session { get; private set; }
    public PlayerProfile? Player { get; private set; }
    public Location? LastKnownLocation { get; private set; }
    public Guid? WorldId { get; private set; }
    public string? WorldName { get; private set; }
    public int FacingDirection { get; private set; } = 1;
    public float LastServerRotationDegrees { get; private set; } = 0f;

    public GrpcGameClient(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task InitializeAsync()
    {
        if (_channel != null)
        {
            return;
        }

        var address = _configuration.GetValue<string>("GameServer:GrpcAddress")
                      ?? Environment.GetEnvironmentVariable("RPG_GAMESERVER_URL")
                      ?? "http://localhost:5124";

        Log?.Invoke($"Connecting to RPG game server at {address}...");

        _channel = GrpcChannel.ForAddress(address);
        _characterClient = new CharacterService.CharacterServiceClient(_channel);
        _sessionClient = new SessionService.SessionServiceClient(_channel);
        _worldClient = new WorldService.WorldServiceClient(_channel);

        Player = CreatePlayerProfile();

        Session = await InitializeGameSessionAsync(_characterClient, _sessionClient, Player).ConfigureAwait(false);

        var joinReply = await _worldClient.JoinWorldAsync(new JoinWorldRequest
        {
            SessionId = Session.SessionId.ToString()
        }, cancellationToken: _cts.Token).ConfigureAwait(false);

        if (joinReply.SpawnLocation != null)
        {
            LastKnownLocation = joinReply.SpawnLocation;
            LastServerRotationDegrees = joinReply.SpawnLocation.Rotation;
            FacingDirection = DirectionFromRotation(joinReply.SpawnLocation.Rotation);
        }

        if (joinReply.Snapshot?.Metadata != null)
        {
            if (Guid.TryParse(joinReply.Snapshot.Metadata.WorldId, out var worldId))
            {
                WorldId = worldId;
            }

            WorldName = string.IsNullOrWhiteSpace(joinReply.Snapshot.Metadata.WorldName)
                ? null
                : joinReply.Snapshot.Metadata.WorldName;
        }

        if (joinReply.Snapshot != null)
        {
            UpdateTrackedState(joinReply.Snapshot);
            SnapshotReceived?.Invoke(joinReply.Snapshot);
        }

        if (WorldId.HasValue)
        {
            _worldStreamTask = StreamWorldStateAsync(WorldId.Value, _cts.Token);
        }

        _heartbeatTask = RunHeartbeatLoopAsync(_cts.Token);

        Log?.Invoke($"Joined world {WorldName ?? WorldId?.ToString() ?? "(unknown)"} as {Player.PlayerId}.");
    }

    public async Task<bool> StartMovementAsync(int direction, bool preserveFacing)
    {
        if (_characterClient == null || Session == null)
        {
            return false;
        }

        if (direction < 1 || direction > 8)
        {
            return false;
        }

        await _movementLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var requiresStop = _activeMovementDirection.HasValue &&
                               (_activeMovementDirection != direction || _activePreserveFacing != preserveFacing);

            if (requiresStop)
            {
                await StopMovementInternalAsync().ConfigureAwait(false);
            }

            var reply = await _characterClient.StartMovementAsync(new MovementCommandRequest
            {
                CharacterId = Session.CharacterId.ToString(),
                Direction = direction,
                PreserveFacing = preserveFacing
            }).ConfigureAwait(false);

            if (!reply.Success)
            {
                Log?.Invoke($"StartMovement failed: {reply.ErrorCode} {reply.Message}");
                return false;
            }

            _activeMovementDirection = direction;
            _activePreserveFacing = preserveFacing;
            return true;
        }
        catch (Exception ex)
        {
            Log?.Invoke($"StartMovement exception: {ex.Message}");
            return false;
        }
        finally
        {
            _movementLock.Release();
        }
    }

    public async Task<bool> StopMovementAsync()
    {
        if (_characterClient == null || Session == null)
        {
            return false;
        }

        await _movementLock.WaitAsync().ConfigureAwait(false);
        try
        {
            return await StopMovementInternalAsync().ConfigureAwait(false);
        }
        finally
        {
            _movementLock.Release();
        }
    }

    public async Task<bool> RotateAsync(int step)
    {
        if (_characterClient == null || Session == null || step == 0)
        {
            return false;
        }

        await _rotationLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var targetDirection = OffsetDirection(FacingDirection, step);
            var started = false;

            try
            {
                var reply = await _characterClient.StartRotationAsync(new MovementCommandRequest
                {
                    CharacterId = Session.CharacterId.ToString(),
                    Direction = targetDirection
                }).ConfigureAwait(false);

                if (!reply.Success)
                {
                    Log?.Invoke($"StartRotation failed: {reply.ErrorCode} {reply.Message}");
                    return false;
                }

                started = true;
                FacingDirection = targetDirection;
                LastServerRotationDegrees = DegreesFromDirection(FacingDirection);
                return true;
            }
            catch (Exception ex)
            {
                Log?.Invoke($"StartRotation exception: {ex.Message}");
                return false;
            }
            finally
            {
                if (started)
                {
                    try
                    {
                        await _characterClient.StopRotationAsync(new CharacterIdRequest
                        {
                            CharacterId = Session.CharacterId.ToString()
                        }).ConfigureAwait(false);
                    }
                    catch (Exception stopEx)
                    {
                        Log?.Invoke($"StopRotation exception: {stopEx.Message}");
                    }
                }
            }
        }
        finally
        {
            _rotationLock.Release();
        }
    }

    private async Task<bool> StopMovementInternalAsync()
    {
        if (_activeMovementDirection == null)
        {
            return true;
        }

        try
        {
            var reply = await _characterClient!.StopMovementAsync(new CharacterIdRequest
            {
                CharacterId = Session!.CharacterId.ToString()
            }).ConfigureAwait(false);

            if (!reply.Success)
            {
                Log?.Invoke($"StopMovement failed: {reply.ErrorCode} {reply.Message}");
                return false;
            }

            _activeMovementDirection = null;
            _activePreserveFacing = false;
            return true;
        }
        catch (Exception ex)
        {
            Log?.Invoke($"StopMovement exception: {ex.Message}");
            return false;
        }
    }

    private async Task StreamWorldStateAsync(Guid worldId, CancellationToken cancellationToken)
    {
        if (_worldClient == null || Session == null)
        {
            return;
        }

        using var call = _worldClient.StreamWorldState(new WorldStreamRequest
        {
            SessionId = Session.SessionId.ToString(),
            WorldId = worldId.ToString(),
            IntervalMilliseconds = 250
        }, cancellationToken: cancellationToken);

        try
        {
            while (await call.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false))
            {
                var update = call.ResponseStream.Current;
                if (update?.Snapshot == null)
                {
                    continue;
                }

                UpdateTrackedState(update.Snapshot);
                SnapshotReceived?.Invoke(update.Snapshot);
            }
        }
        catch (RpcException rpcEx) when (rpcEx.StatusCode == StatusCode.Cancelled && cancellationToken.IsCancellationRequested)
        {
            // graceful shutdown
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // graceful shutdown
        }
        catch (Exception ex)
        {
            Log?.Invoke($"World stream ended: {ex.Message}");
        }
    }

    private void UpdateTrackedState(WorldSnapshot snapshot)
    {
        if (snapshot.Metadata != null)
        {
            if (Guid.TryParse(snapshot.Metadata.WorldId, out var parsedWorldId))
            {
                WorldId = parsedWorldId;
            }

            if (!string.IsNullOrWhiteSpace(snapshot.Metadata.WorldName))
            {
                WorldName = snapshot.Metadata.WorldName;
            }
        }

        if (Session == null)
        {
            return;
        }

        var player = snapshot.Characters.FirstOrDefault(c =>
            Guid.TryParse(c.SessionId, out var sessionId) && sessionId == Session.SessionId);

        if (player?.Location != null)
        {
            LastKnownLocation = player.Location;
            LastServerRotationDegrees = player.Location.Rotation;
            FacingDirection = DirectionFromRotation(player.Location.Rotation);
        }
    }

    private async Task RunHeartbeatLoopAsync(CancellationToken cancellationToken)
    {
        if (_sessionClient == null || Session == null)
        {
            return;
        }

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(HeartbeatInterval, cancellationToken).ConfigureAwait(false);

                var request = new SessionHeartbeatRequest
                {
                    SessionId = Session.SessionId.ToString()
                };

                if (LastKnownLocation != null)
                {
                    request.Location = new Location
                    {
                        X = LastKnownLocation.X,
                        Y = LastKnownLocation.Y,
                        Z = LastKnownLocation.Z,
                        WorldId = LastKnownLocation.WorldId,
                        MapId = LastKnownLocation.MapId,
                        ZoneName = LastKnownLocation.ZoneName,
                        Rotation = LastKnownLocation.Rotation
                    };
                }

                try
                {
                    await _sessionClient.HeartbeatSessionAsync(request, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Log?.Invoke($"Heartbeat failed: {ex.Message}");
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // ignore cancellation
        }
    }

    private async Task<CharacterSession> InitializeGameSessionAsync(
        CharacterService.CharacterServiceClient characterClient,
        SessionService.SessionServiceClient sessionClient,
        PlayerProfile player)
    {
        var characterId = Guid.NewGuid();
        var sessionReply = await sessionClient.CreateSessionAsync(new CreateSessionRequest
        {
            CharacterId = characterId.ToString(),
            PlayerId = player.PlayerId.ToString()
        }, cancellationToken: _cts.Token).ConfigureAwait(false);

        var sessionId = Guid.Parse(sessionReply.Session.Id);

        try
        {
            var createdCharacterId = await CreateCharacterAsync(characterClient, characterId, sessionId, player).ConfigureAwait(false);
            return new CharacterSession(createdCharacterId, sessionId, player.PlayerId, player.DisplayName);
        }
        catch
        {
            await sessionClient.EndSessionAsync(new EndSessionRequest
            {
                SessionId = sessionId.ToString()
            }, cancellationToken: CancellationToken.None).ConfigureAwait(false);

            throw;
        }
    }

    private async Task<Guid> CreateCharacterAsync(
        CharacterService.CharacterServiceClient client,
        Guid characterId,
        Guid sessionId,
        PlayerProfile player)
    {
        var request = new CharacterRequest
        {
            Character = new PlayerCharacter
            {
                SessionId = sessionId.ToString(),
                CharacterClass = CharacterClass.Warrior,
                BaseCharacter = new BaseCharacter
                {
                    Id = characterId.ToString(),
                    Name = $"{player.DisplayName}-Hero",
                    Level = 1,
                    MaxHealth = 100,
                    CurrentHealth = 100,
                    MaxMana = 50,
                    CurrentMana = 50,
                    Stats = new Stats
                    {
                        MoveSpeed = 5
                    }
                }
            }
        };

        var reply = await client.CreateCharacterAsync(request, cancellationToken: _cts.Token).ConfigureAwait(false);
        return Guid.Parse(reply.CharacterId);
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();

        if (_worldStreamTask != null)
        {
            try
            {
                await _worldStreamTask.ConfigureAwait(false);
            }
            catch
            {
                // ignore
            }
        }

        if (_heartbeatTask != null)
        {
            try
            {
                await _heartbeatTask.ConfigureAwait(false);
            }
            catch
            {
                // ignore
            }
        }

        await SafeLeaveWorldAsync().ConfigureAwait(false);
        await SafeEndSessionAsync().ConfigureAwait(false);

        _movementLock.Dispose();
        _rotationLock.Dispose();
        _cts.Dispose();

        _channel?.Dispose();
        _channel = null;
    }

    private async Task SafeLeaveWorldAsync()
    {
        if (_worldClient == null || Session == null)
        {
            return;
        }

        try
        {
            await _worldClient.LeaveWorldAsync(new WorldMembershipRequest
            {
                SessionId = Session.SessionId.ToString()
            }, cancellationToken: CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // ignore cleanup errors
        }
    }

    private async Task SafeEndSessionAsync()
    {
        if (_sessionClient == null || Session == null)
        {
            return;
        }

        try
        {
            await _sessionClient.EndSessionAsync(new EndSessionRequest
            {
                SessionId = Session.SessionId.ToString()
            }, cancellationToken: CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // ignore
        }
    }

    private static PlayerProfile CreatePlayerProfile()
    {
        var playerId = Guid.NewGuid();
        var displayName = $"MonoGamePlayer-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        return new PlayerProfile(playerId, displayName);
    }

    private static int DirectionFromRotation(float rotation)
    {
        if (float.IsNaN(rotation) || float.IsInfinity(rotation))
        {
            return 1;
        }

        var normalized = rotation % 360f;
        if (normalized < 0f)
        {
            normalized += 360f;
        }

        var adjusted = (normalized + 22.5f) % 360f;
        var index = (int)MathF.Floor(adjusted / 45f);
        return index + 1;
    }

    private static float DegreesFromDirection(int direction)
    {
        var index = NormalizeDirection(direction) - 1;
        return index * 45f;
    }

    private static int NormalizeDirection(int direction)
    {
        var normalized = (direction - 1) % 8;
        if (normalized < 0)
        {
            normalized += 8;
        }

        return normalized + 1;
    }

    private static int OffsetDirection(int baseDirection, int offset)
    {
        return NormalizeDirection(baseDirection + offset);
    }
}

internal sealed record CharacterSession(Guid CharacterId, Guid SessionId, Guid PlayerId, string PlayerName);

internal sealed record PlayerProfile(Guid PlayerId, string DisplayName);
