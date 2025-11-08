wwusing System.Text;
using Grpc.Core;
using Grpc.Net.Client;
using RPG.GameServer.Protos;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace RPG.DesktopClient;

internal static class Program
{
    private const int BoardWidth = 25;
    private const int BoardHeight = 13;
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

    public static async Task Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.TreatControlCAsInput = true;

        var serverAddress = Environment.GetEnvironmentVariable("RPG_GAMESERVER_URL") ?? "http://localhost:5124";

    using var channel = GrpcChannel.ForAddress(serverAddress);
        var client = new CharacterService.CharacterServiceClient(channel);

        try
        {
            Console.CursorVisible = false;

            var session = await CreateCharacterAsync(client);
            var controller = new CharacterConsoleController(client, session);

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

    private static async Task<CharacterSession> CreateCharacterAsync(CharacterService.CharacterServiceClient client)
    {
        var characterId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        var request = new CharacterRequest
        {
            Character = new PlayerCharacter
            {
                SessionId = sessionId.ToString(),
                CharacterClass = CharacterClass.Warrior,
                BaseCharacter = new BaseCharacter
                {
                    Id = characterId.ToString(),
                    Name = $"ConsoleHero-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
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
        var createdCharacterId = Guid.Parse(response.CharacterId);

        return new CharacterSession(createdCharacterId, sessionId);
    }

    private sealed class CharacterConsoleController
    {
        private readonly CharacterService.CharacterServiceClient _client;
        private readonly CharacterSession _session;
        private readonly object _drawLock = new();

        private (int x, int y) _position = (BoardWidth / 2, BoardHeight / 2);
        private int _facingDirection = 1;
        private bool _movementActive;
        private bool _rotationActive;
        private int _movementDirection = 1;
    private int _rotationDirection = 1;
    private double _movementRemainder;
    private DateTime _lastUpdate = DateTime.UtcNow;
    private bool _isRunning = true;
    private readonly Queue<string> _messages = new();
    private const int MaxMessages = 5;
    private static readonly TimeSpan InputReleaseDelay = TimeSpan.FromMilliseconds(200);
    private DateTime _lastMovementInput = DateTime.MinValue;
    private DateTime _lastRotationInput = DateTime.MinValue;

        public CharacterConsoleController(CharacterService.CharacterServiceClient client, CharacterSession session)
        {
            _client = client;
            _session = session;
        }

        public async Task RunAsync()
        {
            while (_isRunning)
            {
                var keys = PollKeys();
                await ProcessKeysAsync(keys);
                UpdatePosition();

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
                await MaybeReleaseRotationAsync(now);
                return;
            }

            var handledMovement = false;
            var handledRotation = false;

            foreach (var keyInfo in keys)
            {
                switch (keyInfo.Key)
                {
                    case ConsoleKey.W:
                    case ConsoleKey.UpArrow:
                        await StartMovementAsync(1);
                        handledMovement = true;
                        _lastMovementInput = now;
                        break;
                    case ConsoleKey.S:
                    case ConsoleKey.DownArrow:
                        await StartMovementAsync(5);
                        handledMovement = true;
                        _lastMovementInput = now;
                        break;
                    case ConsoleKey.A:
                    case ConsoleKey.LeftArrow:
                        await StartMovementAsync(7);
                        handledMovement = true;
                        _lastMovementInput = now;
                        break;
                    case ConsoleKey.D:
                    case ConsoleKey.RightArrow:
                        await StartMovementAsync(3);
                        handledMovement = true;
                        _lastMovementInput = now;
                        break;
                    case ConsoleKey.Q:
                        await StartRotationAsync(7);
                        handledRotation = true;
                        _lastRotationInput = now;
                        break;
                    case ConsoleKey.E:
                        await StartRotationAsync(3);
                        handledRotation = true;
                        _lastRotationInput = now;
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

            if (!handledRotation)
            {
                await MaybeReleaseRotationAsync(now);
            }
        }

        private async Task StartMovementAsync(int direction)
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
                    _facingDirection = direction;
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

        private async Task StartRotationAsync(int direction)
        {
            if (!DirectionVectors.ContainsKey(direction))
            {
                return;
            }

            if (!_rotationActive || _rotationDirection != direction)
            {
                try
                {
                    await _client.StartRotationAsync(new MovementCommandRequest
                    {
                        CharacterId = _session.CharacterId.ToString(),
                        Direction = direction
                    });

                    _rotationActive = true;
                    _rotationDirection = direction;
                    _facingDirection = direction;
                }
                catch (Exception ex)
                {
                    LogError($"StartRotation failed: {ex.Message}");
                }
            }
        }

        private async Task ReleaseRotationAsync()
        {
            if (!_rotationActive)
            {
                return;
            }

            try
            {
                await _client.StopRotationAsync(new CharacterIdRequest
                {
                    CharacterId = _session.CharacterId.ToString()
                });

                _rotationActive = false;
            }
            catch (Exception ex)
            {
                LogError($"StopRotation failed: {ex.Message}");
            }
        }

        private async Task MaybeReleaseRotationAsync(DateTime now)
        {
            if (_rotationActive && now - _lastRotationInput > InputReleaseDelay)
            {
                await ReleaseRotationAsync();
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

        private async Task ShutdownAsync()
        {
            await ReleaseMovementAsync();
            await ReleaseRotationAsync();
        }

        private void LogError(string message)
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
            var buffer = new string[BoardHeight];
            for (var y = 0; y < BoardHeight; y++)
            {
                var line = new char[BoardWidth];
                Array.Fill(line, '.');
                buffer[y] = new string(line);
            }

            if (!DirectionVectors.TryGetValue(facingDirection, out var vector))
            {
                vector = DirectionVectors[1];
            }

            var arrow = vector.arrow;
            var sb = new StringBuilder(buffer[position.y]);
            sb[position.x] = arrow[0];
            buffer[position.y] = sb.ToString();

            return string.Join('\n', buffer);
        }

        private IRenderable Render()
        {
            var board = BuildBoard(_position, _facingDirection);
            var status = new StringBuilder()
                .AppendLine("Sterowanie:")
                .AppendLine("  W/S/A/D lub strzałki – start ruchu")
                .AppendLine("  Spacja – zatrzymaj ruch")
                .AppendLine("  Q / E – rozpocznij rotację (lewo / prawo)")
                .AppendLine("  Enter – zatrzymaj rotację")
                .AppendLine("  Esc – zakończ aplikację")
                .AppendLine()
                .AppendLine($"Postać: {_session.CharacterId}")
                .AppendLine($"Pozycja: {_position.x},{_position.y}")
                .AppendLine($"Facing: {_facingDirection}")
                .AppendLine($"Ruch: {_movementActive} (dir {_movementDirection})")
                .AppendLine($"Rotacja: {_rotationActive} (dir {_rotationDirection})");

            var boardPanel = new Panel(new Text(board))
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

    private sealed record CharacterSession(Guid CharacterId, Guid SessionId);
}
