using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using RPG.GameServer.Protos;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace RPG.DesktopClient;

internal static class Program
{
    private const int BoardWidth = 45;
    private const int BoardHeight = 21;
    private const float StepPerSecond = 5f;

    private static readonly Dictionary<int, (int dx, int dy, string arrow)> DirectionVectors = new()
    {
        { 1, (0, -1, "↑") },
        { 2, (1, -1, "↗") },
        { 3, (1, 0, "→") },
        { 4, (1, 1, "↘") },
        { 5, (0, 1, "↓") },
        { 6, (-1, 1, "↙") },
        { 7, (-1, 0, "←") },
        { 8, (-1, -1, "↖") },
    };
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(5);

    public static async Task Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.TreatControlCAsInput = true;

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        var serverAddress = configuration.GetValue<string>("GameServer:GrpcAddress")
                             ?? Environment.GetEnvironmentVariable("RPG_GAMESERVER_URL")
                             ?? "http://localhost:5124";

        using var channel = GrpcChannel.ForAddress(serverAddress);
        var characterClient = new CharacterService.CharacterServiceClient(channel);
        var sessionClient = new SessionService.SessionServiceClient(channel);
        var worldClient = new WorldService.WorldServiceClient(channel);
        var player = CreatePlayerProfile();
        var automationMode = string.Equals(Environment.GetEnvironmentVariable("RPG_AUTOMATION_MODE"), "1", StringComparison.OrdinalIgnoreCase);
        TimeSpan? automationDuration = null;

        var automationDurationEnv = Environment.GetEnvironmentVariable("RPG_AUTOMATION_DURATION");
        if (int.TryParse(automationDurationEnv, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds) && seconds > 0)
        {
            automationDuration = TimeSpan.FromSeconds(seconds);
        }

        try
        {
            Console.CursorVisible = false;

            var session = await InitializeGameSessionAsync(characterClient, sessionClient, player);

            JoinWorldReply joinReply;
            try
            {
                joinReply = await worldClient.JoinWorldAsync(new JoinWorldRequest
                {
                    SessionId = session.SessionId.ToString()
                });
            }
            catch
            {
                await sessionClient.EndSessionAsync(new EndSessionRequest
                {
                    SessionId = session.SessionId.ToString()
                });

                throw;
            }

            var worldMetadata = joinReply.Snapshot?.Metadata;
            if (worldMetadata != null && Guid.TryParse(worldMetadata.WorldId, out var parsedWorldId))
            {
                session = session with { WorldId = parsedWorldId };
            }

            if (joinReply.SpawnLocation != null)
            {
                session = session with { SpawnLocation = joinReply.SpawnLocation };
            }

            if (worldMetadata != null)
            {
                session = session with { WorldName = worldMetadata.WorldName };
            }

            var controller = new CharacterConsoleController(characterClient, sessionClient, worldClient, session, joinReply, automationMode, automationDuration);

            await controller.RunAsync();
        }
        catch (RpcException rpcEx)
        {
            AnsiConsole.MarkupLine($"[red]gRPC error: {rpcEx.Status}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]{ex.Message}[/]");
        }
        finally
        {
            Console.CursorVisible = true;
        }
    }

    private static PlayerProfile CreatePlayerProfile()
    {
        var playerId = Guid.NewGuid();
        var displayName = $"ConsolePlayer-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

        // TODO: Replace stubbed player profile with authenticated login and player identification.

        return new PlayerProfile(playerId, displayName);
    }

    private static async Task<CharacterSession> InitializeGameSessionAsync(
        CharacterService.CharacterServiceClient characterClient,
        SessionService.SessionServiceClient sessionClient,
        PlayerProfile player)
    {
        var characterId = Guid.NewGuid();

        var sessionReply = await sessionClient.CreateSessionAsync(new CreateSessionRequest
        {
            CharacterId = characterId.ToString(),
            PlayerId = player.PlayerId.ToString()
        });

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
            });

            throw;
        }
    }

    private static async Task<Guid> CreateCharacterAsync(
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

        var response = await client.CreateCharacterAsync(request);
        return Guid.Parse(response.CharacterId);
    }

    private sealed class CharacterConsoleController
    {
        private const string OtherPlayerColor = "springgreen1";

        private readonly CharacterService.CharacterServiceClient _client;
        private readonly SessionService.SessionServiceClient _sessionClient;
        private readonly WorldService.WorldServiceClient _worldClient;
        private readonly CharacterSession _session;
        private readonly object _drawLock = new();
    private readonly Dictionary<Guid, (int x, int y, string displayName)> _otherCharacters = new();
    private readonly Dictionary<Guid, NpcBoardEntity> _npcs = new();
    private readonly Dictionary<Guid, MapObjectBoardEntity> _mapObjects = new();
    private readonly Dictionary<(int x, int y), TileDetails> _tileDetails = new();
	private readonly List<(string Label, int X, int Y)> _spawnPoints = new();
    private Location? _lastWorldLocation;
	private WorldNpcSummary _npcSummary = WorldNpcSummary.Empty;
	private WorldMapSummary _mapSummary = WorldMapSummary.Empty;

        private (int x, int y) _position = (BoardWidth / 2, BoardHeight / 2);
        private int _facingDirection = 1;
        private bool _movementActive;
        private int _movementDirection = 1;
        private double _movementRemainder;
        private DateTime _lastUpdate = DateTime.UtcNow;
        private bool _isRunning = true;
        private readonly Queue<string> _messages = new();
        private const int MaxMessages = 5;
        private static readonly TimeSpan InputReleaseDelay = TimeSpan.FromMilliseconds(200);
        private DateTime _lastMovementInput = DateTime.MinValue;
        private DateTime _lastHeartbeat = DateTime.MinValue;
        private Guid? _worldId;
        private string? _worldName;
        private CancellationTokenSource? _worldStreamCts;
        private Task? _worldStreamTask;
        private int _worldPopulation;
        private readonly bool _automationMode;
        private readonly TimeSpan _automationDuration;
        private readonly TaskCompletionSource<bool>? _firstSnapshotTcs;
    private long _lastSnapshotTick;
    private double _worldMinX = double.MaxValue;
    private double _worldMaxX = double.MinValue;
    private double _worldMinY = double.MaxValue;
    private double _worldMaxY = double.MinValue;
    private bool _hasWorldBounds;
        private const int RotationLeftCommand = 7;
        private const int RotationRightCommand = 3;
        private const string ArrowColor = "orange1";

        public CharacterConsoleController(
            CharacterService.CharacterServiceClient client,
            SessionService.SessionServiceClient sessionClient,
            WorldService.WorldServiceClient worldClient,
            CharacterSession session,
            JoinWorldReply joinReply,
            bool automationMode,
            TimeSpan? automationDuration)
        {
            _client = client;
            _sessionClient = sessionClient;
            _worldClient = worldClient;
            _session = session;
            _worldId = session.WorldId;
            _worldName = session.WorldName;
            _automationMode = automationMode;
            _automationDuration = automationDuration ?? TimeSpan.FromSeconds(6);
            _firstSnapshotTcs = _automationMode ? new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously) : null;

            if (session.SpawnLocation != null)
            {
                UpdateWorldBounds(session.SpawnLocation);
                _position = MapWorldToBoard(session.SpawnLocation);
            }

            ApplyWorldSnapshot(joinReply.Snapshot);

            if (_worldId.HasValue)
            {
                StartWorldStream(_worldId.Value);
            }

            if (_worldId.HasValue)
            {
                var worldLabel = _worldName ?? _worldId.Value.ToString();
                LogInfo($"Dołączono do świata {worldLabel}.");
            }

            LogInfo($"Gracz {_session.PlayerName} rozpoczął sesję {_session.SessionId}.");
        }

        public async Task RunAsync()
        {
            if (_automationMode)
            {
                await RunAutomationAsync();
                return;
            }

            while (_isRunning)
            {
                var keys = PollKeys();
                await ProcessKeysAsync(keys);
                UpdatePosition();

                await MaybeSendHeartbeatAsync(DateTime.UtcNow);

                lock (_drawLock)
                {
                    AnsiConsole.Clear();
                    AnsiConsole.Write(Render());
                }

                await Task.Delay(50);
            }
        }

        private static IReadOnlyList<ConsoleKeyInfo> PollKeys()
        {
            var keys = new List<ConsoleKeyInfo>();
            while (Console.KeyAvailable)
            {
                keys.Add(Console.ReadKey(intercept: true));
            }

            return keys;
        }

        private async Task ProcessKeysAsync(IReadOnlyList<ConsoleKeyInfo> keys)
        {
            var now = DateTime.UtcNow;

            if (keys.Count == 0)
            {
                await MaybeReleaseMovementAsync(now);
                return;
            }

            var handledMovement = false;

            foreach (var keyInfo in keys)
            {
                switch (keyInfo.Key)
                {
                    case ConsoleKey.W:
                    case ConsoleKey.UpArrow:
                        await StartRelativeMovementAsync(MovementIntent.Forward);
                        handledMovement = true;
                        _lastMovementInput = now;
                        break;
                    case ConsoleKey.S:
                    case ConsoleKey.DownArrow:
                        await StartRelativeMovementAsync(MovementIntent.Backward);
                        handledMovement = true;
                        _lastMovementInput = now;
                        break;
                    case ConsoleKey.A:
                    case ConsoleKey.LeftArrow:
                        await StartRelativeMovementAsync(MovementIntent.StrafeLeft);
                        handledMovement = true;
                        _lastMovementInput = now;
                        break;
                    case ConsoleKey.D:
                    case ConsoleKey.RightArrow:
                        await StartRelativeMovementAsync(MovementIntent.StrafeRight);
                        handledMovement = true;
                        _lastMovementInput = now;
                        break;
                    case ConsoleKey.Q:
                        await RotateAsync(-1);
                        break;
                    case ConsoleKey.E:
                        await RotateAsync(1);
                        break;
                    case ConsoleKey.Spacebar:
                        await ReleaseMovementAsync();
                        break;
                    case ConsoleKey.Enter:
                    case ConsoleKey.I:
                        InspectCurrentTile();
                        break;
                    case ConsoleKey.Escape:
                        await ShutdownAsync();
                        _isRunning = false;
                        return;
                }
            }

            if (!handledMovement)
            {
                await MaybeReleaseMovementAsync(now);
            }
        }

        private enum MovementIntent
        {
            Forward,
            Backward,
            StrafeLeft,
            StrafeRight
        }

        private static int NormalizeDirection(int direction)
        {
            return ((direction - 1 + 8) % 8) + 1;
        }

        private static int OffsetDirection(int baseDirection, int offset)
        {
            return NormalizeDirection(baseDirection + offset);
        }

        private async Task StartRelativeMovementAsync(MovementIntent intent)
        {
            var direction = intent switch
            {
                MovementIntent.Forward => _facingDirection,
                MovementIntent.Backward => OffsetDirection(_facingDirection, 4),
                MovementIntent.StrafeLeft => OffsetDirection(_facingDirection, -2),
                MovementIntent.StrafeRight => OffsetDirection(_facingDirection, 2),
                _ => _facingDirection
            };

            await StartMovementAsync(direction, updateFacing: false);
        }

        private async Task RotateAsync(int step)
        {
            if (step == 0)
            {
                return;
            }

            var rotationCommand = step < 0 ? RotationLeftCommand : RotationRightCommand;
            var characterId = _session.CharacterId.ToString();
            var rotationStarted = false;

            try
            {
                var reply = await _client.StartRotationAsync(new MovementCommandRequest
                {
                    CharacterId = characterId,
                    Direction = rotationCommand
                });

                if (!reply.Success)
                {
                    LogError($"StartRotation odrzucone ({reply.ErrorCode}): {reply.Message}");
                }
                else
                {
                    rotationStarted = true;
                    _facingDirection = OffsetDirection(_facingDirection, step);
                }
            }
            catch (Exception ex)
            {
                LogError($"Rotation failed: {ex.Message}");
            }

            if (rotationStarted)
            {
                await StopRotationAsync(characterId);
            }
        }

        private async Task StopRotationAsync(string characterId)
        {
            try
            {
                var reply = await _client.StopRotationAsync(new CharacterIdRequest
                {
                    CharacterId = characterId
                });

                if (!reply.Success)
                {
                    LogError($"StopRotation odrzucone ({reply.ErrorCode}): {reply.Message}");
                }
            }
            catch (Exception ex)
            {
                LogError($"StopRotation failed: {ex.Message}");
            }
        }

        private async Task StartMovementAsync(int direction, bool updateFacing)
        {
            if (!DirectionVectors.ContainsKey(direction))
            {
                return;
            }

            if (!_movementActive || _movementDirection != direction)
            {
                try
                {
                    var reply = await _client.StartMovementAsync(new MovementCommandRequest
                    {
                        CharacterId = _session.CharacterId.ToString(),
                        Direction = direction
                    });

                    if (!reply.Success)
                    {
                        LogError($"StartMovement odrzucone ({reply.ErrorCode}): {reply.Message}");
                        return;
                    }

                    _movementActive = true;
                    _movementDirection = direction;
                    if (updateFacing)
                    {
                        _facingDirection = direction;
                    }
                }
                catch (Exception ex)
                {
                    LogError($"StartMovement failed: {ex.Message}");
                }
            }
        }

        private async Task ReleaseMovementAsync()
        {
            if (!_movementActive)
            {
                return;
            }

            try
            {
                var reply = await _client.StopMovementAsync(new CharacterIdRequest
                {
                    CharacterId = _session.CharacterId.ToString()
                });

                if (!reply.Success)
                {
                    LogError($"StopMovement odrzucone ({reply.ErrorCode}): {reply.Message}");
                    return;
                }

                _movementActive = false;
            }
            catch (Exception ex)
            {
                LogError($"StopMovement failed: {ex.Message}");
            }
        }

        private async Task MaybeReleaseMovementAsync(DateTime now)
        {
            if (_movementActive && now - _lastMovementInput > InputReleaseDelay)
            {
                await ReleaseMovementAsync();
            }
        }

        private void UpdatePosition()
        {
            var now = DateTime.UtcNow;
            var delta = now - _lastUpdate;
            _lastUpdate = now;

            if (!_movementActive)
            {
                return;
            }

            if (!DirectionVectors.TryGetValue(_movementDirection, out var vector))
            {
                return;
            }

            var moveUnits = delta.TotalSeconds * StepPerSecond;
            _movementRemainder += moveUnits;
            var steps = (int)_movementRemainder;
            if (steps <= 0)
            {
                return;
            }

            _movementRemainder -= steps;

            var (dx, dy, _) = vector;
            var newX = Math.Clamp(_position.x + dx * steps, 0, BoardWidth - 1);
            var newY = Math.Clamp(_position.y + dy * steps, 0, BoardHeight - 1);
            _position = (newX, newY);
        }

        private async Task MaybeSendHeartbeatAsync(DateTime now)
        {
            if (now - _lastHeartbeat < HeartbeatInterval)
            {
                return;
            }

            await SendHeartbeatAsync(now);
        }

        private async Task SendHeartbeatAsync(DateTime timestamp)
        {
            try
            {
                await _sessionClient.HeartbeatSessionAsync(new SessionHeartbeatRequest
                {
                    SessionId = _session.SessionId.ToString(),
                    Location = BuildHeartbeatLocation()
                });
            }
            catch (Exception ex)
            {
                LogError($"Heartbeat failed: {ex.Message}");
            }
            finally
            {
                _lastHeartbeat = timestamp;
            }
        }

        private Location? BuildHeartbeatLocation()
        {
            lock (_drawLock)
            {
                if (_lastWorldLocation == null)
                {
                    return null;
                }

                return new Location
                {
                    X = _lastWorldLocation.X,
                    Y = _lastWorldLocation.Y,
                    Z = _lastWorldLocation.Z,
                    WorldId = _lastWorldLocation.WorldId,
                    MapId = _lastWorldLocation.MapId,
                    ZoneName = _lastWorldLocation.ZoneName,
                    Rotation = _lastWorldLocation.Rotation
                };
            }
        }

        private async Task ShutdownAsync()
        {
            await ReleaseMovementAsync();
            await StopRotationAsync(_session.CharacterId.ToString());
            await StopWorldStreamAsync();

            if (_worldId.HasValue)
            {
                try
                {
                    await _worldClient.LeaveWorldAsync(new WorldMembershipRequest
                    {
                        SessionId = _session.SessionId.ToString()
                    });
                }
                catch (Exception ex)
                {
                    LogError($"Opuszczenie świata nie powiodło się: {ex.Message}");
                }
            }

            try
            {
                await _sessionClient.EndSessionAsync(new EndSessionRequest
                {
                    SessionId = _session.SessionId.ToString()
                });

                LogInfo($"Sesja {_session.SessionId} została zakończona.");
            }
            catch (Exception ex)
            {
                LogError($"Zamknięcie sesji nie powiodło się: {ex.Message}");
            }
        }

        private void LogError(string message)
        {
            EnqueueMessage($"Błąd: {message}");
            if (_automationMode)
            {
                AnsiConsole.MarkupLine($"[red]{message}[/]");
            }
        }

        private void LogInfo(string message)
        {
            EnqueueMessage(message);
            if (_automationMode)
            {
                AnsiConsole.MarkupLine(message);
            }
        }

        private void EnqueueMessage(string message)
        {
            lock (_drawLock)
            {
                if (_messages.Count >= MaxMessages)
                {
                    _messages.Dequeue();
                }

                _messages.Enqueue(message);
            }
        }

    private static string BuildBoard(
            (int x, int y) position,
            int facingDirection,
            IReadOnlyCollection<(int x, int y)> otherCharacters,
            IReadOnlyCollection<BoardEntity> npcEntities,
            IReadOnlyCollection<BoardEntity> mapEntities)
        {
            if (!DirectionVectors.TryGetValue(facingDirection, out var vector))
            {
                vector = DirectionVectors[1];
            }

            var arrow = vector.arrow;
            var boardBuilder = new StringBuilder();
            var otherOccupancy = new Dictionary<(int x, int y), int>();

            foreach (var other in otherCharacters)
            {
                if (other == position)
                {
                    continue;
                }

                if (otherOccupancy.TryGetValue(other, out var count))
                {
                    otherOccupancy[other] = count + 1;
                }
                else
                {
                    otherOccupancy[other] = 1;
                }
            }

            var npcLookup = npcEntities
                .GroupBy(npc => (npc.X, npc.Y))
                .ToDictionary(
                    group => group.Key,
                    group => group.Count() > 1
                        ? new BoardSymbol("&", group.First().Color)
                        : new BoardSymbol(group.First().Glyph, group.First().Color));

            var mapLookup = mapEntities
                .GroupBy(obj => (obj.X, obj.Y))
                .ToDictionary(
                    group => group.Key,
                    group => group.Count() > 1
                        ? new BoardSymbol("#", group.First().Color)
                        : new BoardSymbol(group.First().Glyph, group.First().Color));

            for (var y = 0; y < BoardHeight; y++)
            {
                for (var x = 0; x < BoardWidth; x++)
                {
                    if (x == position.x && y == position.y)
                    {
                        boardBuilder.Append('[')
                                    .Append(ArrowColor)
                                    .Append(']')
                                    .Append(arrow)
                                    .Append("[/]");
                    }
                    else if (otherOccupancy.TryGetValue((x, y), out var occupants))
                    {
                        boardBuilder.Append('[')
                                    .Append(OtherPlayerColor)
                                    .Append(']')
                                    .Append(occupants > 1 ? '+' : 'P')
                                    .Append("[/]");
                    }
                    else if (npcLookup.TryGetValue((x, y), out var npcSymbol))
                    {
                        boardBuilder.Append('[')
                                    .Append(npcSymbol.Color)
                                    .Append(']')
                                    .Append(npcSymbol.Glyph)
                                    .Append("[/]");
                    }
                    else if (mapLookup.TryGetValue((x, y), out var mapSymbol))
                    {
                        boardBuilder.Append('[')
                                    .Append(mapSymbol.Color)
                                    .Append(']')
                                    .Append(mapSymbol.Glyph)
                                    .Append("[/]");
                    }
                    else
                    {
                        boardBuilder.Append("[grey30].[/]");
                    }
                }

                if (y < BoardHeight - 1)
                {
                    boardBuilder.Append('\n');
                }
            }

            return boardBuilder.ToString();
        }

        private IRenderable Render()
        {
            var otherCharacters = _otherCharacters.Values.ToList();
            var npcEntities = _npcs.Values.Select(n => n.ToBoardEntity()).ToList();
            var mapEntities = _mapObjects.Values.Select(m => m.ToBoardEntity()).ToList();
            var board = BuildBoard(
                _position,
                _facingDirection,
                otherCharacters.Select(c => (c.x, c.y)).ToList(),
                npcEntities,
                mapEntities);
            var facingVector = DirectionVectors.TryGetValue(_facingDirection, out var vector)
                ? vector
                : DirectionVectors[1];
            var facingAngle = (_facingDirection - 1) * 45;
            var status = new StringBuilder()
                .AppendLine("Sterowanie:")
                .AppendLine("  Q / E – obrót o 45° (lewo / prawo)")
                .AppendLine("  W – ruch do przodu względem kierunku")
                .AppendLine("  S – ruch do tyłu")
                .AppendLine("  A / D – ruch w lewo / prawo (strafe)")
                .AppendLine("  Strzałki działają analogicznie do WASD")
                .AppendLine("  Spacja – zatrzymaj ruch")
                .AppendLine("  Pomarańczowa strzałka pokazuje aktualny kierunek")
                .AppendLine("  Esc – zakończ aplikację")
                .AppendLine()
                .AppendLine($"Gracz: {_session.PlayerName}")
                .AppendLine($"Sesja: {_session.SessionId}")
                .AppendLine($"Postać: {_session.CharacterId}")
                .AppendLine($"Pozycja: {_position.x},{_position.y}")
                .AppendLine($"Facing: {facingAngle}° ({facingVector.arrow}) [dir {_facingDirection}]")
                .AppendLine($"Ruch: {_movementActive} (dir {_movementDirection})")
                .AppendLine($"NPC na mapie: {npcEntities.Count}")
                .AppendLine($"Obiekty mapy: {mapEntities.Count}")
                .AppendLine($"Granice świata: {GetWorldBoundsSummary()}");

            status.AppendLine($"NPC ogółem: {_npcSummary.Total}, przyjaźni: {_npcSummary.Friendly}, wrodzy: {_npcSummary.Hostile}, kupcy: {_npcSummary.Merchants}, trenerzy: {_npcSummary.Trainers}");
            status.AppendLine($"Obiekty: spawn {_mapSummary.SpawnPoints}, struktury {_mapSummary.Structures}, targ {_mapSummary.Markets}, trening {_mapSummary.TrainingZones}, interaktywne {_mapSummary.Interactive}");

            if (_lastSnapshotTick > 0)
            {
                var lastSnapshotLocal = DateTimeOffset.FromUnixTimeMilliseconds(_lastSnapshotTick).ToLocalTime();
                status.AppendLine($"Ostatnia migawka: {lastSnapshotLocal:yyyy-MM-dd HH:mm:ss}");
            }

            if (_worldId.HasValue)
            {
                var worldLabel = _worldName ?? _worldId.Value.ToString();
                status.AppendLine($"Świat: {worldLabel}")
                      .AppendLine($"Gracze w świecie: {_worldPopulation}");
            }

            if (otherCharacters.Count > 0)
            {
                status.AppendLine()
                      .AppendLine("Inni gracze w pobliżu:");

                foreach (var other in otherCharacters.Take(5))
                {
                    status.AppendLine($"  - {other.displayName} ({other.x},{other.y})");
                }

                if (otherCharacters.Count > 5)
                {
                    status.AppendLine($"  ... i {otherCharacters.Count - 5} więcej");
                }
            }

            if (npcEntities.Count > 0)
            {
                status.AppendLine()
                      .AppendLine("NPC w pobliżu:");

                foreach (var npc in npcEntities.Take(5))
                {
                    status.AppendLine($"  - {npc.DisplayName} ({npc.X},{npc.Y})");
                }

                if (npcEntities.Count > 5)
                {
                    status.AppendLine($"  ... i {npcEntities.Count - 5} więcej");
                }
            }
            else
            {
                status.AppendLine()
                      .AppendLine("NPC w pobliżu: brak danych (poczekaj na aktualizację świata)");
            }

            if (_spawnPoints.Count > 0)
            {
                status.AppendLine()
                      .AppendLine("Punkty odrodzenia:");

                foreach (var spawn in _spawnPoints.Take(5))
                {
                    status.AppendLine($"  - {spawn.Label} ({spawn.X},{spawn.Y})");
                }

                if (_spawnPoints.Count > 5)
                {
                    status.AppendLine($"  ... i {_spawnPoints.Count - 5} więcej");
                }
            }

            status.AppendLine()
                  .AppendLine("Legenda mapy:")
                  .AppendLine("  ! – wrogie NPC")
                  .AppendLine("  $ – kupcy")
                  .AppendLine("  T – trenerzy")
                  .AppendLine("  F – przyjaźni NPC")
                  .AppendLine("  H – budynki / struktury")
            .AppendLine("  M – stragany / rynek")
            .AppendLine("  X – strefy treningowe")
            .AppendLine("  S – punkty odrodzenia")
            .AppendLine("  C – skrzynie / interakcje")
            .AppendLine("  ^ – las / drzewa")
            .AppendLine("  ~ – woda / doki")
            .AppendLine("  = – ścieżki");

            var boardPanel = new Panel(new Markup(board))
            {
                Header = new PanelHeader("RPG Desktop Client"),
                Padding = new Padding(2, 1),
                Border = BoxBorder.Rounded
            };

            if (_messages.Count > 0)
            {
                status.AppendLine()
                      .AppendLine("Komunikaty:");

                foreach (var msg in _messages)
                {
                    status.AppendLine($"  - {msg}");
                }
            }

            var infoPanel = new Panel(new Text(status.ToString()))
            {
                Header = new PanelHeader("Status"),
                Padding = new Padding(1, 0),
                Border = BoxBorder.Rounded
            };

            var entityPanel = BuildEntityPanel();
            var serverDataPanel = BuildServerDataPanel();
            var rightColumn = new Rows(infoPanel, entityPanel, serverDataPanel);

            return new Columns(boardPanel, rightColumn);
        }

        private string GetWorldBoundsSummary()
        {
            if (!_hasWorldBounds)
            {
                return "brak danych";
            }

            return FormattableString.Invariant($"X {_worldMinX:F1} – {_worldMaxX:F1}, Y {_worldMinY:F1} – {_worldMaxY:F1}");
        }

        private IRenderable BuildEntityPanel()
        {
            var table = new Table().Title("Otoczenie");
            table.AddColumn(new TableColumn("Typ"));
            table.AddColumn(new TableColumn("Nazwa"));
            table.AddColumn(new TableColumn("Szczegóły"));

            if (_tileDetails.TryGetValue(_position, out var tile) && (tile.Npcs.Count > 0 || tile.MapObjects.Count > 0))
            {
                foreach (var npc in tile.Npcs)
                {
                    var details = new StringBuilder();
                    details.Append("Stan: ").Append(npc.IsAlive ? "żywy" : "nieaktywny");

                    if (npc.Tags.Count > 0)
                    {
                        details.Append(" | Tagi: ").Append(string.Join(", ", npc.Tags.Take(5)));
                        if (npc.Tags.Count > 5)
                        {
                            details.Append(" +").Append(npc.Tags.Count - 5);
                        }
                    }

                    details.Append(" | Pozycja: ")
                        .AppendFormat(CultureInfo.InvariantCulture, "{0:0.0},{1:0.0}", npc.Location.X, npc.Location.Y);

                    table.AddRow("NPC", npc.DisplayName, details.ToString());
                }

                foreach (var mapObject in tile.MapObjects)
                {
                    var details = new StringBuilder();
                    details.Append("Stan: ").Append(mapObject.IsActive ? "aktywne" : "nieaktywne");

                    if (mapObject.Tags.Count > 0)
                    {
                        details.Append(" | Tagi: ").Append(string.Join(", ", mapObject.Tags.Take(5)));
                        if (mapObject.Tags.Count > 5)
                        {
                            details.Append(" +").Append(mapObject.Tags.Count - 5);
                        }
                    }

                    if (mapObject.State.Count > 0)
                    {
                        var preview = mapObject.State.Take(2).Select(kvp => $"{kvp.Key}={kvp.Value}");
                        details.Append(" | Stan: ").Append(string.Join("; ", preview));
                    }

                    details.Append(" | Pozycja: ")
                        .AppendFormat(CultureInfo.InvariantCulture, "{0:0.0},{1:0.0}", mapObject.Location.X, mapObject.Location.Y);

                    table.AddRow("Obiekt", mapObject.DisplayName, details.ToString());
                }
            }
            else
            {
                table.AddRow("-", "Brak", "Na tym polu nie ma NPC ani obiektów.");
            }

            return new Panel(table)
            {
                Header = new PanelHeader("Szczegóły pola"),
                Padding = new Padding(1, 0),
                Border = BoxBorder.Rounded
            };
        }

        private IRenderable BuildServerDataPanel()
        {
            var table = new Table().Title("Dane z serwera");
            table.AddColumn(new TableColumn("Typ"));
            table.AddColumn(new TableColumn("Nazwa"));
            table.AddColumn(new TableColumn("Pozycja świata"));
            table.AddColumn(new TableColumn("Na planszy"));

            var hasRows = false;

            if (_lastWorldLocation != null)
            {
                var label = string.IsNullOrWhiteSpace(_session.PlayerName)
                    ? _session.CharacterId.ToString()
                    : _session.PlayerName;

                table.AddRow("Postać", label, FormatWorldLocation(_lastWorldLocation), FormatBoardPosition(_position));
                hasRows = true;
            }

            foreach (var npc in _npcs.Values.OrderBy(n => n.DisplayName).Take(5))
            {
                if (npc.Location == null)
                {
                    continue;
                }

                table.AddRow("NPC", npc.DisplayName, FormatWorldLocation(npc.Location), FormatBoardPosition((npc.X, npc.Y)));
                hasRows = true;
            }

            foreach (var mapObject in _mapObjects.Values.OrderBy(m => m.DisplayName).Take(5))
            {
                if (mapObject.Location == null)
                {
                    continue;
                }

                table.AddRow("Obiekt", mapObject.DisplayName, FormatWorldLocation(mapObject.Location), FormatBoardPosition((mapObject.X, mapObject.Y)));
                hasRows = true;
            }

            if (_hasWorldBounds)
            {
                var worldLabel = _worldName ?? _worldId?.ToString() ?? "-";
                var bounds = FormattableString.Invariant($"X {_worldMinX:0.0}–{_worldMaxX:0.0}, Y {_worldMinY:0.0}–{_worldMaxY:0.0}");
                table.AddRow("Świat", worldLabel, bounds, FormattableString.Invariant($"{BoardWidth}x{BoardHeight}"));
                hasRows = true;
            }

            if (!hasRows)
            {
                table.AddRow("-", "Brak danych", "-", "-");
            }

            return new Panel(table)
            {
                Header = new PanelHeader("Migawka serwera"),
                Padding = new Padding(1, 0),
                Border = BoxBorder.Rounded
            };
        }

        private async Task RunAutomationAsync()
        {
            LogInfo("Automation mode aktywny – oczekiwanie na strumień świata.");

            var snapshotTask = _firstSnapshotTcs?.Task ?? Task.CompletedTask;
            var initial = await Task.WhenAny(snapshotTask, Task.Delay(_automationDuration));
            if (initial != snapshotTask)
            {
                LogError("Nie odebrano migawki świata przed upływem limitu czasu w trybie automatycznym.");
            }

            var watch = DateTime.UtcNow + _automationDuration;
            while (DateTime.UtcNow < watch)
            {
                await MaybeSendHeartbeatAsync(DateTime.UtcNow);
                await Task.Delay(200);
            }

            var boundsSummary = _hasWorldBounds
                ? FormattableString.Invariant($"X {_worldMinX:F2} – {_worldMaxX:F2}, Y {_worldMinY:F2} – {_worldMaxY:F2}")
                : "brak danych";

            LogInfo($"Ostatnia migawka świata: {_lastSnapshotTick}.");
            LogInfo($"Zaobserwowane granice świata: {boundsSummary}.");
            LogInfo($"Widoczne NPC: {_npcs.Count}, obiekty mapy: {_mapObjects.Count}.");
            LogInfo($"Podsumowanie NPC – przyjaźni: {_npcSummary.Friendly}, wrodzy: {_npcSummary.Hostile}, kupcy: {_npcSummary.Merchants}, trenerzy: {_npcSummary.Trainers}.");
            LogInfo($"Podsumowanie obiektów – spawn: {_mapSummary.SpawnPoints}, struktury: {_mapSummary.Structures}, targ: {_mapSummary.Markets}, trening: {_mapSummary.TrainingZones}, interaktywne: {_mapSummary.Interactive}.");

            if (_otherCharacters.Count > 0)
            {
                foreach (var other in _otherCharacters.Values.Take(10))
                {
                    LogInfo($"Postać {other.displayName} na ({other.x},{other.y}).");
                }
            }

            await ShutdownAsync();
            _isRunning = false;
        }

        private void StartWorldStream(Guid worldId)
        {
            if (_worldStreamTask != null)
            {
                return;
            }

            var cts = new CancellationTokenSource();
            _worldStreamCts = cts;
            _worldStreamTask = StreamWorldStateAsync(worldId, cts.Token);
        }

        private async Task StreamWorldStateAsync(Guid worldId, CancellationToken cancellationToken)
        {
            try
            {
                using var call = _worldClient.StreamWorldState(new WorldStreamRequest
                {
                    SessionId = _session.SessionId.ToString(),
                    WorldId = worldId.ToString(),
                    IntervalMilliseconds = 500
                }, cancellationToken: cancellationToken);

                var responseStream = call.ResponseStream;
                while (await responseStream.MoveNext(cancellationToken))
                {
                    ApplyWorldSnapshot(responseStream.Current.Snapshot);
                }
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled && cancellationToken.IsCancellationRequested)
            {
                // graceful shutdown
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // stream cancelled
            }
            catch (Exception ex)
            {
                LogError($"World stream error: {ex.Message}");
            }
        }

        private async Task StopWorldStreamAsync()
        {
            var cts = _worldStreamCts;
            var task = _worldStreamTask;
            _worldStreamCts = null;
            _worldStreamTask = null;

            if (cts == null)
            {
                return;
            }

            try
            {
                cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // already disposed
            }

            if (task != null)
            {
                try
                {
                    var completed = await Task.WhenAny(task, Task.Delay(2000));
                    if (completed == task && task.IsFaulted && task.Exception != null)
                    {
                        LogError($"World stream zakończył się błędem: {task.Exception.GetBaseException().Message}");
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Zatrzymanie strumienia świata nie powiodło się: {ex.Message}");
                }
            }

            cts.Dispose();
        }

        private void ApplyWorldSnapshot(WorldSnapshot? snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            if (snapshot.Metadata != null)
            {
                if (Guid.TryParse(snapshot.Metadata.WorldId, out var parsedWorldId))
                {
                    _worldId = parsedWorldId;
                }

                if (!string.IsNullOrWhiteSpace(snapshot.Metadata.WorldName))
                {
                    _worldName = snapshot.Metadata.WorldName;
                }
            }

            var mapObjectList = snapshot.MapObjects?
                .Where(o => o != null && o.Location != null)
                .ToList()
                ?? new List<WorldMapObject>();

            var npcList = snapshot.Npcs?
                .Where(n => n != null && n.Location != null)
                .ToList()
                ?? new List<WorldNpc>();

            var npcSummary = BuildNpcSummary(npcList);
            var mapSummary = BuildMapSummary(mapObjectList);

            lock (_drawLock)
            {
                _otherCharacters.Clear();
                _mapObjects.Clear();
                _npcs.Clear();
                _spawnPoints.Clear();
                _tileDetails.Clear();

                foreach (var mapObject in mapObjectList)
                {
                    UpdateWorldBounds(mapObject.Location);
                }

                foreach (var npc in npcList)
                {
                    UpdateWorldBounds(npc.Location);
                }

                if (snapshot.Characters != null)
                {
                    var population = 0;

                    foreach (var character in snapshot.Characters)
                    {
                        if (character == null || !character.IsOnline)
                        {
                            continue;
                        }

                        population++;

                        if (!Guid.TryParse(character.CharacterId, out var characterId))
                        {
                            continue;
                        }

                        UpdateWorldBounds(character.Location);
                        var location = MapWorldToBoard(character.Location);

                        if (characterId == _session.CharacterId)
                        {
                            _lastWorldLocation = character.Location != null
                                ? new Location
                                {
                                    X = character.Location.X,
                                    Y = character.Location.Y,
                                    Z = character.Location.Z,
                                    WorldId = character.Location.WorldId,
                                    MapId = character.Location.MapId,
                                    ZoneName = character.Location.ZoneName,
                                    Rotation = character.Location.Rotation
                                }
                                : null;
                            _position = location;
                            continue;
                        }

                        var displayName = string.IsNullOrWhiteSpace(character.DisplayName)
                            ? character.CharacterId
                            : character.DisplayName;

                        _otherCharacters[characterId] = (location.x, location.y, displayName);
                    }

                    _worldPopulation = population;
                }
                else
                {
                    _worldPopulation = 0;
                }

                foreach (var npc in npcList)
                {
                    if (!Guid.TryParse(npc.NpcId, out var npcId))
                    {
                        continue;
                    }

                    var board = MapWorldToBoard(npc.Location);
                    var symbol = ResolveNpcSymbol(npc);
                    var label = string.IsNullOrWhiteSpace(npc.Name) ? npc.NpcId : npc.Name;

                    var npcEntity = new NpcBoardEntity(
                        npcId,
                        label,
                        board.x,
                        board.y,
                        symbol.Glyph,
                        symbol.Color,
                        npc.Location,
                        npc.Tags.ToList(),
                        npc.IsAlive);

                    _npcs[npcId] = npcEntity;
                    AddTileNpc(board, npcEntity);
                }

                foreach (var mapObject in mapObjectList)
                {
                    if (!Guid.TryParse(mapObject.MapObjectId, out var mapObjectId))
                    {
                        continue;
                    }

                    var board = MapWorldToBoard(mapObject.Location);
                    var symbol = ResolveMapObjectSymbol(mapObject);
                    var label = string.IsNullOrWhiteSpace(mapObject.DisplayName)
                        ? mapObject.Name
                        : mapObject.DisplayName;

                    var mapEntity = new MapObjectBoardEntity(
                        mapObjectId,
                        label,
                        board.x,
                        board.y,
                        symbol.Glyph,
                        symbol.Color,
                        mapObject.Location,
                        mapObject.Tags.ToList(),
                        mapObject.State.ToDictionary(entry => entry.Key, entry => entry.Value),
                        mapObject.IsActive);

                    _mapObjects[mapObjectId] = mapEntity;
                    AddTileMapObject(board, mapEntity);

                    if (HasTag(mapObject.Tags, "spawn"))
                    {
                        _spawnPoints.Add((label, board.x, board.y));
                    }
                }

                _npcSummary = npcSummary;
                _mapSummary = mapSummary;
            }

            if (snapshot.LastUpdated > 0)
            {
                Interlocked.Exchange(ref _lastSnapshotTick, snapshot.LastUpdated);
            }

            _firstSnapshotTcs?.TrySetResult(true);
        }

        private void UpdateWorldBounds(Location? location)
        {
            if (location == null)
            {
                return;
            }

            if (!_hasWorldBounds)
            {
                _worldMinX = _worldMaxX = location.X;
                _worldMinY = _worldMaxY = location.Y;
                _hasWorldBounds = true;
                return;
            }

            if (location.X < _worldMinX)
            {
                _worldMinX = location.X;
            }

            if (location.X > _worldMaxX)
            {
                _worldMaxX = location.X;
            }

            if (location.Y < _worldMinY)
            {
                _worldMinY = location.Y;
            }

            if (location.Y > _worldMaxY)
            {
                _worldMaxY = location.Y;
            }
        }

        private (int x, int y) MapWorldToBoard(Location? location)
        {
            if (location == null)
            {
                return (BoardWidth / 2, BoardHeight / 2);
            }

            var minX = _hasWorldBounds ? _worldMinX : location.X;
            var maxX = _hasWorldBounds ? _worldMaxX : location.X;
            var minY = _hasWorldBounds ? _worldMinY : location.Y;
            var maxY = _hasWorldBounds ? _worldMaxY : location.Y;

            var width = Math.Max(1d, maxX - minX);
            var height = Math.Max(1d, maxY - minY);

            var normalizedX = (location.X - minX) / width;
            var normalizedY = (location.Y - minY) / height;

            var boardX = (int)Math.Round(normalizedX * (BoardWidth - 1), MidpointRounding.AwayFromZero);
            var boardY = (int)Math.Round(normalizedY * (BoardHeight - 1), MidpointRounding.AwayFromZero);

            var clampedX = Math.Clamp(boardX, 0, BoardWidth - 1);
            var clampedY = Math.Clamp(boardY, 0, BoardHeight - 1);

            return (clampedX, clampedY);
        }

        private static BoardSymbol ResolveNpcSymbol(WorldNpc npc)
        {
            if (HasTag(npc?.Tags, "enemy") || HasTag(npc?.Tags, "hostile"))
            {
                return new BoardSymbol("!", "red1");
            }

            if (HasTag(npc?.Tags, "merchant"))
            {
                return new BoardSymbol("$", "gold1");
            }

            if (HasTag(npc?.Tags, "trainer"))
            {
                return new BoardSymbol("T", "springgreen2");
            }

            if (HasTag(npc?.Tags, "friendly") || HasTag(npc?.Tags, "quest"))
            {
                return new BoardSymbol("F", "deepskyblue1");
            }

            return new BoardSymbol("N", "silver");
        }

        private static BoardSymbol ResolveMapObjectSymbol(WorldMapObject mapObject)
        {
            if (HasTag(mapObject?.Tags, "spawn"))
            {
                return new BoardSymbol("S", "yellow3");
            }

            if (HasTag(mapObject?.Tags, "market") || HasTag(mapObject?.Tags, "merchant"))
            {
                return new BoardSymbol("M", "orange3");
            }

            if (HasTag(mapObject?.Tags, "building") || HasTag(mapObject?.Tags, "structure") || HasTag(mapObject?.Tags, "hub"))
            {
                return new BoardSymbol("H", "gold3");
            }

            if (HasTag(mapObject?.Tags, "training") || HasTag(mapObject?.Tags, "arena"))
            {
                return new BoardSymbol("X", "plum3");
            }

            if (HasTag(mapObject?.Tags, "chest") || HasTag(mapObject?.Tags, "interactive"))
            {
                return new BoardSymbol("C", "lightgoldenrod1");
            }

            if (HasTag(mapObject?.Tags, "water") || HasTag(mapObject?.Tags, "dock"))
            {
                return new BoardSymbol("~", "teal");
            }

            if (HasTag(mapObject?.Tags, "path") || HasTag(mapObject?.Tags, "cobblestone"))
            {
                return new BoardSymbol("=", "grey70");
            }

            if (HasTag(mapObject?.Tags, "tree") || HasTag(mapObject?.Tags, "forest") || HasTag(mapObject?.Tags, "shrine") || HasTag(mapObject?.Tags, "lore"))
            {
                return new BoardSymbol("^", "green3");
            }

            return new BoardSymbol("#", "silver");
        }

        private static bool HasTag(IEnumerable<string>? tags, string value)
        {
            if (tags is null)
            {
                return false;
            }

            return tags.Any(tag =>
                !string.IsNullOrWhiteSpace(tag) &&
                tag.Contains(value, StringComparison.OrdinalIgnoreCase));
        }

        private void AddTileNpc((int x, int y) boardPosition, NpcBoardEntity entity)
        {
            if (!_tileDetails.TryGetValue(boardPosition, out var tile))
            {
                tile = new TileDetails();
                _tileDetails[boardPosition] = tile;
            }

            tile.Npcs.Add(entity);
        }

        private void AddTileMapObject((int x, int y) boardPosition, MapObjectBoardEntity entity)
        {
            if (!_tileDetails.TryGetValue(boardPosition, out var tile))
            {
                tile = new TileDetails();
                _tileDetails[boardPosition] = tile;
            }

            tile.MapObjects.Add(entity);
        }

        private void InspectCurrentTile()
        {
            bool hasTile;
            TileDetails? tile;
            lock (_drawLock)
            {
                hasTile = _tileDetails.TryGetValue(_position, out tile);
            }

            if (!hasTile || tile == null || (tile.Npcs.Count == 0 && tile.MapObjects.Count == 0))
            {
                LogInfo("Wybrane pole jest puste.");
                return;
            }

            LogInfo("Szczegóły bieżącego pola:");

            foreach (var npc in tile.Npcs)
            {
                var tags = npc.Tags.Count > 0 ? string.Join(',', npc.Tags) : "brak";
                LogInfo(FormattableString.Invariant($"  NPC: {npc.DisplayName} [{tags}] @ {npc.Location.X:0.0},{npc.Location.Y:0.0}"));
            }

            foreach (var mapObject in tile.MapObjects)
            {
                var tags = mapObject.Tags.Count > 0 ? string.Join(',', mapObject.Tags) : "brak";
                var state = mapObject.State.Count > 0
                    ? string.Join("; ", mapObject.State.Take(3).Select(kvp => $"{kvp.Key}={kvp.Value}"))
                    : "brak";

                LogInfo(FormattableString.Invariant($"  Obiekt: {mapObject.DisplayName} [{tags}] @ {mapObject.Location.X:0.0},{mapObject.Location.Y:0.0} (stan: {state})"));
            }
        }

        private static WorldNpcSummary BuildNpcSummary(IReadOnlyCollection<WorldNpc> npcs)
        {
            if (npcs.Count == 0)
            {
                return WorldNpcSummary.Empty;
            }

            var friendly = 0;
            var hostile = 0;
            var merchants = 0;
            var trainers = 0;

            foreach (var npc in npcs)
            {
                if (HasTag(npc.Tags, "friendly"))
                {
                    friendly++;
                }

                if (HasTag(npc.Tags, "hostile") || HasTag(npc.Tags, "enemy"))
                {
                    hostile++;
                }

                if (HasTag(npc.Tags, "merchant"))
                {
                    merchants++;
                }

                if (HasTag(npc.Tags, "trainer"))
                {
                    trainers++;
                }
            }

            return new WorldNpcSummary(npcs.Count, friendly, hostile, merchants, trainers);
        }

        private static WorldMapSummary BuildMapSummary(IReadOnlyCollection<WorldMapObject> mapObjects)
        {
            if (mapObjects.Count == 0)
            {
                return WorldMapSummary.Empty;
            }

            var spawn = 0;
            var structures = 0;
            var markets = 0;
            var training = 0;
            var interactive = 0;
            var water = 0;
            var paths = 0;
            var nature = 0;
            var other = 0;

            foreach (var mapObject in mapObjects)
            {
                if (HasTag(mapObject.Tags, "spawn"))
                {
                    spawn++;
                }
                else if (HasTag(mapObject.Tags, "market") || HasTag(mapObject.Tags, "merchant"))
                {
                    markets++;
                }
                else if (HasTag(mapObject.Tags, "building") || HasTag(mapObject.Tags, "structure") || HasTag(mapObject.Tags, "hub"))
                {
                    structures++;
                }
                else if (HasTag(mapObject.Tags, "training") || HasTag(mapObject.Tags, "arena"))
                {
                    training++;
                }
                else if (HasTag(mapObject.Tags, "chest") || HasTag(mapObject.Tags, "interactive"))
                {
                    interactive++;
                }
                else if (HasTag(mapObject.Tags, "water") || HasTag(mapObject.Tags, "dock"))
                {
                    water++;
                }
                else if (HasTag(mapObject.Tags, "path") || HasTag(mapObject.Tags, "cobblestone"))
                {
                    paths++;
                }
                else if (HasTag(mapObject.Tags, "tree") || HasTag(mapObject.Tags, "forest") || HasTag(mapObject.Tags, "shrine") || HasTag(mapObject.Tags, "lore"))
                {
                    nature++;
                }
                else
                {
                    other++;
                }
            }

            return new WorldMapSummary(spawn, structures, markets, training, interactive, water, paths, nature, other);
        }

        private sealed record BoardEntity(int X, int Y, string DisplayName, string Glyph, string Color);

        private readonly record struct BoardSymbol(string Glyph, string Color);

        private sealed record NpcBoardEntity(
            Guid Id,
            string DisplayName,
            int X,
            int Y,
            string Glyph,
            string Color,
            Location Location,
            IReadOnlyList<string> Tags,
            bool IsAlive)
        {
            public BoardEntity ToBoardEntity() => new(X, Y, DisplayName, Glyph, Color);
        }

        private sealed record MapObjectBoardEntity(
            Guid Id,
            string DisplayName,
            int X,
            int Y,
            string Glyph,
            string Color,
            Location Location,
            IReadOnlyList<string> Tags,
            IReadOnlyDictionary<string, string> State,
            bool IsActive)
        {
            public BoardEntity ToBoardEntity() => new(X, Y, DisplayName, Glyph, Color);
        }

        private sealed class TileDetails
        {
            public List<NpcBoardEntity> Npcs { get; } = new();
            public List<MapObjectBoardEntity> MapObjects { get; } = new();
        }

        private readonly record struct WorldNpcSummary(int Total, int Friendly, int Hostile, int Merchants, int Trainers)
        {
            public static WorldNpcSummary Empty => new(0, 0, 0, 0, 0);
        }

        private readonly record struct WorldMapSummary(int SpawnPoints, int Structures, int Markets, int TrainingZones, int Interactive, int Water, int Paths, int Nature, int Other)
        {
            public static WorldMapSummary Empty => new(0, 0, 0, 0, 0, 0, 0, 0, 0);
        }

        private static string FormatWorldLocation(Location location)
        {
            var coordinates = FormattableString.Invariant($"{location.X:0.0},{location.Y:0.0}");

            if (Math.Abs(location.Z) > 0.01)
            {
                coordinates += FormattableString.Invariant($" (Z {location.Z:0.0})");
            }

            if (!string.IsNullOrWhiteSpace(location.ZoneName))
            {
                coordinates += FormattableString.Invariant($" [{location.ZoneName}]");
            }

            return coordinates;
        }

        private static string FormatBoardPosition((int x, int y) boardPosition)
        {
            return FormattableString.Invariant($"{boardPosition.x},{boardPosition.y}");
        }
    }

    private sealed record CharacterSession(Guid CharacterId, Guid SessionId, Guid PlayerId, string PlayerName)
    {
        public Guid? WorldId { get; init; }
        public string? WorldName { get; init; }
        public Location? SpawnLocation { get; init; }
    }

    private sealed record PlayerProfile(Guid PlayerId, string DisplayName);
}
