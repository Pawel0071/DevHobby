using System.Text;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using RPG.GameServer.Protos;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace RPG.DesktopClient;

internal static class Program
{
    private const int BoardWidth = 35;
    private const int BoardHeight = 17;
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
        var player = CreatePlayerProfile();

        try
        {
            Console.CursorVisible = false;

            var session = await InitializeGameSessionAsync(characterClient, sessionClient, player);
            var controller = new CharacterConsoleController(characterClient, sessionClient, session);

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
    private readonly CharacterService.CharacterServiceClient _client;
    private readonly SessionService.SessionServiceClient _sessionClient;
        private readonly CharacterSession _session;
        private readonly object _drawLock = new();

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
        private const int RotationLeftCommand = 7;
        private const int RotationRightCommand = 3;
        private const string ArrowColor = "orange1";

        public CharacterConsoleController(
            CharacterService.CharacterServiceClient client,
            SessionService.SessionServiceClient sessionClient,
            CharacterSession session)
        {
            _client = client;
            _sessionClient = sessionClient;
            _session = session;

            LogInfo($"Gracz {_session.PlayerName} rozpoczął sesję {_session.SessionId}.");
        }

        public async Task RunAsync()
        {
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
                await _client.StartRotationAsync(new MovementCommandRequest
                {
                    CharacterId = characterId,
                    Direction = rotationCommand
                });

                rotationStarted = true;
                _facingDirection = OffsetDirection(_facingDirection, step);
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
                await _client.StopRotationAsync(new CharacterIdRequest
                {
                    CharacterId = characterId
                });
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
                    await _client.StartMovementAsync(new MovementCommandRequest
                    {
                        CharacterId = _session.CharacterId.ToString(),
                        Direction = direction
                    });

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
                await _client.StopMovementAsync(new CharacterIdRequest
                {
                    CharacterId = _session.CharacterId.ToString()
                });

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
                    Location = new Location
                    {
                        X = _position.x,
                        Y = _position.y,
                        Z = 0
                    }
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

        private async Task ShutdownAsync()
        {
            await ReleaseMovementAsync();
            await StopRotationAsync(_session.CharacterId.ToString());

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
        }

        private void LogInfo(string message)
        {
            EnqueueMessage(message);
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

        private static string BuildBoard((int x, int y) position, int facingDirection)
        {
            if (!DirectionVectors.TryGetValue(facingDirection, out var vector))
            {
                vector = DirectionVectors[1];
            }

            var arrow = vector.arrow;
            var boardBuilder = new StringBuilder();

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
            var board = BuildBoard(_position, _facingDirection);
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
                .AppendLine($"Ruch: {_movementActive} (dir {_movementDirection})");

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

            return new Columns(boardPanel, infoPanel);
        }
    }

    private sealed record CharacterSession(Guid CharacterId, Guid SessionId, Guid PlayerId, string PlayerName);

    private sealed record PlayerProfile(Guid PlayerId, string DisplayName);
}
