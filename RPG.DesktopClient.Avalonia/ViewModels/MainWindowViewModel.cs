using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using RPG.DesktopClient.Avalonia.Services;
using RPG.GameServer.Protos;

namespace RPG.DesktopClient.Avalonia.ViewModels;

internal sealed class MainWindowViewModel : ViewModelBase, IAsyncDisposable
{
    private const int BoardWidthValue = 45;
    private const int BoardHeightValue = 21;
    private const int MaxMessages = 10;

    private static readonly IBrush DefaultForeground = Brushes.LightGray;
    private static readonly IBrush DefaultBackground = Brushes.Black;
    private static readonly IBrush PlayerForeground = Brushes.Orange;
    private static readonly IBrush OtherPlayerForeground = Brushes.SpringGreen;
    private static readonly string[] DirectionGlyphs =
    {
        "↑", "↗", "→", "↘", "↓", "↙", "←", "↖"
    };
    private static readonly string[] DirectionNames =
    {
        "Północ", "Północny-wschód", "Wschód", "Południowy-wschód",
        "Południe", "Południowy-zachód", "Zachód", "Północny-zachód"
    };
    private static readonly TimeSpan PendingRotationTimeout = TimeSpan.FromSeconds(3);

    private readonly GameClientService _gameClient;
    private readonly BoardCellViewModel[] _boardCells;
    private readonly Queue<MessageViewModel> _messageQueue = new();
    private readonly HashSet<Key> _movementKeys = new();
    private readonly SemaphoreSlim _movementUpdateLock = new(1, 1);

    private double _worldMinX = double.MaxValue;
    private double _worldMaxX = double.MinValue;
    private double _worldMinY = double.MaxValue;
    private double _worldMaxY = double.MinValue;
    private bool _hasWorldBounds;

    private string _playerName = "-";
    private string _sessionId = "-";
    private string _worldName = "-";
    private string _playerPosition = "-";
    private string _worldBounds = "brak danych";
    private string _lastSnapshot = "-";
    private int _worldPopulation;
    private int _npcCount;
    private int _mapObjectCount;
    private int? _currentMovementDirection;
    private int _facingDirection = 1;
    private float _lastServerRotationDegrees = float.NaN;
    private bool _hasPendingRotation;
    private int _pendingFacingDirection = 1;
    private DateTimeOffset _lastRotationCommandAt = DateTimeOffset.MinValue;

    public MainWindowViewModel(GameClientService gameClient)
    {
        _gameClient = gameClient;
        BoardCells = new ObservableCollection<BoardCellViewModel>();
        _boardCells = new BoardCellViewModel[BoardWidthValue * BoardHeightValue];
        _lastServerRotationDegrees = DegreesFromDirection(_facingDirection);

        for (var i = 0; i < _boardCells.Length; i++)
        {
            var cell = new BoardCellViewModel
            {
                Glyph = string.Empty,
                Foreground = DefaultForeground,
                Background = DefaultBackground
            };
            _boardCells[i] = cell;
            BoardCells.Add(cell);
        }
    }

    public ObservableCollection<BoardCellViewModel> BoardCells { get; }
    public ObservableCollection<EntityInfoViewModel> NearbyNpcs { get; } = new();
    public ObservableCollection<EntityInfoViewModel> MapObjects { get; } = new();
    public ObservableCollection<MessageViewModel> Messages { get; } = new();
    public ObservableCollection<EntityInfoViewModel> WorldEntities { get; } = new();

    public int BoardWidth => BoardWidthValue;
    public int BoardHeight => BoardHeightValue;

    public string PlayerName
    {
        get => _playerName;
        private set => SetProperty(ref _playerName, value);
    }

    public string SessionId
    {
        get => _sessionId;
        private set => SetProperty(ref _sessionId, value);
    }

    public string WorldName
    {
        get => _worldName;
        private set => SetProperty(ref _worldName, value);
    }

    public string PlayerPosition
    {
        get => _playerPosition;
        private set => SetProperty(ref _playerPosition, value);
    }

    public string WorldBounds
    {
        get => _worldBounds;
        private set => SetProperty(ref _worldBounds, value);
    }

    public string LastSnapshot
    {
        get => _lastSnapshot;
        private set => SetProperty(ref _lastSnapshot, value);
    }

    public int WorldPopulation
    {
        get => _worldPopulation;
        private set => SetProperty(ref _worldPopulation, value);
    }

    public int NpcCount
    {
        get => _npcCount;
        private set => SetProperty(ref _npcCount, value);
    }

    public int MapObjectCount
    {
        get => _mapObjectCount;
        private set => SetProperty(ref _mapObjectCount, value);
    }

    public async Task InitializeAsync()
    {
        _gameClient.SnapshotReceived += OnSnapshotReceived;
        _gameClient.MessageReceived += OnMessageReceived;

        if (_gameClient.Session != null)
        {
            PlayerName = _gameClient.Session.PlayerName;
            SessionId = _gameClient.Session.SessionId.ToString();
        }

        await _gameClient.InitializeAsync().ConfigureAwait(false);

        if (_gameClient.Session != null)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                PlayerName = _gameClient.Session!.PlayerName;
                SessionId = _gameClient.Session!.SessionId.ToString();
            });
        }
    }

    public async ValueTask DisposeAsync()
    {
        await ResetMovementAsync().ConfigureAwait(false);
        _gameClient.SnapshotReceived -= OnSnapshotReceived;
        _gameClient.MessageReceived -= OnMessageReceived;
        await _gameClient.DisposeAsync().ConfigureAwait(false);
        _movementUpdateLock.Dispose();
    }

    public async Task<bool> HandleKeyDownAsync(Key key)
    {
        switch (key)
        {
            case Key.Q:
                await RotateAsync(-1).ConfigureAwait(false);
                return true;
            case Key.E:
                await RotateAsync(1).ConfigureAwait(false);
                return true;
            case Key.Space:
                await ResetMovementAsync().ConfigureAwait(false);
                return true;
        }

        if (IsMovementKey(key))
        {
            if (!_movementKeys.Add(key))
            {
                return true;
            }

            await UpdateMovementFromKeysAsync().ConfigureAwait(false);
            return true;
        }

        return false;
    }

    public async Task<bool> HandleKeyUpAsync(Key key)
    {
        if (IsMovementKey(key))
        {
            if (_movementKeys.Remove(key))
            {
                await UpdateMovementFromKeysAsync().ConfigureAwait(false);
            }
            return true;
        }

        if (key == Key.Space)
        {
            await ResetMovementAsync().ConfigureAwait(false);
            return true;
        }

        return false;
    }

    public async Task ResetMovementAsync()
    {
        await _movementUpdateLock.WaitAsync().ConfigureAwait(false);
        try
        {
            _movementKeys.Clear();
            _currentMovementDirection = null;
        }
        finally
        {
            _movementUpdateLock.Release();
        }

        await _gameClient.StopMovementAsync().ConfigureAwait(false);
    }

    public void ReportExternalMessage(string message)
    {
        AddMessage(message);
    }

    private static bool IsMovementKey(Key key)
    {
        return key is Key.W or Key.A or Key.S or Key.D
            or Key.Up or Key.Down or Key.Left or Key.Right
            or Key.NumPad8 or Key.NumPad2 or Key.NumPad4 or Key.NumPad6
            or Key.NumPad7 or Key.NumPad9 or Key.NumPad1 or Key.NumPad3;
    }

    private static bool ContainsAny(HashSet<Key> keys, params Key[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (keys.Contains(candidate))
            {
                return true;
            }
        }

        return false;
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

        var index = (int)MathF.Round(normalized / 45f) % 8;
        return index + 1;
    }

    private static float DegreesFromDirection(int direction)
    {
        var index = NormalizeDirection(direction) - 1;
        return index * 45f;
    }

    private static (double x, double y) ProjectLocation(Location location)
    {
        if (location == null)
        {
            return (0d, 0d);
        }

        var x = location.X;
        var absY = Math.Abs(location.Y);
        var absZ = Math.Abs(location.Z);
        double y;

        if (absZ > absY + 0.0001d)
        {
            y = location.Z;
        }
        else if (absY > 0.0001d)
        {
            y = location.Y;
        }
        else
        {
            y = location.Z;
        }

        return (x, y);
    }

    private static string FormatCoordinates(Location location)
    {
        var (x, y) = ProjectLocation(location);
        return FormattableString.Invariant($"{x:0.0}, {y:0.0}");
    }

    private async Task RotateAsync(int step)
    {
        if (step == 0)
        {
            return;
        }

        var rotated = await _gameClient.RotateAsync(step).ConfigureAwait(false);
        if (!rotated)
        {
            return;
        }

        _facingDirection = OffsetDirection(_facingDirection, step);
        _pendingFacingDirection = _facingDirection;
        _hasPendingRotation = true;
        _lastRotationCommandAt = DateTimeOffset.UtcNow;
        _lastServerRotationDegrees = DegreesFromDirection(_facingDirection);

        if (_movementKeys.Count > 0)
        {
            await UpdateMovementFromKeysAsync().ConfigureAwait(false);
        }

        await Dispatcher.UIThread.InvokeAsync(UpdatePlayerMarker);
    }

    private void UpdateFacingFromServerRotation(float rotation)
    {
        var serverDirection = DirectionFromRotation(rotation);

        if (_hasPendingRotation)
        {
            var timedOut = _lastRotationCommandAt != DateTimeOffset.MinValue
                           && DateTimeOffset.UtcNow - _lastRotationCommandAt > PendingRotationTimeout;

            if (serverDirection == _pendingFacingDirection)
            {
                _hasPendingRotation = false;
            }
            else if (!timedOut)
            {
                // Keep local facing until the server confirms the rotation or timeout occurs.
                return;
            }
            else
            {
                _hasPendingRotation = false;
            }
        }

        if (_facingDirection != serverDirection)
        {
            _facingDirection = serverDirection;
        }

        _pendingFacingDirection = _facingDirection;
        _lastServerRotationDegrees = rotation;
    }

    private bool TryResolveMovementDirection(out int direction)
    {
        var forward = ContainsAny(_movementKeys, Key.W, Key.Up, Key.NumPad8, Key.NumPad9, Key.NumPad7);
        var backward = ContainsAny(_movementKeys, Key.S, Key.Down, Key.NumPad2, Key.NumPad1, Key.NumPad3);
        var strafeLeft = ContainsAny(_movementKeys, Key.A, Key.Left, Key.NumPad4, Key.NumPad7, Key.NumPad1);
        var strafeRight = ContainsAny(_movementKeys, Key.D, Key.Right, Key.NumPad6, Key.NumPad9, Key.NumPad3);

        if (forward && backward)
        {
            forward = backward = false;
        }

        if (strafeLeft && strafeRight)
        {
            strafeLeft = strafeRight = false;
        }

        int offset;

        if (forward)
        {
            if (strafeRight)
            {
                offset = 1;
            }
            else if (strafeLeft)
            {
                offset = -1;
            }
            else
            {
                offset = 0;
            }
        }
        else if (backward)
        {
            if (strafeRight)
            {
                offset = 3;
            }
            else if (strafeLeft)
            {
                offset = -3;
            }
            else
            {
                offset = 4;
            }
        }
        else if (strafeRight)
        {
            offset = 2;
        }
        else if (strafeLeft)
        {
            offset = -2;
        }
        else
        {
            direction = default;
            return false;
        }

        direction = OffsetDirection(_facingDirection, offset);
        return true;
    }

    private static string GetDirectionGlyph(int direction)
    {
        var index = NormalizeDirection(direction) - 1;
        return DirectionGlyphs[index];
    }

    private static string GetDirectionName(int direction)
    {
        var index = NormalizeDirection(direction) - 1;
        return DirectionNames[index];
    }

    private void UpdatePlayerMarker()
    {
        var location = _gameClient.LastWorldLocation;
        if (location == null)
        {
            return;
        }

        var board = MapWorldToBoard(location);
        var index = ToIndex(board.x, board.y);
        var cell = _boardCells[index];
        var glyph = GetDirectionGlyph(_facingDirection);
        cell.Glyph = glyph;
        cell.Foreground = PlayerForeground;
        cell.Background = Brushes.Transparent;

        var directionName = GetDirectionName(_facingDirection);
        var rotation = float.IsNaN(_lastServerRotationDegrees)
            ? DegreesFromDirection(_facingDirection)
            : _lastServerRotationDegrees;
        var coordsText = FormatCoordinates(location);
        PlayerPosition = coordsText;
        cell.Tooltip = FormattableString.Invariant(
            $"Gracz @ {coordsText}\nKierunek: {directionName} ({glyph}), {rotation:0}°");
    }

    private async Task UpdateMovementFromKeysAsync()
    {
        await _movementUpdateLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!TryResolveMovementDirection(out var direction))
            {
                if (_currentMovementDirection.HasValue)
                {
                    await _gameClient.StopMovementAsync().ConfigureAwait(false);
                    _currentMovementDirection = null;
                }

                return;
            }

            if (_currentMovementDirection == direction)
            {
                return;
            }

            var started = await _gameClient.StartMovementAsync(direction).ConfigureAwait(false);
            if (started)
            {
                _currentMovementDirection = direction;
            }
        }
        finally
        {
            _movementUpdateLock.Release();
        }
    }

    private void OnMessageReceived(string message)
    {
        Dispatcher.UIThread.Post(() => AddMessage(message));
    }

    private void OnSnapshotReceived(WorldSnapshot snapshot)
    {
        Dispatcher.UIThread.Post(() => ApplySnapshot(snapshot));
    }

    private void ApplySnapshot(WorldSnapshot snapshot)
    {
        ResetBoard();

        var npcCountRaw = snapshot.Npcs?.Count ?? 0;
        var mapCountRaw = snapshot.MapObjects?.Count ?? 0;
        Console.WriteLine(FormattableString.Invariant($"[ViewModel] ApplySnapshot start: npc={npcCountRaw}, mapObjects={mapCountRaw}"));

        if (!string.IsNullOrWhiteSpace(snapshot.Metadata?.WorldName))
        {
            WorldName = snapshot.Metadata.WorldName;
        }

        if (snapshot.LastUpdated > 0)
        {
            var last = DateTimeOffset.FromUnixTimeMilliseconds(snapshot.LastUpdated).ToLocalTime();
            LastSnapshot = last.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture);
        }

        var mapObjects = snapshot.MapObjects?
            .Where(o => o != null && o.Location != null)
            .ToList() ?? new List<WorldMapObject>();

        var npcs = snapshot.Npcs?
            .Where(n => n != null && n.Location != null)
            .ToList() ?? new List<WorldNpc>();

        var aggregatedEntities = new List<EntityInfoViewModel>();
        var npcInfos = new List<EntityInfoViewModel>();
        var mapInfos = new List<EntityInfoViewModel>();
        var otherPlayerInfos = new List<EntityInfoViewModel>();
        EntityInfoViewModel? selfEntity = null;

        foreach (var mapObject in mapObjects)
        {
            UpdateWorldBounds(mapObject.Location);
        }

        foreach (var npc in npcs)
        {
            UpdateWorldBounds(npc.Location);
        }

        var otherCharacterCells = new Dictionary<(int x, int y), List<string>>();

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

                if (character.Location != null)
                {
                    UpdateWorldBounds(character.Location);
                }

                if (_gameClient.Session != null && characterId == _gameClient.Session.CharacterId)
                {
                    var pos = character.Location;
                    if (pos != null)
                    {
                        PlayerPosition = FormatCoordinates(pos);
                        UpdateFacingFromServerRotation(pos.Rotation);
                        selfEntity = CreatePlayerInfo(character);
                    }

                    continue;
                }

                if (character.Location == null)
                {
                    continue;
                }

                var board = MapWorldToBoard(character.Location);
                var displayName = string.IsNullOrWhiteSpace(character.DisplayName)
                    ? character.CharacterId
                    : character.DisplayName;

                if (!otherCharacterCells.TryGetValue(board, out var list))
                {
                    list = new List<string>();
                    otherCharacterCells[board] = list;
                }

                list.Add(displayName);
                otherPlayerInfos.Add(CreateOtherPlayerInfo(character));
            }

            WorldPopulation = population;
        }
        else
        {
            WorldPopulation = 0;
        }

        var npcLookup = GroupNpcSymbols(npcs);
        var mapLookup = GroupMapObjectSymbols(mapObjects);

        foreach (var kvp in mapLookup)
        {
            var (x, y) = kvp.Key;
            var state = kvp.Value;
            var index = ToIndex(x, y);
            var cell = _boardCells[index];
            cell.Glyph = state.Symbol.Glyph;
            cell.Foreground = state.Symbol.Foreground;
            cell.Background = state.Symbol.Background;
            cell.Tooltip = string.Join("\n", state.Tooltips);
        }

        foreach (var kvp in npcLookup)
        {
            var (x, y) = kvp.Key;
            var state = kvp.Value;
            var index = ToIndex(x, y);
            var cell = _boardCells[index];
            cell.Glyph = state.Symbol.Glyph;
            cell.Foreground = state.Symbol.Foreground;
            cell.Background = state.Symbol.Background;
            cell.Tooltip = string.Join("\n", state.Tooltips);
        }

        foreach (var kvp in otherCharacterCells)
        {
            var (x, y) = kvp.Key;
            var names = kvp.Value;
            var index = ToIndex(x, y);
            var cell = _boardCells[index];
            cell.Glyph = names.Count > 1 ? "+" : "P";
            cell.Foreground = OtherPlayerForeground;
            cell.Background = Brushes.Transparent;
            cell.Tooltip = string.Join("\n", names);
        }

        foreach (var npc in npcs)
        {
            npcInfos.Add(CreateNpcInfo(npc));
        }

        foreach (var mapObject in mapObjects)
        {
            mapInfos.Add(CreateMapObjectInfo(mapObject));
        }

    Console.WriteLine(FormattableString.Invariant($"[ViewModel] ApplySnapshot processed: npcInfos={npcInfos.Count}, mapInfos={mapInfos.Count}, worldEntities={aggregatedEntities.Count}"));

        if (selfEntity != null)
        {
            aggregatedEntities.Add(selfEntity);
        }

        var nameComparer = StringComparer.CurrentCultureIgnoreCase;
        aggregatedEntities.AddRange(otherPlayerInfos.OrderBy(e => e.Name, nameComparer));
        aggregatedEntities.AddRange(npcInfos.OrderBy(e => e.Name, nameComparer));
        aggregatedEntities.AddRange(mapInfos.OrderBy(e => e.Name, nameComparer));

        UpdatePlayerMarker();

        UpdateCollections(npcInfos, mapInfos);
        UpdateWorldEntities(aggregatedEntities);
        UpdateSummary(npcInfos.Count, mapInfos.Count);
        Console.WriteLine(FormattableString.Invariant($"[ViewModel] ApplySnapshot completed: NearbyNpcs={NearbyNpcs.Count}, MapObjects={MapObjects.Count}, WorldEntities={WorldEntities.Count}"));
    }

    private void UpdateSummary(int npcCount, int mapObjectCount)
    {
        NpcCount = npcCount;
        MapObjectCount = mapObjectCount;

        if (_hasWorldBounds)
        {
            WorldBounds = FormattableString.Invariant($"X {_worldMinX:0.0} – {_worldMaxX:0.0}, Z {_worldMinY:0.0} – {_worldMaxY:0.0}");
        }
        else
        {
            WorldBounds = "brak danych";
        }
    }

    private void UpdateCollections(IReadOnlyCollection<EntityInfoViewModel> npcInfos, IReadOnlyCollection<EntityInfoViewModel> mapInfos)
    {
        var comparer = StringComparer.CurrentCultureIgnoreCase;

        NearbyNpcs.Clear();
        foreach (var info in npcInfos.OrderBy(n => n.Name, comparer).Take(20))
        {
            NearbyNpcs.Add(info);
        }

        MapObjects.Clear();
        foreach (var info in mapInfos.OrderBy(o => o.Name, comparer).Take(20))
        {
            MapObjects.Add(info);
        }
    }

    private void UpdateWorldEntities(IEnumerable<EntityInfoViewModel> entities)
    {
        WorldEntities.Clear();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entity in entities)
        {
            var key = FormattableString.Invariant($"{entity.Name}|{entity.Position}|{entity.Details}");
            if (seen.Add(key))
            {
                WorldEntities.Add(entity);
            }
        }
    }

    private static string BuildNpcDetails(WorldNpc npc)
    {
        var tags = npc.Tags?.Where(t => !string.IsNullOrWhiteSpace(t)).ToList() ?? new List<string>();
        var tagText = tags.Count > 0 ? string.Join(", ", tags) : "brak";
        var status = npc.IsAlive ? "żywy" : "nieaktywny";
        return FormattableString.Invariant($"Stan: {status} | Tagi: {tagText}");
    }

    private static string BuildMapObjectDetails(WorldMapObject mapObject)
    {
        var tags = mapObject.Tags?.Where(t => !string.IsNullOrWhiteSpace(t)).ToList() ?? new List<string>();
        var tagText = tags.Count > 0 ? string.Join(", ", tags) : "brak";
        var statePreview = mapObject.State.Take(3).Select(kvp => $"{kvp.Key}={kvp.Value}");
        var stateText = statePreview.Any() ? string.Join("; ", statePreview) : "brak";
        return FormattableString.Invariant($"Tagi: {tagText} | Stan: {stateText}");
    }

    private static string BuildCharacterDetails(WorldCharacter character)
    {
        var effects = character.StatusEffects?.Where(e => !string.IsNullOrWhiteSpace(e)).ToList() ?? new List<string>();
        var effectsText = effects.Count > 0 ? string.Join(", ", effects) : "brak";
        var status = character.IsInCombat ? "walczy" : "spokojny";
        var rotation = character.Location?.Rotation ?? 0f;
        return FormattableString.Invariant($"Stan: {status} | Efekty: {effectsText} | Rotacja: {rotation:0}°");
    }

    private static EntityInfoViewModel CreatePlayerInfo(WorldCharacter character)
    {
        var baseName = string.IsNullOrWhiteSpace(character.DisplayName) ? character.CharacterId : character.DisplayName;
        var label = string.IsNullOrWhiteSpace(baseName) ? "Ty" : FormattableString.Invariant($"{baseName} (Ty)");
        var position = character.Location != null
            ? FormatCoordinates(character.Location)
            : "-";

        return new EntityInfoViewModel
        {
            Name = label,
            Position = position,
            Details = BuildCharacterDetails(character)
        };
    }

    private static EntityInfoViewModel CreateOtherPlayerInfo(WorldCharacter character)
    {
        var label = string.IsNullOrWhiteSpace(character.DisplayName) ? character.CharacterId : character.DisplayName;
        if (string.IsNullOrWhiteSpace(label))
        {
            label = "Gracz";
        }

        var position = character.Location != null
            ? FormatCoordinates(character.Location)
            : "-";

        return new EntityInfoViewModel
        {
            Name = label,
            Position = position,
            Details = BuildCharacterDetails(character)
        };
    }

    private static EntityInfoViewModel CreateNpcInfo(WorldNpc npc)
    {
        var name = string.IsNullOrWhiteSpace(npc.Name) ? npc.NpcId : npc.Name;
        var position = npc.Location != null
            ? FormatCoordinates(npc.Location)
            : "-";

        return new EntityInfoViewModel
        {
            Name = name,
            Position = position,
            Details = BuildNpcDetails(npc)
        };
    }

    private static EntityInfoViewModel CreateMapObjectInfo(WorldMapObject mapObject)
    {
        var name = string.IsNullOrWhiteSpace(mapObject.DisplayName) ? mapObject.Name : mapObject.DisplayName;
        if (string.IsNullOrWhiteSpace(name))
        {
            name = mapObject.MapObjectId;
        }

        var position = mapObject.Location != null
            ? FormatCoordinates(mapObject.Location)
            : "-";

        return new EntityInfoViewModel
        {
            Name = name,
            Position = position,
            Details = BuildMapObjectDetails(mapObject)
        };
    }

    private static bool HasTag(IEnumerable<string>? tags, string value)
    {
        if (tags is null)
        {
            return false;
        }

        return tags.Any(tag => !string.IsNullOrWhiteSpace(tag) && tag.Contains(value, StringComparison.OrdinalIgnoreCase));
    }

    private static BoardSymbol ResolveNpcSymbol(WorldNpc npc)
    {
        if (HasTag(npc.Tags, "enemy") || HasTag(npc.Tags, "hostile"))
        {
            return new BoardSymbol("!", Brushes.Red, Brushes.Transparent);
        }

        if (HasTag(npc.Tags, "merchant"))
        {
            return new BoardSymbol("$", Brushes.Gold, Brushes.Transparent);
        }

        if (HasTag(npc.Tags, "trainer"))
        {
            return new BoardSymbol("T", Brushes.MediumSpringGreen, Brushes.Transparent);
        }

        if (HasTag(npc.Tags, "friendly") || HasTag(npc.Tags, "quest"))
        {
            return new BoardSymbol("F", Brushes.DeepSkyBlue, Brushes.Transparent);
        }

        return new BoardSymbol("N", Brushes.Silver, Brushes.Transparent);
    }

    private static BoardSymbol ResolveMapObjectSymbol(WorldMapObject mapObject)
    {
        if (HasTag(mapObject.Tags, "spawn"))
        {
            return new BoardSymbol("S", Brushes.Gold, Brushes.DarkGoldenrod);
        }

        if (HasTag(mapObject.Tags, "market") || HasTag(mapObject.Tags, "merchant"))
        {
            return new BoardSymbol("M", Brushes.Orange, Brushes.SaddleBrown);
        }

        if (HasTag(mapObject.Tags, "building") || HasTag(mapObject.Tags, "structure") || HasTag(mapObject.Tags, "hub"))
        {
            return new BoardSymbol("H", Brushes.Goldenrod, Brushes.DimGray);
        }

        if (HasTag(mapObject.Tags, "training") || HasTag(mapObject.Tags, "arena"))
        {
            return new BoardSymbol("X", Brushes.Plum, Brushes.MidnightBlue);
        }

        if (HasTag(mapObject.Tags, "chest") || HasTag(mapObject.Tags, "interactive"))
        {
            return new BoardSymbol("C", Brushes.LightGoldenrodYellow, Brushes.DarkSlateGray);
        }

        if (HasTag(mapObject.Tags, "water") || HasTag(mapObject.Tags, "dock"))
        {
            return new BoardSymbol("~", Brushes.Teal, Brushes.DarkSlateBlue);
        }

        if (HasTag(mapObject.Tags, "path") || HasTag(mapObject.Tags, "cobblestone"))
        {
            return new BoardSymbol("=", Brushes.Gainsboro, Brushes.DimGray);
        }

        if (HasTag(mapObject.Tags, "tree") || HasTag(mapObject.Tags, "forest") || HasTag(mapObject.Tags, "shrine") || HasTag(mapObject.Tags, "lore"))
        {
            return new BoardSymbol("^", Brushes.Green, Brushes.DarkOliveGreen);
        }

        return new BoardSymbol("#", Brushes.Silver, Brushes.Transparent);
    }

    private Dictionary<(int x, int y), BoardGroup> GroupNpcSymbols(IEnumerable<WorldNpc> npcs)
    {
        var result = new Dictionary<(int x, int y), BoardGroup>();

        foreach (var npc in npcs)
        {
            if (npc.Location == null)
            {
                continue;
            }

            if (!Guid.TryParse(npc.NpcId, out _))
            {
                continue;
            }

            var board = MapWorldToBoard(npc.Location);
            var symbol = ResolveNpcSymbol(npc);
            var label = string.IsNullOrWhiteSpace(npc.Name) ? npc.NpcId : npc.Name;
            var tooltip = FormattableString.Invariant($"NPC: {label} ({FormatCoordinates(npc.Location)})");

            if (!result.TryGetValue(board, out var group))
            {
                group = new BoardGroup(symbol, "&");
                result[board] = group;
            }

            group.Add(tooltip);
        }

        return result;
    }

    private Dictionary<(int x, int y), BoardGroup> GroupMapObjectSymbols(IEnumerable<WorldMapObject> mapObjects)
    {
        var result = new Dictionary<(int x, int y), BoardGroup>();

        foreach (var mapObject in mapObjects)
        {
            if (mapObject.Location == null)
            {
                continue;
            }

            if (!Guid.TryParse(mapObject.MapObjectId, out _))
            {
                continue;
            }

            var board = MapWorldToBoard(mapObject.Location);
            var symbol = ResolveMapObjectSymbol(mapObject);
            var label = string.IsNullOrWhiteSpace(mapObject.DisplayName) ? mapObject.Name : mapObject.DisplayName;
            var tooltip = FormattableString.Invariant($"Obiekt: {label} ({FormatCoordinates(mapObject.Location)})");

            if (!result.TryGetValue(board, out var group))
            {
                group = new BoardGroup(symbol, "#");
                result[board] = group;
            }

            group.Add(tooltip);
        }

        return result;
    }

    private void ResetBoard()
    {
        foreach (var cell in _boardCells)
        {
            cell.Glyph = string.Empty;
            cell.Foreground = DefaultForeground;
            cell.Background = DefaultBackground;
            cell.Tooltip = null;
        }
    }

    private void UpdateWorldBounds(Location location)
    {
        var (x, y) = ProjectLocation(location);

        if (!_hasWorldBounds)
        {
            _worldMinX = _worldMaxX = x;
            _worldMinY = _worldMaxY = y;
            _hasWorldBounds = true;
            return;
        }

        if (x < _worldMinX)
        {
            _worldMinX = x;
        }

        if (x > _worldMaxX)
        {
            _worldMaxX = x;
        }

        if (y < _worldMinY)
        {
            _worldMinY = y;
        }

        if (y > _worldMaxY)
        {
            _worldMaxY = y;
        }
    }

    private (int x, int y) MapWorldToBoard(Location location)
    {
        var (worldX, worldY) = ProjectLocation(location);

        var minX = _hasWorldBounds ? _worldMinX : worldX;
        var maxX = _hasWorldBounds ? _worldMaxX : worldX;
        var minY = _hasWorldBounds ? _worldMinY : worldY;
        var maxY = _hasWorldBounds ? _worldMaxY : worldY;

        var width = Math.Max(1d, maxX - minX);
        var height = Math.Max(1d, maxY - minY);

        var normalizedX = (worldX - minX) / width;
        var normalizedY = (worldY - minY) / height;

        var boardX = (int)Math.Round(normalizedX * (BoardWidthValue - 1), MidpointRounding.AwayFromZero);
        var boardY = (int)Math.Round(normalizedY * (BoardHeightValue - 1), MidpointRounding.AwayFromZero);

        var clampedX = Math.Clamp(boardX, 0, BoardWidthValue - 1);
        var clampedY = Math.Clamp(boardY, 0, BoardHeightValue - 1);

        return (clampedX, clampedY);
    }

    private static int ToIndex(int x, int y) => y * BoardWidthValue + x;

    private void AddMessage(string message)
    {
        var entry = new MessageViewModel { Text = message };
        _messageQueue.Enqueue(entry);
        Messages.Add(entry);

        while (_messageQueue.Count > MaxMessages)
        {
            var removed = _messageQueue.Dequeue();
            Messages.Remove(removed);
        }
    }

    private sealed record BoardSymbol(string Glyph, IBrush Foreground, IBrush Background);

    private sealed class BoardGroup
    {
        public BoardGroup(BoardSymbol symbol, string multiGlyph)
        {
            Symbol = symbol;
            MultiGlyph = multiGlyph;
        }

        public BoardSymbol Symbol { get; private set; }
        public string MultiGlyph { get; }
        public List<string> Tooltips { get; } = new();

        public void Add(string tooltip)
        {
            Tooltips.Add(tooltip);

            if (Tooltips.Count > 1)
            {
                Symbol = new BoardSymbol(MultiGlyph, Symbol.Foreground, Symbol.Background);
            }
        }
    }
}
