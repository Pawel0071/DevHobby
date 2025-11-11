using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using RPG.GameServer.Protos;

namespace RPG.DesktopClient.Avalonia.Services;

internal sealed class GameClientService : IAsyncDisposable
{
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(5);

    private readonly IConfiguration _configuration;
    private GrpcChannel? _channel;
    private CharacterService.CharacterServiceClient? _characterClient;
    private SessionService.SessionServiceClient? _sessionClient;
    private WorldService.WorldServiceClient? _worldClient;
    private readonly CancellationTokenSource _cts = new();
    private readonly SemaphoreSlim _movementLock = new(1, 1);
    private readonly SemaphoreSlim _rotationLock = new(1, 1);
    private Task? _worldStreamTask;
    private Task? _heartbeatTask;
    private int? _activeMovementDirection;
    private const int RotationLeftCommand = 7;
    private const int RotationRightCommand = 3;
    private readonly SeedWorldStateLoader _seedWorldStateLoader;
    private bool _offlineSnapshotLoaded;
    private bool _streamLoggedFirstSnapshot;

    public event Action<WorldSnapshot>? SnapshotReceived;
    public event Action<string>? MessageReceived;

    public CharacterSession? Session { get; private set; }
    public PlayerProfile? Player { get; private set; }
    public Location? LastWorldLocation { get; private set; }
    public Guid? WorldId { get; private set; }
    public string? WorldName { get; private set; }

    public GameClientService(IConfiguration configuration)
    {
        _configuration = configuration;
        _seedWorldStateLoader = new SeedWorldStateLoader(configuration);
    }

    public async Task InitializeAsync()
    {
        if (_channel != null || _offlineSnapshotLoaded)
        {
            return;
        }

        try
        {
            await InitializeOnlineAsync().ConfigureAwait(false);
        }
        catch (Exception ex) when (!_cts.IsCancellationRequested)
        {
            await SafeEndSessionAsync().ConfigureAwait(false);
            ResetGrpcClients();

            var snapshot = await _seedWorldStateLoader.TryLoadAsync(_cts.Token).ConfigureAwait(false);
            if (snapshot != null)
            {
                _offlineSnapshotLoaded = true;

                if (Guid.TryParse(snapshot.Metadata?.WorldId, out var worldId))
                {
                    WorldId = worldId;
                }

                WorldName = snapshot.Metadata?.WorldName;
                EmitSnapshotDiagnostics("Offline seed snapshot", snapshot);
                SnapshotReceived?.Invoke(snapshot);
                Report("Uruchomiono tryb offline z danymi seed (RPG.WorldSeeder).");
                Report($"Połączenie z serwerem gry nie powiodło się: {ex.Message}");
                return;
            }

            Report("Brak możliwości załadowania danych seed – upewnij się, że katalog SeedData jest dostępny.");

            throw;
        }
    }

    private async Task InitializeOnlineAsync()
    {
    var serverAddress = _configuration.GetValue<string>("GameServer:GrpcAddress")
                 ?? Environment.GetEnvironmentVariable("RPG_GAMESERVER_URL")
                 ?? "http://localhost:5124";

    Report($"Łączenie z serwerem gry pod adresem: {serverAddress}");

    _channel = GrpcChannel.ForAddress(serverAddress);
        _characterClient = new CharacterService.CharacterServiceClient(_channel);
        _sessionClient = new SessionService.SessionServiceClient(_channel);
        _worldClient = new WorldService.WorldServiceClient(_channel);

        Player = CreatePlayerProfile();

        var session = await InitializeGameSessionAsync(_characterClient, _sessionClient, Player).ConfigureAwait(false);
        Session = session;

        JoinWorldReply joinReply;
        try
        {
            joinReply = await _worldClient.JoinWorldAsync(new JoinWorldRequest
            {
                SessionId = session.SessionId.ToString()
            }, cancellationToken: _cts.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await SafeEndSessionAsync().ConfigureAwait(false);
            throw new InvalidOperationException($"Nie udało się dołączyć do świata: {ex.Message}", ex);
        }

        if (joinReply.Snapshot?.Metadata != null)
        {
            if (Guid.TryParse(joinReply.Snapshot.Metadata.WorldId, out var parsedWorldId))
            {
                WorldId = parsedWorldId;
            }

            if (!string.IsNullOrWhiteSpace(joinReply.Snapshot.Metadata.WorldName))
            {
                WorldName = joinReply.Snapshot.Metadata.WorldName;
            }
        }

        if (joinReply.SpawnLocation != null)
        {
            LastWorldLocation = joinReply.SpawnLocation;
        }

        if (joinReply.Snapshot != null)
        {
            EmitSnapshotDiagnostics("JoinWorld snapshot", joinReply.Snapshot);
            SnapshotReceived?.Invoke(joinReply.Snapshot);
            Report(BuildSnapshotSummary("Migawka z JoinWorld", joinReply.Snapshot));

            var npcCount = joinReply.Snapshot.Npcs?.Count ?? 0;
            var mapObjectCount = joinReply.Snapshot.MapObjects?.Count ?? 0;
            if (npcCount == 0 || mapObjectCount == 0)
            {
                await RequestSupplementalWorldSnapshotAsync(joinReply.Snapshot).ConfigureAwait(false);
            }
        }

        Report($"Dołączono do świata {WorldName ?? WorldId?.ToString() ?? "?"}.");

        _worldStreamTask = StreamWorldStateAsync(WorldId, _cts.Token);
        _heartbeatTask = RunHeartbeatLoopAsync(_cts.Token);
        _offlineSnapshotLoaded = false;
    }

    private void ResetGrpcClients()
    {
        _worldStreamTask = null;
        _heartbeatTask = null;
        _characterClient = null;
        _sessionClient = null;
        _worldClient = null;

        _channel?.Dispose();
        _channel = null;

        Session = null;
        Player = null;
        WorldId = null;
        WorldName = null;
        LastWorldLocation = null;
        _streamLoggedFirstSnapshot = false;
    }

    public async Task<bool> StartMovementAsync(int direction)
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
            if (_activeMovementDirection == direction)
            {
                return true;
            }

            if (_activeMovementDirection.HasValue)
            {
                await StopMovementInternalAsync().ConfigureAwait(false);
            }

            try
            {
                var reply = await _characterClient.StartMovementAsync(new MovementCommandRequest
                {
                    CharacterId = Session.CharacterId.ToString(),
                    Direction = direction
                }).ConfigureAwait(false);

                if (!reply.Success)
                {
                    Report($"StartMovement nie powiodło się ({reply.ErrorCode}): {reply.Message}");
                    return false;
                }

                _activeMovementDirection = direction;
                return true;
            }
            catch (Exception ex)
            {
                Report($"Błąd StartMovement: {ex.Message}");
                return false;
            }
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

        var rotationCommand = step < 0 ? RotationLeftCommand : RotationRightCommand;
        await _rotationLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var rotationStarted = false;
            var success = false;

            try
            {
                var reply = await _characterClient.StartRotationAsync(new MovementCommandRequest
                {
                    CharacterId = Session.CharacterId.ToString(),
                    Direction = rotationCommand
                }).ConfigureAwait(false);

                if (!reply.Success)
                {
                    Report($"StartRotation nie powiodło się ({reply.ErrorCode}): {reply.Message}");
                }
                else
                {
                    rotationStarted = true;
                    success = true;
                }
            }
            catch (Exception ex)
            {
                Report($"Błąd StartRotation: {ex.Message}");
            }
            finally
            {
                if (rotationStarted)
                {
                    try
                    {
                        var stopReply = await _characterClient.StopRotationAsync(new CharacterIdRequest
                        {
                            CharacterId = Session.CharacterId.ToString()
                        }).ConfigureAwait(false);

                        if (!stopReply.Success)
                        {
                            Report($"StopRotation nie powiodło się ({stopReply.ErrorCode}): {stopReply.Message}");
                        }
                    }
                    catch (Exception stopEx)
                    {
                        Report($"Błąd StopRotation: {stopEx.Message}");
                    }
                }
            }

            return success;
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
                Report($"StopMovement nie powiodło się ({reply.ErrorCode}): {reply.Message}");
                return false;
            }

            _activeMovementDirection = null;
            return true;
        }
        catch (Exception ex)
        {
            Report($"Błąd StopMovement: {ex.Message}");
            return false;
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
        }, cancellationToken: _cts.Token);

        var sessionId = Guid.Parse(sessionReply.Session.Id);

        try
        {
            var createdCharacterId = await CreateCharacterAsync(characterClient, characterId, sessionId, player);
            return new CharacterSession(createdCharacterId, sessionId, player.PlayerId, player.DisplayName);
        }
        catch
        {
            await sessionClient.EndSessionAsync(new EndSessionRequest
            {
                SessionId = sessionId.ToString()
            }, cancellationToken: _cts.Token);

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
                        MoveSpeed = 5,
                        Strength = 10,
                        Vitality = 10
                    },
                    Position = new Location
                    {
                        X = 0,
                        Y = 0,
                        Z = 0
                    },
                    IsMoving = false,
                    IsRotating = false,
                    Rotation = 0
                }
            }
        };

        var response = await client.CreateCharacterAsync(request, cancellationToken: _cts.Token);
        return Guid.Parse(response.CharacterId);
    }

    private void Report(string message)
    {
        MessageReceived?.Invoke(FormattableString.Invariant($"[Client] {message}"));
    }

    private static string BuildSnapshotSummary(string prefix, WorldSnapshot snapshot)
    {
        var worldName = snapshot.Metadata?.WorldName ?? "?";
        var worldId = snapshot.Metadata?.WorldId ?? "?";
        var characterCount = snapshot.Characters?.Count(c => c != null && c.IsOnline) ?? 0;
        var npcCount = snapshot.Npcs?.Count ?? 0;
        var mapObjectCount = snapshot.MapObjects?.Count ?? 0;
        return FormattableString.Invariant($"{prefix}: świat={worldName} ({worldId}), gracze={characterCount}, npc={npcCount}, obiekty={mapObjectCount}");
    }

    private void EmitSnapshotDiagnostics(string source, WorldSnapshot snapshot)
    {
        var npcCount = snapshot.Npcs?.Count ?? 0;
        var sampleNpc = snapshot.Npcs?.FirstOrDefault();
        var sampleNpcName = sampleNpc == null
            ? "-"
            : string.IsNullOrWhiteSpace(sampleNpc.Name)
                ? sampleNpc.NpcId
                : sampleNpc.Name;

        var mapObjectCount = snapshot.MapObjects?.Count ?? 0;
        var sampleObject = snapshot.MapObjects?.FirstOrDefault();
        var sampleObjectName = sampleObject == null
            ? "-"
            : !string.IsNullOrWhiteSpace(sampleObject.DisplayName)
                ? sampleObject.DisplayName
                : string.IsNullOrWhiteSpace(sampleObject.Name)
                    ? sampleObject.MapObjectId
                    : sampleObject.Name;

        Report(FormattableString.Invariant($"{source}: npc={npcCount} (przykład: {sampleNpcName}), obiekty={mapObjectCount} (przykład: {sampleObjectName})"));
    }

    private async Task StreamWorldStateAsync(Guid? worldId, CancellationToken cancellationToken)
    {
        if (worldId == null || _worldClient == null || Session == null)
        {
            return;
        }

        try
        {
            using var call = _worldClient.StreamWorldState(new WorldStreamRequest
            {
                SessionId = Session.SessionId.ToString(),
                WorldId = worldId.Value.ToString(),
                IntervalMilliseconds = 500
            }, cancellationToken: cancellationToken);

            while (await call.ResponseStream.MoveNext(cancellationToken))
            {
                var snapshot = call.ResponseStream.Current.Snapshot;
                if (snapshot == null)
                {
                    continue;
                }

                UpdatePlayerLocation(snapshot);
                EmitSnapshotDiagnostics("Stream snapshot", snapshot);
                SnapshotReceived?.Invoke(snapshot);

                if (!_streamLoggedFirstSnapshot)
                {
                    Report(BuildSnapshotSummary("Migawka strumienia", snapshot));
                    _streamLoggedFirstSnapshot = true;
                }
            }
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled && cancellationToken.IsCancellationRequested)
        {
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // stream cancelled
        }
        catch (Exception ex)
        {
            Report($"Błąd strumienia świata: {ex.Message}");
        }
    }

    private async Task RequestSupplementalWorldSnapshotAsync(WorldSnapshot? previousSnapshot)
    {
        if (_worldClient == null)
        {
            return;
        }

        var request = new WorldSnapshotRequest();
        if (WorldId.HasValue)
        {
            request.WorldId = WorldId.Value.ToString();
        }

        if (Session != null)
        {
            request.SessionId = Session.SessionId.ToString();
        }

        try
        {
            var reply = await _worldClient.GetWorldSnapshotAsync(request, cancellationToken: _cts.Token).ConfigureAwait(false);
            var snapshot = reply.Snapshot;
            if (snapshot == null)
            {
                return;
            }

            EmitSnapshotDiagnostics("GetWorldSnapshot reply", snapshot);
            Report(BuildSnapshotSummary("Migawka GetWorldSnapshot", snapshot));

            var npcCount = snapshot.Npcs?.Count ?? 0;
            var mapObjectCount = snapshot.MapObjects?.Count ?? 0;
            var previousNpcCount = previousSnapshot?.Npcs?.Count ?? 0;
            var previousMapObjectCount = previousSnapshot?.MapObjects?.Count ?? 0;

            if (npcCount > previousNpcCount || mapObjectCount > previousMapObjectCount)
            {
                SnapshotReceived?.Invoke(snapshot);
            }
        }
        catch (Exception ex)
        {
            Report($"GetWorldSnapshot nie powiodło się: {ex.Message}");
        }
    }

    private void UpdatePlayerLocation(WorldSnapshot snapshot)
    {
        if (Session == null)
        {
            return;
        }

        if (snapshot.Characters == null)
        {
            return;
        }

        foreach (var character in snapshot.Characters)
        {
            if (character == null || !Guid.TryParse(character.CharacterId, out var characterId))
            {
                continue;
            }

            if (characterId == Session.CharacterId && character.Location != null)
            {
                LastWorldLocation = new Location
                {
                    X = character.Location.X,
                    Y = character.Location.Y,
                    Z = character.Location.Z,
                    WorldId = character.Location.WorldId,
                    MapId = character.Location.MapId,
                    ZoneName = character.Location.ZoneName,
                    Rotation = character.Location.Rotation
                };
                break;
            }
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
                await Task.Delay(HeartbeatInterval, cancellationToken);

                var location = LastWorldLocation;

                var request = new SessionHeartbeatRequest
                {
                    SessionId = Session.SessionId.ToString()
                };

                if (location != null)
                {
                    request.Location = new Location
                    {
                        X = location.X,
                        Y = location.Y,
                        Z = location.Z,
                        WorldId = location.WorldId,
                        MapId = location.MapId,
                        ZoneName = location.ZoneName,
                        Rotation = location.Rotation
                    };
                }

                try
                {
                    await _sessionClient.HeartbeatSessionAsync(request, cancellationToken: cancellationToken);
                }
                catch (Exception ex)
                {
                    Report($"Heartbeat failed: {ex.Message}");
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // ignore cancellation
        }
    }

    private static PlayerProfile CreatePlayerProfile()
    {
        var playerId = Guid.NewGuid();
        var displayName = $"AvaloniaPlayer-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        return new PlayerProfile(playerId, displayName);
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
            }, cancellationToken: CancellationToken.None);
        }
        catch
        {
            // ignore errors on shutdown
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
            }, cancellationToken: CancellationToken.None);
        }
        catch
        {
            // ignore
        }
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
                // ignored
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
                // ignored
            }
        }

        await SafeLeaveWorldAsync();
        await SafeEndSessionAsync();

        _cts.Dispose();
        _movementLock.Dispose();
        _rotationLock.Dispose();

        _channel?.Dispose();
        _channel = null;
        _activeMovementDirection = null;
    }
}

internal sealed record CharacterSession(Guid CharacterId, Guid SessionId, Guid PlayerId, string PlayerName)
{
    public Guid? WorldId { get; init; }
    public string? WorldName { get; init; }
}

internal sealed record PlayerProfile(Guid PlayerId, string DisplayName);
