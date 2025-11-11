using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using RPG.Client.Stride.Rendering;
using RPG.Client.Stride.Services;
using RPG.GameServer.Protos;

namespace RPG.Client.Stride;

internal static class Program
{
    static Program()
    {
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
    }

    [STAThread]
    private static void Main()
    {
        using var game = new GraphicsClientGame();
        game.Run();
    }
}

internal sealed class GraphicsClientGame : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private IConfigurationRoot _configuration = default!;
    private GrpcGameClient? _client;
    private Task? _initializationTask;

    private BasicEffect? _effect;
    private VertexPositionColor[]? _groundVertices;
    private VertexPositionTexture[]? _groundQuadVertices;
    private short[]? _groundQuadIndices;
    private Texture2D? _groundTexture;
    private Texture2D? _playerTexture;
    private Texture2D? _npcTexture;
    private Texture2D? _mapObjectTexture;
    private Texture2D? _shadowTexture;
    private readonly Dictionary<string, Texture2D> _customTextures = new(StringComparer.OrdinalIgnoreCase);

    private SpriteBatch? _spriteBatch;
    private Desktop? _hudDesktop;
    private Label? _statusLabel;
    private Label? _movementLabel;
    private Label? _snapshotLabel;
    private Label? _logLabel;

    private readonly Queue<Action> _uiActions = new();
    private readonly object _uiActionsLock = new();
    private readonly Queue<string> _logLines = new();
    private const int MaxLogLines = 6;

    private readonly object _snapshotLock = new();
    private WorldSnapshot? _pendingSnapshot;

    private readonly List<RenderEntity> _npcEntities = new();
    private readonly List<MapObjectVisualization> _mapObjectVisualizations = new();
    private readonly Dictionary<string, MapObjectOverride> _mapObjectOverrides = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<RenderEntity> _otherPlayerEntities = new();

    private RenderEntity _playerEntity;
    private bool _hasPlayerEntity;

    private Vector3 _serverPlayerPosition;
    private Vector3 _predictedPlayerPosition;
    private Vector3 _predictedVelocity;
    private bool _hasPlayerPosition;
    private float _playerRotationDegrees;
    private float _targetRotationDegrees;
    private float _cameraYawDegrees;
    private Vector3 _cameraForward = new(0f, 0f, 1f);
    private Vector3 _cameraRight = new(1f, 0f, 0f);

    private int _facingDirection = 1;
    private int? _activeMovementDirection;
    private bool _activePreserveFacing;
    private float _movementSpeed = 5f;
    private string _movementStatus = "idle";

    private Task? _movementLoopTask;
    private CancellationTokenSource? _movementLoopCts;
    private static readonly TimeSpan MovementCommandInterval = TimeSpan.FromMilliseconds(100);
    private const float RotationDegreesPerSecond = 270f;
    private const float CharacterScaleMultiplier = 1.5f;
    private const float PlayerBillboardBaseSize = 2.1f;
    private const float OtherPlayerBillboardBaseSize = 2.0f;
    private const float NpcAliveBillboardBaseSize = 1.8f;
    private const float NpcInactiveBillboardBaseSize = 1.5f;
    private const float HouseMapObjectScaleMultiplier = 12f;
    private const float DefaultMapObjectScaleMultiplier = 6f;
    private static readonly Dictionary<string, float> MapObjectScaleMultipliers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["default"] = DefaultMapObjectScaleMultiplier,
        ["house"] = HouseMapObjectScaleMultiplier,
        ["house_alt"] = HouseMapObjectScaleMultiplier,
        ["house_alt2"] = HouseMapObjectScaleMultiplier,
        ["tree"] = 8f,
        ["tree_alt"] = 8f,
        ["campfire"] = 5f,
        ["spawn_beacon"] = 6f,
        ["stone"] = 5f,
        ["mountain"] = 12f,
        ["chest"] = 4f
    };
    private static readonly string[] DefaultMapObjectTextureKeys = { "default" };
    private static readonly string[] CampfireTextureKeys = { "campfire", "default" };
    private static readonly string[] SpawnBeaconTextureKeys = { "spawn_beacon", "default" };
    private static readonly string[] TreeTextureKeys = { "tree", "tree_alt", "default" };
    private static readonly string[] HouseTextureKeys = { "house", "house_alt", "house_alt2", "default" };
    private static readonly string[] StoneTextureKeys = { "stone", "mountain", "default" };
    private static readonly string[] MountainTextureKeys = { "mountain", "stone", "default" };
    private static readonly string[] ShrineTextureKeys = { "spawn_beacon", "campfire", "default" };
    private static readonly string[] ChestTextureKeys = { "chest", "default" };

    private readonly struct MapObjectVisual
    {
        public MapObjectVisual(string key, Texture2D? texture)
        {
            Key = key;
            Texture = texture;
        }

        public string Key { get; }
        public Texture2D? Texture { get; }
    }

    private DateTime _lastSnapshotUtc = DateTime.MinValue;
    private bool _connectionErrorDisplayed;

    private const float BillboardScaleFactor = 320f;
    private const float ShadowScaleFactor = 280f;
    private const float GroundPlaneY = -0.02f;
    private const float GroundHalfSize = 40f;
    private const float GroundTextureRepeat = GroundHalfSize / 2f;

    private bool _editorModeEnabled;
    private bool _editToggleKeyDown;
    private bool _addObjectKeyDown;
    private MouseState _previousMouseState;
    private Matrix _lastViewMatrix;
    private Matrix _lastProjectionMatrix;
    private Vector3 _lastCameraPosition;
    private bool _hasCameraMatrices;
    private MapObjectVisualization? _selectedMapObject;
    private EditorPanel? _editorPanel;
    private SeedDataPersistence? _seedDataPersistence;
    private string? _currentWorldId;
    private string? _currentZoneId;
    private string? _currentMapId;
    private PendingMapObjectCreation? _pendingMapObjectCreation;

    public GraphicsClientGame()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1600,
            PreferredBackBufferHeight = 900,
            SynchronizeWithVerticalRetrace = true
        };

        IsMouseVisible = true;
        Window.Title = "DevHobby RPG - MonoGame Client";
    }

    protected override void Initialize()
    {
        base.Initialize();

        _configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

    BuildGroundGrid();
    BuildGroundPlane();
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        BuildHud();
    }

    protected override void LoadContent()
    {
        _effect = new BasicEffect(GraphicsDevice)
        {
            LightingEnabled = false,
            TextureEnabled = true,
            VertexColorEnabled = true
        };

    _groundTexture = LoadTextureOrFallback("Ground", CreateFallbackGroundTexture, "Assets", "Textures", "grass-tile.png");
        _playerTexture = LoadTextureOrFallback("Player", CreateFallbackPlayerTexture, "Assets", "Textures", "player.png");
        _npcTexture = LoadTextureOrFallback("NPC", CreateFallbackNpcTexture, "Assets", "Textures", "npc.png");
    _mapObjectTexture = CreateFallbackMapObjectTexture();
        _shadowTexture = CreateShadowTexture(96);

        RegisterCustomTexture("campfire", "Assets", "Textures", "campfire.png");
        RegisterCustomTexture("mountain", "Assets", "Textures", "mountain.png");
        RegisterCustomTexture("stone", "Assets", "Textures", "stone.png");
        RegisterCustomTexture("spawn_beacon", "Assets", "Textures", "spawn_beacon.png");
        RegisterCustomTexture("chest", "Assets", "Textures", "chest.png");
        RegisterCustomTexture("tree", "Assets", "Textures", "tree1.png");
        RegisterCustomTexture("tree_alt", "Assets", "Textures", "tree2.png");
        RegisterCustomTexture("house", "Assets", "Textures", "hause1.png");
        RegisterCustomTexture("house_alt", "Assets", "Textures", "hause2.png");
        RegisterCustomTexture("house_alt2", "Assets", "Textures", "hause3.png");
        RegisterCustomTexture("npc_guide", "Assets", "Textures", "npc_guide.png");
        RegisterCustomTexture("npc_guard", "Assets", "Textures", "npc_guard.png");
        RegisterCustomTexture("npc_merchant", "Assets", "Textures", "npc_merchant.png");
        RegisterCustomTexture("elf", "Assets", "Textures", "elf.png");
        RegisterCustomTexture("dwarf", "Assets", "Textures", "dwarf.png");
    }

    protected override void Update(GameTime gameTime)
    {
    ProcessUiActions();
    EnsureClientInitialization();
    ProcessPendingSnapshot();
    UpdateMovementPrediction(gameTime);

    var deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
    var keyboardState = Keyboard.GetState();
    HandleRotationInput(keyboardState, deltaSeconds);
    HandleMovementInput(keyboardState);
    HandleEditorInput(gameTime, keyboardState);
    UpdateRotationSmoothing(deltaSeconds);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(8, 10, 18));

        if (_effect == null || _spriteBatch == null)
        {
            base.Draw(gameTime);
            _hudDesktop?.Render();
            return;
        }

        var target = _hasPlayerPosition ? _predictedPlayerPosition : Vector3.Zero;
        var cameraOffset = Vector3.Transform(new Vector3(0f, 6.5f, 10f), Matrix.CreateRotationY(MathHelper.ToRadians(_cameraYawDegrees)));
        var cameraPosition = target + cameraOffset;
        var view = Matrix.CreateLookAt(cameraPosition, target, Vector3.Up);
        var projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(60f),
            _graphics.GraphicsDevice.Viewport.AspectRatio, 0.1f, 200f);

        _lastViewMatrix = view;
        _lastProjectionMatrix = projection;
        _lastCameraPosition = cameraPosition;
        _hasCameraMatrices = true;

    DrawGroundPlane(view, projection);
    DrawGroundGrid(view, projection);
        DrawBillboardEntities(view, projection, cameraPosition);

        base.Draw(gameTime);
        _hudDesktop?.Render();
    }

    protected override void OnExiting(object sender, EventArgs args)
    {
        base.OnExiting(sender, args);
        StopMovementLoop();
        if (_client != null)
        {
            _client.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }

    private void EnsureClientInitialization()
    {
        if (_initializationTask == null)
        {
            StartClientInitialization();
            return;
        }

        if (_initializationTask.IsFaulted && !_connectionErrorDisplayed)
        {
            _connectionErrorDisplayed = true;
            var message = _initializationTask.Exception?.GetBaseException().Message ?? "Unknown error";
            EnqueueLog($"Connection failed: {message}");
            EnqueueUi(() =>
            {
                if (_statusLabel != null)
                {
                    _statusLabel.Text = $"Status: connection failed ({message})";
                }
            });
        }
    }

    private void StartClientInitialization()
    {
        _initializationTask = Task.Run(async () =>
        {
            var client = new GrpcGameClient(_configuration);
            client.SnapshotReceived += snapshot =>
            {
                lock (_snapshotLock)
                {
                    _pendingSnapshot = snapshot;
                }

                EnqueueUi(() =>
                {
                    if (_snapshotLabel != null)
                    {
                        var npcCount = snapshot.Npcs?.Count ?? 0;
                        var mapCount = snapshot.MapObjects?.Count ?? 0;
                        var playerCount = snapshot.Characters?.Count ?? 0;
                        _snapshotLabel.Text = $"Snapshot: {playerCount} players / {npcCount} NPC / {mapCount} objects";
                    }
                });
            };
            client.Log += EnqueueLog;

            try
            {
                await client.InitializeAsync().ConfigureAwait(false);
                _client = client;

                EnqueueUi(() =>
                {
                    if (_statusLabel != null)
                    {
                        var worldName = client.WorldName ?? client.WorldId?.ToString() ?? "world";
                        _statusLabel.Text = $"Status: connected to {worldName}";
                    }
                });

                EnqueueLog("Connected to game server.");
            }
            catch (Exception ex)
            {
                EnqueueLog($"Initialization error: {ex.Message}");
                throw;
            }
        });
    }

    private void ProcessPendingSnapshot()
    {
        WorldSnapshot? snapshot = null;
        lock (_snapshotLock)
        {
            if (_pendingSnapshot != null)
            {
                snapshot = _pendingSnapshot;
                _pendingSnapshot = null;
            }
        }

        if (snapshot == null)
        {
            return;
        }

        _npcEntities.Clear();
        if (snapshot.Npcs != null)
        {
            foreach (var npc in snapshot.Npcs)
            {
                if (npc.Location == null)
                {
                    continue;
                }

                var position = ToWorldPosition(npc.Location);
                var tint = npc.IsAlive ? Color.White : new Color(120, 120, 120);
                var size = (npc.IsAlive ? NpcAliveBillboardBaseSize : NpcInactiveBillboardBaseSize) * CharacterScaleMultiplier;
                var rotation = npc.Location.Rotation;
                var texture = ResolveNpcTexture(npc);
                _npcEntities.Add(new RenderEntity(position, size, tint, rotation, texture));
            }
        }

        _mapObjectVisualizations.Clear();
        if (snapshot.MapObjects != null)
        {
            foreach (var mapObject in snapshot.MapObjects)
            {
                if (mapObject.Location == null)
                {
                    continue;
                }

                _currentWorldId = mapObject.Location.WorldId;
                _currentZoneId = mapObject.Location.ZoneName;
                _currentMapId = mapObject.Location.MapId;

                var position = ToWorldPosition(mapObject.Location);
                var rotationDegrees = (float)mapObject.Location.Rotation;

                if (_mapObjectOverrides.TryGetValue(mapObject.MapObjectId, out var overrideData))
                {
                    if (overrideData.Position.HasValue)
                    {
                        position = overrideData.Position.Value;
                        UpdateLocationFromWorld(mapObject.Location, position);
                    }

                    if (overrideData.RotationDegrees.HasValue)
                    {
                        rotationDegrees = overrideData.RotationDegrees.Value;
                        mapObject.Location.Rotation = rotationDegrees;
                    }
                }

                var tint = mapObject.IsActive ? Color.White : new Color(120, 120, 120);
                var visual = ResolveMapObjectVisual(mapObject);
                var baseSize = mapObject.IsActive ? 2.4f : 1.8f;
                var scale = ResolveMapObjectScale(mapObject, visual.Key);

                if (_mapObjectOverrides.TryGetValue(mapObject.MapObjectId, out var overrideScaleData) &&
                    overrideScaleData.Scale.HasValue)
                {
                    scale = overrideScaleData.Scale.Value;
                }

                var size = baseSize * scale;
                var texture = visual.Texture ?? _mapObjectTexture;
                var renderEntity = new RenderEntity(position, size, tint, rotationDegrees, texture);
                _mapObjectVisualizations.Add(new MapObjectVisualization(mapObject, renderEntity, visual.Texture ?? _mapObjectTexture, visual.Key, baseSize, scale));
            }
        }

        _otherPlayerEntities.Clear();
        if (snapshot.Characters != null && _client?.Session != null)
        {
            foreach (var character in snapshot.Characters)
            {
                if (character.Location == null)
                {
                    continue;
                }

                var position = ToWorldPosition(character.Location);
                if (string.Equals(character.SessionId, _client.Session.SessionId.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    _serverPlayerPosition = position;
                    var rotationDegrees = NormalizeDegrees(character.Location.Rotation);
                    _targetRotationDegrees = rotationDegrees;

                    if (!_hasPlayerPosition)
                    {
                        _predictedPlayerPosition = position;
                        _predictedVelocity = Vector3.Zero;
                        _playerRotationDegrees = rotationDegrees;
                        _hasPlayerPosition = true;
                    }
                    else
                    {
                        if (Vector3.Distance(_predictedPlayerPosition, position) > 5f)
                        {
                            _predictedPlayerPosition = position;
                            _predictedVelocity = Vector3.Zero;
                        }

                        var rotationDelta = MathHelper.WrapAngle(MathHelper.ToRadians(rotationDegrees - _playerRotationDegrees));
                        if (Math.Abs(rotationDelta) > MathHelper.ToRadians(120f))
                        {
                            _playerRotationDegrees = rotationDegrees;
                        }
                    }

                    _facingDirection = DirectionFromRotation(rotationDegrees);
                    _movementSpeed = Math.Max(_movementSpeed, 2.5f);
                    _playerEntity = new RenderEntity(_predictedPlayerPosition, PlayerBillboardBaseSize * CharacterScaleMultiplier, Color.White, _playerRotationDegrees);
                    _hasPlayerEntity = true;
                    continue;
                }

                var tint = new Color(120, 200, 255);
                _otherPlayerEntities.Add(new RenderEntity(position, OtherPlayerBillboardBaseSize * CharacterScaleMultiplier, tint, character.Location.Rotation));
            }
        }

        _lastSnapshotUtc = DateTime.UtcNow;
    RefreshMovementStatus();
    }

    private void UpdateMovementPrediction(GameTime gameTime)
    {
        if (!_hasPlayerPosition)
        {
            return;
        }

        var delta = (float)gameTime.ElapsedGameTime.TotalSeconds;

        var targetVelocity = Vector3.Zero;
        if (_activeMovementDirection.HasValue)
        {
            targetVelocity = DirectionToVector(_activeMovementDirection.Value) * _movementSpeed;
        }

        var accelRate = _activeMovementDirection.HasValue ? 12f : 18f;
        var velocityLerp = MathHelper.Clamp(delta * accelRate, 0f, 1f);
        _predictedVelocity = Vector3.Lerp(_predictedVelocity, targetVelocity, velocityLerp);

        _predictedPlayerPosition += _predictedVelocity * delta;

        var diff = _serverPlayerPosition - _predictedPlayerPosition;
        var diffLength = diff.Length();

        if (diffLength > 5f)
        {
            _predictedPlayerPosition = _serverPlayerPosition;
            if (!_activeMovementDirection.HasValue)
            {
                _predictedVelocity = Vector3.Zero;
            }
        }
        else if (diffLength > 0.01f)
        {
            var correctionRate = _activeMovementDirection.HasValue ? 4f : 10f;
            var correctionFactor = MathHelper.Clamp(delta * correctionRate, 0f, 1f);
            _predictedPlayerPosition += diff * correctionFactor;
        }

    _playerEntity = new RenderEntity(_predictedPlayerPosition, PlayerBillboardBaseSize * CharacterScaleMultiplier, Color.White, _playerRotationDegrees);
        _hasPlayerEntity = true;
    }

    private void HandleMovementInput(KeyboardState keyboardState)
    {
    if (!TryResolveMovementIntent(keyboardState, out var direction, out var preserveFacing))
        {
            if (_activeMovementDirection != null)
            {
                QueueStopMovement();
            }

            return;
        }

        if (_activeMovementDirection == direction && _activePreserveFacing == preserveFacing)
        {
            return;
        }

        QueueStartMovement(direction, preserveFacing);
    }

    private void HandleEditorInput(GameTime gameTime, KeyboardState keyboardState)
    {
        var toggleKeyDown = keyboardState.IsKeyDown(Keys.P);
        if (toggleKeyDown && !_editToggleKeyDown)
        {
            ToggleEditorMode();
        }

        _editToggleKeyDown = toggleKeyDown;

        var mouseState = Mouse.GetState();

        if (!_editorModeEnabled)
        {
            _addObjectKeyDown = keyboardState.IsKeyDown(Keys.OemPlus) || keyboardState.IsKeyDown(Keys.Add);
            _previousMouseState = mouseState;
            return;
        }

        var addKeyDown = keyboardState.IsKeyDown(Keys.OemPlus) || keyboardState.IsKeyDown(Keys.Add);
        if (addKeyDown && !_addObjectKeyDown)
        {
            BeginAddMapObject();
        }

        _addObjectKeyDown = addKeyDown;

        if (_pendingMapObjectCreation != null)
        {
            if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
            {
                TryPlaceNewMapObject(new Point(mouseState.X, mouseState.Y));
            }
        }
        else
        {
            if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
            {
                TrySelectMapObject(new Point(mouseState.X, mouseState.Y));
            }
        }

        _previousMouseState = mouseState;
    }

    private void ToggleEditorMode()
    {
        _editorModeEnabled = !_editorModeEnabled;

        if (_editorModeEnabled)
        {
            if (!EnsureSeedDataPersistence())
            {
                _editorModeEnabled = false;
                return;
            }

            if (_hudDesktop == null)
            {
                EnqueueLog("Editor: HUD not initialized.");
                _editorModeEnabled = false;
                return;
            }

            _editorPanel ??= new EditorPanel(this, _hudDesktop);
            _editorPanel.Show();
            EnqueueLog("Editor mode enabled. Click objects to edit, press '+' to add.");
        }
        else
        {
            _pendingMapObjectCreation = null;
            DeselectMapObject();
            _editorPanel?.Hide();
            EnqueueLog("Editor mode disabled.");
        }
    }

    private bool EnsureSeedDataPersistence()
    {
        if (_seedDataPersistence != null)
        {
            return true;
        }

        if (SeedDataPersistence.TryCreate(out var persistence, out var error))
        {
            _seedDataPersistence = persistence;
            return true;
        }

        EnqueueLog(error ?? "Editor: Unable to locate seed data.");
        return false;
    }

    private void BeginAddMapObject()
    {
        if (!EnsureSeedDataPersistence())
        {
            return;
        }

        if (_hudDesktop == null)
        {
            return;
        }

        var textureKeys = _customTextures.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();
        if (textureKeys.Count == 0)
        {
            EnqueueLog("Editor: No textures available for placement.");
            return;
        }

        var picker = new TexturePickerWindow(this, textureKeys);
        picker.Show(_hudDesktop);
    }

    internal void BeginPlacementForTexture(string textureKey)
    {
        _pendingMapObjectCreation = new PendingMapObjectCreation(textureKey);
        EnqueueLog($"Editor: Placement mode for '{textureKey}'. Click on the ground to place the object.");
    }

    private void TryPlaceNewMapObject(Point mousePoint)
    {
        if (_pendingMapObjectCreation == null)
        {
            return;
        }

        if (!_hasCameraMatrices)
        {
            EnqueueLog("Editor: Camera matrices unavailable for placement.");
            return;
        }

        if (!TryProjectToGround(mousePoint, out var worldPosition))
        {
            EnqueueLog("Editor: Unable to determine placement position.");
            return;
        }

        if (!EnsureSeedDataPersistence())
        {
            return;
        }

        var textureKey = _pendingMapObjectCreation.TextureKey;
        _pendingMapObjectCreation = null;

        var texture = _customTextures.TryGetValue(textureKey, out var customTexture) ? customTexture : _mapObjectTexture;
        var baseSize = 2.4f;
        var scale = ResolveDefaultScaleForTexture(textureKey);
        var size = baseSize * scale;
        var renderEntity = new RenderEntity(worldPosition, size, Color.White, 0f, texture ?? _mapObjectTexture);

        var mapObject = new WorldMapObject
        {
            MapObjectId = Guid.NewGuid().ToString(),
            Name = $"editor.mapobject.{textureKey}.{Guid.NewGuid():N}",
            DisplayName = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(textureKey.Replace('_', ' ')),
            IsActive = true,
            Location = CreateLocationFromWorld(worldPosition, 0d, _currentMapId, _currentZoneId, _currentWorldId)
        };

        mapObject.Tags.Add("editor");
        if (!string.IsNullOrWhiteSpace(textureKey))
        {
            mapObject.Tags.Add(textureKey);
        }

        mapObject.State["textureKey"] = textureKey;
        mapObject.State["billboardScale"] = scale.ToString(CultureInfo.InvariantCulture);
        mapObject.LastUpdated = DateTime.UtcNow.Ticks;

        var visualization = new MapObjectVisualization(mapObject, renderEntity, texture ?? _mapObjectTexture, textureKey, baseSize, scale);
        _mapObjectVisualizations.Add(visualization);

        _mapObjectOverrides[mapObject.MapObjectId] = new MapObjectOverride
        {
            Position = worldPosition,
            Scale = scale,
            RotationDegrees = 0f
        };

        _selectedMapObject = visualization;

        string? seedPath = null;
        if (_seedDataPersistence != null)
        {
            seedPath = _seedDataPersistence.UpdateOrCreateMapObject(mapObject, textureKey, scale);
        }

        _editorPanel?.Bind(visualization, seedPath);
        EnqueueLog(seedPath != null
            ? $"Editor: Added new map object saved to {Path.GetFileName(seedPath)}."
            : "Editor: Added new map object.");
    }

    private void TrySelectMapObject(Point mousePoint)
    {
        if (!_hasCameraMatrices || _mapObjectVisualizations.Count == 0)
        {
            return;
        }

        var viewport = GraphicsDevice.Viewport;
        MapObjectVisualization? closest = null;
        var closestDistance = float.MaxValue;

        foreach (var visualization in _mapObjectVisualizations)
        {
            var projected = viewport.Project(visualization.Render.Position, _lastProjectionMatrix, _lastViewMatrix, Matrix.Identity);
            if (projected.Z < 0f || projected.Z > 1f)
            {
                continue;
            }

            var screenPos = new Vector2(projected.X, projected.Y);
            var delta = new Vector2(mousePoint.X, mousePoint.Y) - screenPos;
            var distance = delta.Length();
            var radius = ComputeBillboardPixelSize(visualization.Render, _lastCameraPosition) * 0.5f;

            if (distance <= radius && distance < closestDistance)
            {
                closest = visualization;
                closestDistance = distance;
            }
        }

        if (closest != null)
        {
            _selectedMapObject = closest;
            var seedPath = GetSeedFilePath(closest);
            _editorPanel?.Bind(closest, seedPath);
        }
        else
        {
            DeselectMapObject();
        }
    }

    private bool TryApplyMapObjectChanges(MapObjectVisualization visualization, Vector3 worldPosition,
        float rotationDegrees, float scale, string textureKey, string? displayName,
        out string? seedPath, out string? errorMessage)
    {
        seedPath = null;
        errorMessage = null;

        if (scale <= 0f || scale > 40f)
        {
            errorMessage = "Scale must be between 0 and 40.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(textureKey))
        {
            textureKey = visualization.TextureKey;
        }

        if (visualization.Snapshot.Location == null)
        {
            visualization.Snapshot.Location = new Location();
        }

        var location = visualization.Snapshot.Location!;
        UpdateLocationFromWorld(location, worldPosition);
        location.Rotation = rotationDegrees;

        if (!_mapObjectOverrides.TryGetValue(visualization.Snapshot.MapObjectId, out var overrideData))
        {
            overrideData = new MapObjectOverride();
            _mapObjectOverrides[visualization.Snapshot.MapObjectId] = overrideData;
        }

        overrideData.Position = worldPosition;
        overrideData.RotationDegrees = rotationDegrees;
        overrideData.Scale = scale;

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            visualization.Snapshot.DisplayName = displayName;
        }

        visualization.Scale = scale;
        visualization.BaseSize = visualization.Snapshot.IsActive ? 2.4f : 1.8f;
        visualization.TextureKey = textureKey;

        var texture = GetCustomTexture(textureKey) ?? visualization.FallbackTexture ?? _mapObjectTexture;
        var tint = visualization.Snapshot.IsActive ? Color.White : new Color(120, 120, 120);
        visualization.Render = new RenderEntity(worldPosition, visualization.BaseSize * scale, tint, rotationDegrees, texture);

        var state = visualization.Snapshot.State;
        if (state == null)
        {
            errorMessage = "Map object state is not initialized.";
            return false;
        }

    state["textureKey"] = textureKey;
        state["billboardScale"] = scale.ToString(CultureInfo.InvariantCulture);
        visualization.Snapshot.LastUpdated = DateTime.UtcNow.Ticks;

        if (_seedDataPersistence is not null)
        {
            seedPath = _seedDataPersistence.UpdateOrCreateMapObject(visualization.Snapshot, textureKey, scale);
        }

        _editorPanel?.SetSeedFile(seedPath);

        return true;
    }

    private void DeselectMapObject()
    {
        _selectedMapObject = null;
        _editorPanel?.ClearSelection();
    }

    private string? GetSeedFilePath(MapObjectVisualization visualization)
    {
        if (_seedDataPersistence is null)
        {
            return null;
        }

        return _seedDataPersistence.TryGetSeedPath(visualization.Snapshot.MapObjectId, visualization.Snapshot.Name);
    }

    private bool TryProjectToGround(Point screenPoint, out Vector3 worldPosition)
    {
        worldPosition = default;

        if (!_hasCameraMatrices)
        {
            return false;
        }

        var viewport = GraphicsDevice.Viewport;
        var nearSource = new Vector3(screenPoint.X, screenPoint.Y, 0f);
        var farSource = new Vector3(screenPoint.X, screenPoint.Y, 1f);

        var nearPoint = viewport.Unproject(nearSource, _lastProjectionMatrix, _lastViewMatrix, Matrix.Identity);
        var farPoint = viewport.Unproject(farSource, _lastProjectionMatrix, _lastViewMatrix, Matrix.Identity);

        var direction = farPoint - nearPoint;
        if (direction.LengthSquared() < 0.0001f)
        {
            return false;
        }

        direction.Normalize();
        if (Math.Abs(direction.Y) < 1e-6f)
        {
            return false;
        }

        var t = (GroundPlaneY - nearPoint.Y) / direction.Y;
        if (t < 0f)
        {
            return false;
        }

        var hitPoint = nearPoint + direction * t;
        worldPosition = new Vector3(hitPoint.X, GroundPlaneY, hitPoint.Z);
        return true;
    }

    private float ComputeBillboardPixelSize(RenderEntity entity, Vector3 cameraPosition)
    {
        var distance = Vector3.Distance(cameraPosition, entity.Position);
        var sizeFactor = BillboardScaleFactor * (entity.Size / MathF.Max(distance, 0.1f));

        var maxPixelSize = 240f;
        if (entity.Size >= 24f)
        {
            maxPixelSize = 2048f;
        }
        else if (entity.Size >= 12f)
        {
            maxPixelSize = 960f;
        }
        else if (entity.Size >= 6f)
        {
            maxPixelSize = 600f;
        }
        else if (entity.Size >= 3f)
        {
            maxPixelSize = 360f;
        }

        return MathHelper.Clamp(sizeFactor, 16f, maxPixelSize);
    }

    private static float ResolveDefaultScaleForTexture(string textureKey)
    {
        if (!string.IsNullOrWhiteSpace(textureKey) && MapObjectScaleMultipliers.TryGetValue(textureKey, out var scale))
        {
            return scale;
        }

        return MapObjectScaleMultipliers.TryGetValue("default", out var defaultScale)
            ? defaultScale
            : DefaultMapObjectScaleMultiplier;
    }

    private void HandleRotationInput(KeyboardState keyboardState, float deltaSeconds)
    {
        var rotateLeftDown = keyboardState.IsKeyDown(Keys.Q);
        var rotateRightDown = keyboardState.IsKeyDown(Keys.E);

        if (rotateLeftDown != rotateRightDown)
        {
            var direction = rotateRightDown ? 1f : -1f;
            var yaw = _cameraYawDegrees + direction * RotationDegreesPerSecond * deltaSeconds;
            _cameraYawDegrees = NormalizeDegrees(yaw);
        }

        UpdateCameraOrientationVectors();
    }

    private void UpdateCameraOrientationVectors()
    {
        var yawRadians = MathHelper.ToRadians(_cameraYawDegrees);
        var rotation = Matrix.CreateRotationY(yawRadians);

        var forward = Vector3.Transform(-Vector3.UnitZ, rotation);
        if (forward.LengthSquared() > 0f)
        {
            forward.Normalize();
        }

        var right = Vector3.Transform(Vector3.UnitX, rotation);
        if (right.LengthSquared() > 0f)
        {
            right.Normalize();
        }

        _cameraForward = forward;
        _cameraRight = right;
    }

    private void QueueStartMovement(int direction, bool preserveFacing)
    {
        if (_client == null)
        {
            return;
        }

        if (_movementLoopTask != null && !_movementLoopTask.IsCompleted)
        {
            if (_activeMovementDirection == direction && _activePreserveFacing == preserveFacing)
            {
                return;
            }

            StopMovementLoop();
        }

        _activeMovementDirection = direction;
        _activePreserveFacing = preserveFacing;

        var loopCts = new CancellationTokenSource();
        _movementLoopCts = loopCts;
        _movementLoopTask = RunMovementLoopAsync(direction, preserveFacing, loopCts.Token);
    }

    private void QueueStopMovement()
    {
        if (_client == null)
        {
            return;
        }

    StopMovementLoop();
    _predictedVelocity = Vector3.Zero;

        var hadMovement = _activeMovementDirection.HasValue;
        _activeMovementDirection = null;
        _activePreserveFacing = false;
        UpdateMovementStatus("idle");

        if (!hadMovement)
        {
            return;
        }

        Task.Run(async () =>
        {
            try
            {
                var success = await _client.StopMovementAsync().ConfigureAwait(false);
                if (!success)
                {
                    EnqueueLog("StopMovement rejected by server.");
                }
            }
            catch (Exception ex)
            {
                EnqueueLog($"StopMovement error: {ex.Message}");
            }
        });
    }

    private async Task RunMovementLoopAsync(int direction, bool preserveFacing, CancellationToken cancellationToken)
    {
        var preserveText = preserveFacing ? "strafe" : "move";
        UpdateMovementStatus($"dir {direction} ({preserveText})");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var success = await _client!.StartMovementAsync(direction, preserveFacing).ConfigureAwait(false);
                if (!success)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken).ConfigureAwait(false);
                    continue;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                EnqueueLog($"StartMovement error: {ex.Message}");
                await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken).ConfigureAwait(false);
                continue;
            }

            try
            {
                await Task.Delay(MovementCommandInterval, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private void StopMovementLoop()
    {
        if (_movementLoopCts == null)
        {
            return;
        }

        try
        {
            _movementLoopCts.Cancel();
        }
        catch
        {
            // ignore cancellation exceptions
        }
        finally
        {
            _movementLoopCts.Dispose();
            _movementLoopCts = null;
            _movementLoopTask = null;
        }
    }

    private void UpdateMovementStatus(string status, bool refreshOnly = false)
    {
        if (!refreshOnly)
        {
            _movementStatus = status;
        }

        var displayStatus = refreshOnly ? _movementStatus : status;

        EnqueueUi(() =>
        {
            if (_movementLabel == null)
            {
                return;
            }

            var facingDegrees = _playerRotationDegrees;
            _movementLabel.Text = $"Movement: {displayStatus} | Facing: {_facingDirection} ({facingDegrees:0}°)";
        });
    }

    private void RefreshMovementStatus()
    {
        UpdateMovementStatus(_movementStatus, refreshOnly: true);
    }

    private void UpdateRotationSmoothing(float delta)
    {
        if (!_hasPlayerEntity)
        {
            return;
        }

        var currentRadians = MathHelper.ToRadians(_playerRotationDegrees);
        var targetRadians = MathHelper.ToRadians(_targetRotationDegrees);
        var diffRadians = MathHelper.WrapAngle(targetRadians - currentRadians);
        var maxStepRadians = MathHelper.ToRadians(RotationDegreesPerSecond) * delta;

        if (Math.Abs(diffRadians) <= maxStepRadians)
        {
            _playerRotationDegrees = NormalizeDegrees(_targetRotationDegrees);
            return;
        }

        var stepRadians = Math.Clamp(diffRadians, -maxStepRadians, maxStepRadians);
        var newRadians = currentRadians + stepRadians;
        _playerRotationDegrees = NormalizeDegrees(MathHelper.ToDegrees(newRadians));
    }

    private Texture2D LoadTextureOrFallback(string assetName, Func<Texture2D> fallbackFactory, params string[] relativePathSegments)
    {
        var texture = TryLoadTexture(relativePathSegments);
        if (texture != null)
        {
            return texture;
        }

        EnqueueLog($"{assetName} texture missing; using fallback asset.");
        return fallbackFactory();
    }

    private void RegisterCustomTexture(string key, params string[] relativePathSegments)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        if (_customTextures.ContainsKey(key))
        {
            return;
        }

        var texture = TryLoadTexture(relativePathSegments);
        if (texture != null)
        {
            _customTextures[key] = texture;
        }
    }

    private Texture2D? GetCustomTexture(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        return _customTextures.TryGetValue(key, out var texture) ? texture : null;
    }

    private Texture2D? TryLoadTexture(params string[] relativePathSegments)
    {
        try
        {
            var segments = new string[relativePathSegments.Length + 1];
            segments[0] = AppContext.BaseDirectory;
            Array.Copy(relativePathSegments, 0, segments, 1, relativePathSegments.Length);
            var fullPath = Path.Combine(segments);

            if (!File.Exists(fullPath))
            {
                return null;
            }

            using var stream = File.OpenRead(fullPath);
            return Texture2D.FromStream(GraphicsDevice, stream);
        }
        catch (Exception ex)
        {
            EnqueueLog($"Texture load error for {string.Join('/', relativePathSegments)}: {ex.Message}");
            return null;
        }
    }

    private Texture2D CreateFallbackPlayerTexture()
    {
        return CreateCircleTexture(96, new Color(120, 210, 255), new Color(40, 140, 220));
    }

    private Texture2D CreateFallbackNpcTexture()
    {
        return CreateCircleTexture(84, new Color(255, 182, 66), new Color(180, 84, 12));
    }

    private Texture2D CreateFallbackMapObjectTexture()
    {
        return CreateDiamondTexture(70, new Color(190, 190, 190), new Color(120, 120, 120));
    }

    private void DrawBillboardEntities(Matrix view, Matrix projection, Vector3 cameraPosition)
    {
        if (_spriteBatch == null)
        {
            return;
        }

        GraphicsDevice.BlendState = BlendState.AlphaBlend;
        GraphicsDevice.DepthStencilState = DepthStencilState.None;
        GraphicsDevice.RasterizerState = RasterizerState.CullNone;

        _spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);

        foreach (var mapObject in _mapObjectVisualizations)
        {
            var texture = mapObject.Render.Texture ?? mapObject.FallbackTexture ?? _mapObjectTexture;
            if (texture == null)
            {
                continue;
            }

            var renderEntity = mapObject.Render;
            if (_editorModeEnabled && _selectedMapObject == mapObject)
            {
                DrawBillboard(renderEntity, texture, view, projection, cameraPosition, new Color(200, 255, 200));
            }
            else
            {
                DrawBillboard(renderEntity, texture, view, projection, cameraPosition);
            }
        }

        foreach (var npc in _npcEntities)
        {
            var texture = npc.Texture ?? _npcTexture ?? _playerTexture ?? _mapObjectTexture;
            if (texture == null)
            {
                continue;
            }

            DrawBillboard(npc, texture, view, projection, cameraPosition);
        }

        var playerFallbackTexture = _playerTexture ?? _npcTexture ?? _mapObjectTexture;
        if (playerFallbackTexture != null)
        {
            foreach (var otherPlayer in _otherPlayerEntities)
            {
                var texture = otherPlayer.Texture ?? playerFallbackTexture;
                DrawBillboard(otherPlayer, texture, view, projection, cameraPosition);
            }

            if (_hasPlayerEntity)
            {
                var texture = _playerEntity.Texture ?? playerFallbackTexture;
                DrawBillboard(_playerEntity, texture, view, projection, cameraPosition);
            }
        }

        _spriteBatch.End();
    }

    private void DrawBillboard(RenderEntity entity, Texture2D texture, Matrix view, Matrix projection, Vector3 cameraPosition, Color? colorOverride = null)
    {
        if (_spriteBatch == null)
        {
            return;
        }

        var viewport = GraphicsDevice.Viewport;
        var projected = viewport.Project(entity.Position, projection, view, Matrix.Identity);
        if (projected.Z < 0f || projected.Z > 1f)
        {
            return;
        }

        var distance = Vector3.Distance(cameraPosition, entity.Position);
        var sizeFactor = BillboardScaleFactor * (entity.Size / MathF.Max(distance, 0.1f));

        var maxPixelSize = 240f;
        if (entity.Size >= 24f)
        {
            maxPixelSize = 2048f;
        }
        else if (entity.Size >= 12f)
        {
            maxPixelSize = 960f;
        }
        else if (entity.Size >= 6f)
        {
            maxPixelSize = 600f;
        }
        else if (entity.Size >= 3f)
        {
            maxPixelSize = 360f;
        }

        var pixelSize = MathHelper.Clamp(sizeFactor, 16f, maxPixelSize);

        DrawShadow(entity, view, projection, pixelSize);

    var origin = new Vector2(texture.Width / 2f, texture.Height / 2f);
    var drawPosition = new Vector2(projected.X, projected.Y - pixelSize * 0.25f);
    var scale = pixelSize / texture.Width;
    const float rotation = 0f;
        var tint = colorOverride ?? entity.Color;
        var layerDepth = MathHelper.Clamp(1f - projected.Z, 0f, 1f);

        _spriteBatch.Draw(texture, drawPosition, null, tint, rotation, origin, scale, SpriteEffects.None, layerDepth);
    }

    private void DrawShadow(RenderEntity entity, Matrix view, Matrix projection, float pixelSize)
    {
        if (_spriteBatch == null || _shadowTexture == null)
        {
            return;
        }

        var viewport = GraphicsDevice.Viewport;
        var groundPosition = new Vector3(entity.Position.X, 0.02f, entity.Position.Z);
        var projected = viewport.Project(groundPosition, projection, view, Matrix.Identity);
        if (projected.Z < 0f || projected.Z > 1f)
        {
            return;
        }

        var origin = new Vector2(_shadowTexture.Width / 2f, _shadowTexture.Height / 2f);
        var scale = pixelSize / _shadowTexture.Width;
        var layerDepth = MathHelper.Clamp(1f - projected.Z + 0.01f, 0f, 1f);

        _spriteBatch.Draw(_shadowTexture, new Vector2(projected.X, projected.Y), null, Color.White, 0f, origin, scale, SpriteEffects.None, layerDepth);
    }

    private void BuildGroundPlane()
    {
        _groundQuadVertices = new[]
        {
            new VertexPositionTexture(new Vector3(-GroundHalfSize, GroundPlaneY, -GroundHalfSize), new Vector2(0f, 0f)),
            new VertexPositionTexture(new Vector3(-GroundHalfSize, GroundPlaneY, GroundHalfSize), new Vector2(0f, GroundTextureRepeat)),
            new VertexPositionTexture(new Vector3(GroundHalfSize, GroundPlaneY, -GroundHalfSize), new Vector2(GroundTextureRepeat, 0f)),
            new VertexPositionTexture(new Vector3(GroundHalfSize, GroundPlaneY, GroundHalfSize), new Vector2(GroundTextureRepeat, GroundTextureRepeat))
        };

        _groundQuadIndices = new short[] { 0, 1, 2, 2, 1, 3 };
    }

    private void DrawGroundPlane(Matrix view, Matrix projection)
    {
        if (_effect == null || _groundQuadVertices == null || _groundQuadIndices == null || _groundTexture == null)
        {
            return;
        }

        GraphicsDevice.BlendState = BlendState.Opaque;
        GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        GraphicsDevice.RasterizerState = RasterizerState.CullNone;
        GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;

        _effect.World = Matrix.Identity;
        _effect.View = view;
        _effect.Projection = projection;
        _effect.TextureEnabled = true;
        _effect.VertexColorEnabled = false;
        _effect.Texture = _groundTexture;

        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _groundQuadVertices, 0, _groundQuadVertices.Length, _groundQuadIndices, 0, _groundQuadIndices.Length / 3);
        }
    }

    private void BuildGroundGrid()
    {
        var lines = new List<VertexPositionColor>();
        var half = (int)MathF.Ceiling(GroundHalfSize);
        const float spacing = 1f;
        var baseColor = new Color(34, 58, 34, 110);
        var axisColor = new Color(90, 160, 90, 170);

        for (var i = -half; i <= half; i++)
        {
            var color = i == 0 ? axisColor : baseColor;
            lines.Add(new VertexPositionColor(new Vector3(i * spacing, 0f, -half * spacing), color));
            lines.Add(new VertexPositionColor(new Vector3(i * spacing, 0f, half * spacing), color));
            lines.Add(new VertexPositionColor(new Vector3(-half * spacing, 0f, i * spacing), color));
            lines.Add(new VertexPositionColor(new Vector3(half * spacing, 0f, i * spacing), color));
        }

        _groundVertices = lines.ToArray();
    }

    private void DrawGroundGrid(Matrix view, Matrix projection)
    {
        if (_effect == null || _groundVertices == null || _groundVertices.Length == 0)
        {
            return;
        }

        GraphicsDevice.BlendState = BlendState.AlphaBlend;
        GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        GraphicsDevice.RasterizerState = RasterizerState.CullNone;
        GraphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;

        _effect.World = Matrix.Identity;
        _effect.View = view;
        _effect.Projection = projection;
        _effect.VertexColorEnabled = true;
        _effect.TextureEnabled = false;
        _effect.Texture = null;

        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, _groundVertices, 0, _groundVertices.Length / 2);
        }
    }

    private Texture2D CreateFallbackGroundTexture()
    {
        const int size = 128;
        const int tileSize = 16;
        var light = new Color(82, 118, 82);
        var dark = new Color(66, 94, 66);

        var data = new Color[size * size];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var index = y * size + x;
                var isLight = ((x / tileSize) + (y / tileSize)) % 2 == 0;
                data[index] = isLight ? light : dark;
            }
        }

        var texture = new Texture2D(GraphicsDevice, size, size);
        texture.SetData(data);
        return texture;
    }

    private Texture2D CreateCircleTexture(int diameter, Color fillColor, Color outlineColor)
    {
        var texture = new Texture2D(GraphicsDevice, diameter, diameter);
        var data = new Color[diameter * diameter];
        var radius = diameter / 2f;
        var center = new Vector2(radius - 0.5f);

        for (var y = 0; y < diameter; y++)
        {
            for (var x = 0; x < diameter; x++)
            {
                var index = y * diameter + x;
                var distance = Vector2.Distance(new Vector2(x, y), center);

                if (distance > radius)
                {
                    data[index] = Color.Transparent;
                    continue;
                }

                var t = MathHelper.Clamp(1f - (distance / radius), 0f, 1f);
                var color = Color.Lerp(outlineColor, fillColor, MathF.Pow(t, 0.6f));
                color.A = (byte)MathHelper.Clamp(t * 255f, 0f, 255f);
                data[index] = color;
            }
        }

        texture.SetData(data);
        return texture;
    }

    private Texture2D CreateDiamondTexture(int size, Color fillColor, Color outlineColor)
    {
        var texture = new Texture2D(GraphicsDevice, size, size);
        var data = new Color[size * size];
        var half = size / 2f;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var index = y * size + x;
                var dx = Math.Abs(x - (half - 0.5f));
                var dy = Math.Abs(y - (half - 0.5f));
                var value = dx + dy;

                if (value > half)
                {
                    data[index] = Color.Transparent;
                    continue;
                }

                var t = MathHelper.Clamp(1f - (value / half), 0f, 1f);
                var color = Color.Lerp(outlineColor, fillColor, t);
                color.A = (byte)MathHelper.Clamp(t * 255f, 0f, 255f);
                data[index] = color;
            }
        }

        texture.SetData(data);
        return texture;
    }

    private Texture2D CreateShadowTexture(int diameter)
    {
        var texture = new Texture2D(GraphicsDevice, diameter, diameter);
        var data = new Color[diameter * diameter];
        var radius = diameter / 2f;
        var center = new Vector2(radius - 0.5f);

        for (var y = 0; y < diameter; y++)
        {
            for (var x = 0; x < diameter; x++)
            {
                var index = y * diameter + x;
                var distance = Vector2.Distance(new Vector2(x, y), center);

                if (distance > radius)
                {
                    data[index] = Color.Transparent;
                    continue;
                }

                var t = MathHelper.Clamp(1f - (distance / radius), 0f, 1f);
                var alpha = (byte)MathHelper.Clamp(t * 140f, 0f, 255f);
                data[index] = new Color(0, 0, 0, (int)alpha);
            }
        }

        texture.SetData(data);
        return texture;
    }

    private void BuildHud()
    {
        MyraEnvironment.Game = this;
        _hudDesktop = new Desktop();

        var panel = new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Background = new SolidBrush(new Color(0, 0, 0, 180))
        };

        var stack = new VerticalStackPanel
        {
            Spacing = 4
        };

        _statusLabel = new Label { Text = "Status: connecting..." };
        _movementLabel = new Label
        {
            Text = $"Movement: {_movementStatus} | Facing: {_facingDirection} ({_playerRotationDegrees:0}°)"
        };
        _snapshotLabel = new Label { Text = "Snapshot: awaiting data" };
        _logLabel = new Label { Text = string.Empty };

        stack.Widgets.Add(_statusLabel);
        stack.Widgets.Add(_movementLabel);
        stack.Widgets.Add(_snapshotLabel);
        stack.Widgets.Add(new Label { Text = "Logs:" });
        stack.Widgets.Add(_logLabel);

        panel.Widgets.Add(stack);
        _hudDesktop.Root = panel;
    }

    private void ProcessUiActions()
    {
        lock (_uiActionsLock)
        {
            while (_uiActions.Count > 0)
            {
                var action = _uiActions.Dequeue();
                action();
            }
        }
    }

    private void EnqueueUi(Action action)
    {
        lock (_uiActionsLock)
        {
            _uiActions.Enqueue(action);
        }
    }

    private void EnqueueLog(string message)
    {
        lock (_logLines)
        {
            if (_logLines.Count >= MaxLogLines)
            {
                _logLines.Dequeue();
            }

            _logLines.Enqueue(message);
        }

        EnqueueUi(() =>
        {
            if (_logLabel != null)
            {
                lock (_logLines)
                {
                    _logLabel.Text = string.Join(Environment.NewLine, _logLines);
                }
            }
        });
    }

    private static Vector3 ToWorldPosition(Location location)
    {
        return new Vector3((float)location.X, (float)location.Z, (float)location.Y);
    }

    private static void UpdateLocationFromWorld(Location location, Vector3 worldPosition)
    {
        location.X = worldPosition.X;
        location.Y = worldPosition.Z;
        location.Z = worldPosition.Y;
    }

    private static Location CreateLocationFromWorld(Vector3 worldPosition, double rotationDegrees, string? mapId, string? zoneName, string? worldId)
    {
        return new Location
        {
            X = worldPosition.X,
            Y = worldPosition.Z,
            Z = worldPosition.Y,
            Rotation = (float)rotationDegrees,
            MapId = mapId ?? string.Empty,
            ZoneName = zoneName ?? string.Empty,
            WorldId = worldId ?? string.Empty
        };
    }

    private static Vector3 DirectionToVector(int direction)
    {
        var normalized = NormalizeDirection(direction);
        return normalized switch
        {
            1 => new Vector3(0f, 0f, 1f),
            2 => Vector3.Normalize(new Vector3(1f, 0f, 1f)),
            3 => new Vector3(1f, 0f, 0f),
            4 => Vector3.Normalize(new Vector3(1f, 0f, -1f)),
            5 => new Vector3(0f, 0f, -1f),
            6 => Vector3.Normalize(new Vector3(-1f, 0f, -1f)),
            7 => new Vector3(-1f, 0f, 0f),
            _ => Vector3.Normalize(new Vector3(-1f, 0f, 1f))
        };
    }

    private Texture2D? ResolveNpcTexture(WorldNpc npc)
    {
        if (npc == null)
        {
            return null;
        }

        if (MatchesDescriptor(npc.Tags, npc.Name, null, "guide", "mentor", "tutorial"))
        {
            return GetCustomTexture("npc_guide") ?? GetCustomTexture("npc_guard");
        }

        if (MatchesDescriptor(npc.Tags, npc.Name, null, "merchant", "vendor", "trader"))
        {
            return GetCustomTexture("npc_merchant") ?? GetCustomTexture("npc_guard");
        }

        if (MatchesDescriptor(npc.Tags, npc.Name, null, "guard", "sentinel", "protector"))
        {
            return GetCustomTexture("npc_guard");
        }

        if (MatchesDescriptor(npc.Tags, npc.Name, null, "quest"))
        {
            return GetCustomTexture("npc_guide");
        }

        return null;
    }

    private MapObjectVisual ResolveMapObjectVisual(WorldMapObject mapObject)
    {
        var explicitKey = GetMapObjectStateValue(mapObject, "textureKey")
                          ?? GetMapObjectStateValue(mapObject, "texture_key");

        var candidateKeys = new List<string>();
        if (!string.IsNullOrWhiteSpace(explicitKey))
        {
            candidateKeys.Add(explicitKey);
        }

        candidateKeys.AddRange(ResolveMapObjectTextureKeys(mapObject));

        string? unmatchedExplicit = null;

        foreach (var key in candidateKeys.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (string.Equals(key, "default", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            var texture = GetCustomTexture(key);
            if (texture != null)
            {
                return new MapObjectVisual(key, texture);
            }

            if (unmatchedExplicit == null && explicitKey != null && string.Equals(key, explicitKey, StringComparison.OrdinalIgnoreCase))
            {
                unmatchedExplicit = explicitKey;
            }
        }

        var fallbackTexture = ResolveMapObjectFallbackTexture();
        var fallbackKey = unmatchedExplicit ?? "default";
        return new MapObjectVisual(fallbackKey, fallbackTexture);
    }

    private static IReadOnlyList<string> ResolveMapObjectTextureKeys(WorldMapObject mapObject)
    {
        if (mapObject == null)
        {
            return DefaultMapObjectTextureKeys;
        }

        if (MatchesDescriptor(mapObject.Tags, mapObject.Name, mapObject.DisplayName, "campfire", "fire", "bonfire", "firepit"))
        {
            return CampfireTextureKeys;
        }

        if (MatchesDescriptor(mapObject.Tags, mapObject.Name, mapObject.DisplayName, "spawn", "beacon"))
        {
            return SpawnBeaconTextureKeys;
        }

        if (MatchesDescriptor(mapObject.Tags, mapObject.Name, mapObject.DisplayName, "tree", "pine", "oak", "spruce"))
        {
            return TreeTextureKeys;
        }

        if (MatchesDescriptor(mapObject.Tags, mapObject.Name, mapObject.DisplayName, "house", "home", "building", "hut", "cottage"))
        {
            return HouseTextureKeys;
        }

        if (MatchesDescriptor(mapObject.Tags, mapObject.Name, mapObject.DisplayName, "rock", "stone", "boulder", "ore"))
        {
            return StoneTextureKeys;
        }

        if (MatchesDescriptor(mapObject.Tags, mapObject.Name, mapObject.DisplayName, "mountain", "cliff", "hill", "peak"))
        {
            return MountainTextureKeys;
        }

        if (MatchesDescriptor(mapObject.Tags, mapObject.Name, mapObject.DisplayName, "shrine", "altar", "monument", "totem"))
        {
            return ShrineTextureKeys;
        }

        if (MatchesDescriptor(mapObject.Tags, mapObject.Name, mapObject.DisplayName, "chest", "treasure"))
        {
            return ChestTextureKeys;
        }

        return DefaultMapObjectTextureKeys;
    }

    private static float ResolveMapObjectScale(WorldMapObject mapObject, string visualKey)
    {
        if (mapObject != null)
        {
            var overrideValue = GetMapObjectStateValue(mapObject, "billboardScale") ??
                                 GetMapObjectStateValue(mapObject, "billboard_scale") ??
                                 GetMapObjectStateValue(mapObject, "spriteScale") ??
                                 GetMapObjectStateValue(mapObject, "scale");

            if (!string.IsNullOrWhiteSpace(overrideValue) &&
                float.TryParse(overrideValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) &&
                parsed > 0f)
            {
                return MathHelper.Clamp(parsed, 0.5f, 20f);
            }
        }

        if (!string.IsNullOrWhiteSpace(visualKey) && MapObjectScaleMultipliers.TryGetValue(visualKey, out var scale))
        {
            return scale;
        }

        return MapObjectScaleMultipliers.TryGetValue("default", out var defaultScale)
            ? defaultScale
            : DefaultMapObjectScaleMultiplier;
    }

    private Texture2D? ResolveMapObjectFallbackTexture()
    {
        var fallbackKeys = new[]
        {
            "house",
            "house_alt",
            "house_alt2",
            "tree",
            "tree_alt",
            "stone",
            "mountain",
            "campfire",
            "spawn_beacon",
            "chest"
        };

        foreach (var key in fallbackKeys)
        {
            var texture = GetCustomTexture(key);
            if (texture != null)
            {
                return texture;
            }
        }

        return _mapObjectTexture;
    }

    private static bool MatchesDescriptor(IEnumerable<string>? tags, string? name, string? displayName, params string[] keywords)
    {
        if (keywords == null || keywords.Length == 0)
        {
            return false;
        }

        foreach (var keyword in keywords)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                continue;
            }

            if (ContainsKeyword(name, keyword) || ContainsKeyword(displayName, keyword))
            {
                return true;
            }

            if (tags != null)
            {
                foreach (var tag in tags)
                {
                    if (ContainsKeyword(tag, keyword))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static string? GetMapObjectStateValue(WorldMapObject mapObject, string key)
    {
        if (mapObject == null || mapObject.State == null)
        {
            return null;
        }

        foreach (var entry in mapObject.State)
        {
            if (string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                return entry.Value;
            }
        }

        return null;
    }

    private static bool ContainsKeyword(string? value, string keyword)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private bool TryResolveMovementIntent(KeyboardState keyboardState, out int direction, out bool preserveFacing)
    {
        var forward = keyboardState.IsKeyDown(Keys.W) || keyboardState.IsKeyDown(Keys.Up) ||
                      keyboardState.IsKeyDown(Keys.NumPad8) || keyboardState.IsKeyDown(Keys.NumPad9) ||
                      keyboardState.IsKeyDown(Keys.NumPad7);

        var backward = keyboardState.IsKeyDown(Keys.S) || keyboardState.IsKeyDown(Keys.Down) ||
                       keyboardState.IsKeyDown(Keys.NumPad2) || keyboardState.IsKeyDown(Keys.NumPad1) ||
                       keyboardState.IsKeyDown(Keys.NumPad3);

        var strafeLeft = keyboardState.IsKeyDown(Keys.A) || keyboardState.IsKeyDown(Keys.Left) ||
                          keyboardState.IsKeyDown(Keys.NumPad4) || keyboardState.IsKeyDown(Keys.NumPad7) ||
                          keyboardState.IsKeyDown(Keys.NumPad1);

        var strafeRight = keyboardState.IsKeyDown(Keys.D) || keyboardState.IsKeyDown(Keys.Right) ||
                           keyboardState.IsKeyDown(Keys.NumPad6) || keyboardState.IsKeyDown(Keys.NumPad9) ||
                           keyboardState.IsKeyDown(Keys.NumPad3);

        if (forward && backward)
        {
            forward = backward = false;
        }

        if (strafeLeft && strafeRight)
        {
            strafeLeft = strafeRight = false;
        }

        var desired = Vector3.Zero;

        if (forward)
        {
            desired += _cameraForward;
        }

        if (backward)
        {
            desired -= _cameraForward;
        }

        if (strafeRight)
        {
            desired += _cameraRight;
        }

        if (strafeLeft)
        {
            desired -= _cameraRight;
        }

        desired.Y = 0f;

        if (desired.LengthSquared() < 0.0001f)
        {
            direction = default;
            preserveFacing = false;
            return false;
        }

        desired.Normalize();

        var headingRadians = MathF.Atan2(desired.X, desired.Z);
        var headingDegrees = NormalizeDegrees(MathHelper.ToDegrees(headingRadians));
        direction = DirectionFromRotation(headingDegrees);

        preserveFacing = backward;
        if (!forward && !backward && (strafeLeft || strafeRight))
        {
            preserveFacing = true;
        }

        return true;
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

    private static float NormalizeDegrees(float degrees)
    {
        var normalized = degrees % 360f;
        return normalized < 0f ? normalized + 360f : normalized;
    }

    private static int DirectionFromRotation(float rotation)
    {
        if (float.IsNaN(rotation) || float.IsInfinity(rotation))
        {
            return 1;
        }

        var normalized = NormalizeDegrees(rotation);
        var adjusted = (normalized + 22.5f) % 360f;
        var index = (int)MathF.Floor(adjusted / 45f);
        return index + 1;
    }

    private static float DegreesFromDirection(int direction)
    {
        var index = NormalizeDirection(direction) - 1;
        return index * 45f;
    }

    private sealed class EditorPanel
    {
        private readonly GraphicsClientGame _game;
        private readonly Desktop _desktop;
        private readonly Window _window;
    private readonly TextBox _idField;
    private readonly TextBox _nameField;
    private readonly TextBox _displayNameField;
    private readonly TextBox _posXField;
    private readonly TextBox _posYField;
    private readonly TextBox _posZField;
    private readonly TextBox _rotationField;
    private readonly TextBox _scaleField;
    private readonly TextBox _textureField;
        private readonly Label _seedFileLabel;
        private MapObjectVisualization? _current;

        public EditorPanel(GraphicsClientGame game, Desktop desktop)
        {
            _game = game;
            _desktop = desktop;
            _window = BuildWindow();

            _idField = new TextBox();
            _nameField = new TextBox();
            _displayNameField = new TextBox();
            _posXField = new TextBox();
            _posYField = new TextBox();
            _posZField = new TextBox();
            _rotationField = new TextBox();
            _scaleField = new TextBox();
            _textureField = new TextBox();
            _seedFileLabel = new Label { Text = "Seed file: (unknown)" };

            _idField.Enabled = false;
            _nameField.Enabled = false;

            ComposeWindow();
        }

        public void Show()
        {
            if (_window.Parent == null)
            {
                _desktop.Widgets.Add(_window);
            }

            _window.Visible = true;
        }

        public void Hide()
        {
            _window.Visible = false;
        }

        public void Bind(MapObjectVisualization visualization, string? seedFilePath)
        {
            _current = visualization;

            var location = visualization.Snapshot.Location;
            _idField.Text = visualization.Snapshot.MapObjectId ?? string.Empty;
            _nameField.Text = visualization.Snapshot.Name ?? string.Empty;
            _displayNameField.Text = visualization.Snapshot.DisplayName ?? visualization.Snapshot.Name ?? string.Empty;

            if (location != null)
            {
                _posXField.Text = location.X.ToString("0.###", CultureInfo.InvariantCulture);
                _posYField.Text = location.Y.ToString("0.###", CultureInfo.InvariantCulture);
                _posZField.Text = location.Z.ToString("0.###", CultureInfo.InvariantCulture);
                _rotationField.Text = location.Rotation.ToString("0.##", CultureInfo.InvariantCulture);
            }
            else
            {
                _posXField.Text = _posYField.Text = _posZField.Text = _rotationField.Text = string.Empty;
            }

            _scaleField.Text = visualization.Scale.ToString("0.###", CultureInfo.InvariantCulture);
            _textureField.Text = visualization.TextureKey;
            SetSeedFile(seedFilePath);
            Show();
        }

        public void ClearSelection()
        {
            _current = null;
            _idField.Text = string.Empty;
            _nameField.Text = string.Empty;
            _displayNameField.Text = string.Empty;
            _posXField.Text = string.Empty;
            _posYField.Text = string.Empty;
            _posZField.Text = string.Empty;
            _rotationField.Text = string.Empty;
            _scaleField.Text = string.Empty;
            _textureField.Text = string.Empty;
            SetSeedFile(null);
        }

        public void SetSeedFile(string? path)
        {
            _seedFileLabel.Text = string.IsNullOrWhiteSpace(path)
                ? "Seed file: (unsaved)"
                : $"Seed file: {Path.GetFileName(path)}";
        }

        private Window BuildWindow()
        {
            return new Window
            {
                Title = "Map Object Editor",
                Width = 420,
                Height = 420,
                Visible = false,
                IsModal = false
            };
        }

        private void ComposeWindow()
        {
            var grid = new Grid
            {
                ColumnSpacing = 8,
                RowSpacing = 6
            };

            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));

            for (var i = 0; i < 9; i++)
            {
                grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            }

            grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            grid.RowsProportions.Add(new Proportion(ProportionType.Auto));

            AddRow(grid, 0, "Id", _idField);
            AddRow(grid, 1, "Name", _nameField);
            AddRow(grid, 2, "Display", _displayNameField);
            AddRow(grid, 3, "Pos X", _posXField);
            AddRow(grid, 4, "Pos Y", _posYField);
            AddRow(grid, 5, "Pos Z", _posZField);
            AddRow(grid, 6, "Rotation", _rotationField);
            AddRow(grid, 7, "Scale", _scaleField);
            AddRow(grid, 8, "Texture", _textureField);

            Grid.SetRow(_seedFileLabel, 9);
            Grid.SetColumnSpan(_seedFileLabel, 2);
            grid.Widgets.Add(_seedFileLabel);

            var buttonPanel = new HorizontalStackPanel
            {
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var applyButton = new Button { Content = new Label { Text = "Apply" } };
            applyButton.Click += (_, _) => ApplyChanges();

            var refreshButton = new Button { Content = new Label { Text = "Refresh" } };
            refreshButton.Click += (_, _) =>
            {
                if (_current != null)
                {
                    Bind(_current, _game.GetSeedFilePath(_current));
                }
            };

            buttonPanel.Widgets.Add(applyButton);
            buttonPanel.Widgets.Add(refreshButton);

            Grid.SetRow(buttonPanel, 10);
            Grid.SetColumnSpan(buttonPanel, 2);
            grid.Widgets.Add(buttonPanel);

            _window.Content = grid;
        }

        private void AddRow(Grid grid, int rowIndex, string label, Widget widget)
        {
            var rowLabel = new Label { Text = label };
            Grid.SetRow(rowLabel, rowIndex);
            Grid.SetColumn(rowLabel, 0);
            grid.Widgets.Add(rowLabel);

            Grid.SetRow(widget, rowIndex);
            Grid.SetColumn(widget, 1);
            grid.Widgets.Add(widget);
        }

        private void ApplyChanges()
        {
            if (_current == null)
            {
                _game.EnqueueLog("Editor: No map object selected.");
                return;
            }

            if (!double.TryParse(_posXField.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var posX) ||
                !double.TryParse(_posYField.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var posY) ||
                !double.TryParse(_posZField.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var posZ))
            {
                _game.EnqueueLog("Editor: Position values must be numeric.");
                return;
            }

            if (!float.TryParse(_rotationField.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var rotation))
            {
                _game.EnqueueLog("Editor: Rotation value must be numeric.");
                return;
            }

            if (!float.TryParse(_scaleField.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var scale))
            {
                _game.EnqueueLog("Editor: Scale value must be numeric.");
                return;
            }

            var textureKey = _textureField.Text?.Trim() ?? string.Empty;
            var displayName = _displayNameField.Text?.Trim();

            var worldPosition = new Vector3((float)posX, (float)posZ, (float)posY);
            if (_game.TryApplyMapObjectChanges(_current, worldPosition, rotation, scale, textureKey, displayName, out var seedPath, out var error))
            {
                _game.EnqueueLog("Editor: Map object updated.");
                Bind(_current, seedPath);
            }
            else if (!string.IsNullOrWhiteSpace(error))
            {
                _game.EnqueueLog($"Editor: {error}");
            }
        }
    }

    private sealed class TexturePickerWindow
    {
        private readonly GraphicsClientGame _game;
        private readonly IReadOnlyList<string> _textureKeys;
        private Window? _window;

        public TexturePickerWindow(GraphicsClientGame game, IReadOnlyList<string> textureKeys)
        {
            _game = game;
            _textureKeys = textureKeys;
        }

        public void Show(Desktop desktop)
        {
            if (_window == null)
            {
                Build(desktop);
            }

            if (_window!.Parent == null)
            {
                desktop.Widgets.Add(_window);
            }

            _window.Visible = true;
        }

        private void Build(Desktop desktop)
        {
            var buttonsPanel = new VerticalStackPanel { Spacing = 4 };
            foreach (var key in _textureKeys)
            {
                var button = new Button { Content = new Label { Text = key } };
                button.Click += (_, _) => SelectAndClose(key);
                buttonsPanel.Widgets.Add(button);
            }

            var cancelButton = new Button { Content = new Label { Text = "Cancel" } };
            cancelButton.Click += (_, _) =>
            {
                if (_window != null)
                {
                    _window.Visible = false;
                }
            };

            var root = new VerticalStackPanel
            {
                Spacing = 8
            };

            root.Widgets.Add(new Label { Text = "Select texture for new object" });
            root.Widgets.Add(buttonsPanel);
            root.Widgets.Add(cancelButton);

            _window = new Window
            {
                Title = "Texture Picker",
                Content = root,
                Width = 280,
                Height = 360,
                Visible = false
            };
        }

        private void SelectAndClose(string textureKey)
        {
            _game.BeginPlacementForTexture(textureKey);

            if (_window != null)
            {
                _window.Visible = false;
            }
        }
    }

    private sealed class SeedDataPersistence
    {
        private readonly string _mapObjectsDirectory;
        private readonly Dictionary<string, string> _pathById = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _pathByName = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _knownPaths = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _sync = new();

        private SeedDataPersistence(string mapObjectsDirectory)
        {
            _mapObjectsDirectory = mapObjectsDirectory;
            BuildIndex();
        }

        public static bool TryCreate(out SeedDataPersistence? persistence, out string? error)
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);

            while (current != null)
            {
                var candidate = Path.Combine(current.FullName, "RPG.WorldSeeder", "SeedData", "MapObjects");
                if (Directory.Exists(candidate))
                {
                    persistence = new SeedDataPersistence(candidate);
                    error = null;
                    return true;
                }

                current = current.Parent;
            }

            persistence = null;
            error = "Editor: Could not locate RPG.WorldSeeder/SeedData/MapObjects.";
            return false;
        }

        public string UpdateOrCreateMapObject(WorldMapObject mapObject, string textureKey, float scale)
        {
            lock (_sync)
            {
                var path = ResolveFilePath(mapObject);
                var json = BuildJson(mapObject, textureKey, scale);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, json);

                if (!string.IsNullOrWhiteSpace(mapObject.MapObjectId))
                {
                    _pathById[mapObject.MapObjectId] = path;
                }

                if (!string.IsNullOrWhiteSpace(mapObject.Name))
                {
                    _pathByName[NormalizeName(mapObject.Name)] = path;
                }

                if (!string.IsNullOrWhiteSpace(mapObject.DisplayName))
                {
                    _pathByName[NormalizeName(mapObject.DisplayName)] = path;
                }

                return path;
            }
        }

        public string? TryGetSeedPath(string mapObjectId, string? name)
        {
            lock (_sync)
            {
                if (!string.IsNullOrWhiteSpace(mapObjectId) && _pathById.TryGetValue(mapObjectId, out var byId))
                {
                    return byId;
                }

                if (!string.IsNullOrWhiteSpace(name))
                {
                    var normalized = NormalizeName(name);
                    if (!string.IsNullOrWhiteSpace(normalized) && _pathByName.TryGetValue(normalized, out var byName))
                    {
                        return byName;
                    }
                }

                return null;
            }
        }

        private void BuildIndex()
        {
            if (!Directory.Exists(_mapObjectsDirectory))
            {
                return;
            }

            foreach (var file in Directory.EnumerateFiles(_mapObjectsDirectory, "*.json", SearchOption.AllDirectories))
            {
                try
                {
                    var text = File.ReadAllText(file);
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        continue;
                    }

                    if (JsonNode.Parse(text) is not JsonObject json)
                    {
                        continue;
                    }

                    var id = json["id"]?.GetValue<string>();
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        _pathById[id] = file;
                    }

                    var name = json["name"]?.GetValue<string>();
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        _pathByName[NormalizeName(name)] = file;
                    }

                    _knownPaths.Add(file);
                }
                catch
                {
                    // ignore malformed files
                }
            }
        }

        private string ResolveFilePath(WorldMapObject mapObject)
        {
            if (!string.IsNullOrWhiteSpace(mapObject.MapObjectId) && _pathById.TryGetValue(mapObject.MapObjectId, out var existing))
            {
                return existing;
            }

            if (string.IsNullOrWhiteSpace(mapObject.MapObjectId))
            {
                mapObject.MapObjectId = Guid.NewGuid().ToString();
            }

            var baseName = !string.IsNullOrWhiteSpace(mapObject.Name)
                ? mapObject.Name!
                : !string.IsNullOrWhiteSpace(mapObject.DisplayName)
                    ? mapObject.DisplayName!
                    : "map-object";

            var slug = NormalizeName(baseName);
            if (string.IsNullOrEmpty(slug))
            {
                slug = "map-object";
            }

            var fileName = slug + ".json";
            var path = Path.Combine(_mapObjectsDirectory, fileName);
            var counter = 1;

            while (_knownPaths.Contains(path))
            {
                counter++;
                fileName = $"{slug}-{counter}.json";
                path = Path.Combine(_mapObjectsDirectory, fileName);
            }

            _knownPaths.Add(path);
            _pathById[mapObject.MapObjectId] = path;

            if (!string.IsNullOrWhiteSpace(mapObject.Name))
            {
                _pathByName[NormalizeName(mapObject.Name)] = path;
            }

            if (!string.IsNullOrWhiteSpace(mapObject.DisplayName))
            {
                _pathByName[NormalizeName(mapObject.DisplayName)] = path;
            }

            return path;
        }

        private static string BuildJson(WorldMapObject mapObject, string textureKey, float scale)
        {
            var location = mapObject.Location ?? new Location();
            mapObject.Location = location;

            var position = new JsonObject
            {
                ["x"] = location.X,
                ["y"] = location.Y,
                ["z"] = location.Z
            };

            var locationNode = new JsonObject
            {
                ["position"] = position,
                ["mapId"] = location.MapId ?? string.Empty,
                ["zoneName"] = location.ZoneName ?? string.Empty,
                ["worldId"] = location.WorldId ?? string.Empty,
                ["rotation"] = location.Rotation
            };

            var tagsArray = new JsonArray();
            foreach (var tag in mapObject.Tags)
            {
                tagsArray.Add(tag);
            }

            var stateNode = new JsonObject();
            foreach (var entry in mapObject.State)
            {
                stateNode[entry.Key] = entry.Value;
            }

            stateNode["textureKey"] = textureKey;
            stateNode["billboardScale"] = scale.ToString(CultureInfo.InvariantCulture);

            var root = new JsonObject
            {
                ["id"] = mapObject.MapObjectId ?? Guid.NewGuid().ToString(),
                ["name"] = mapObject.Name ?? string.Empty,
                ["displayName"] = mapObject.DisplayName ?? mapObject.Name ?? string.Empty,
                ["location"] = locationNode,
                ["worldId"] = location.WorldId ?? string.Empty,
                ["zoneId"] = location.ZoneName ?? string.Empty,
                ["isActive"] = mapObject.IsActive,
                ["rotationYaw"] = location.Rotation,
                ["tags"] = tagsArray,
                ["state"] = stateNode,
                ["lastUpdated"] = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
            };

            return root.ToJsonString(new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }

        private static string NormalizeName(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var lowered = value.ToLowerInvariant();
            var normalized = Regex.Replace(lowered, "[^a-z0-9]+", "-");
            normalized = normalized.Trim('-');
            return normalized;
        }
    }

    private sealed class MapObjectVisualization
    {
        public MapObjectVisualization(WorldMapObject snapshot, RenderEntity render, Texture2D? fallbackTexture, string textureKey, float baseSize, float scale)
        {
            Snapshot = snapshot;
            Render = render;
            FallbackTexture = fallbackTexture;
            TextureKey = textureKey;
            BaseSize = baseSize;
            Scale = scale;
        }

        public WorldMapObject Snapshot { get; }
        public RenderEntity Render { get; set; }
        public Texture2D? FallbackTexture { get; }
        public string TextureKey { get; set; }
        public float BaseSize { get; set; }
        public float Scale { get; set; }
    }

    private sealed class MapObjectOverride
    {
        public Vector3? Position { get; set; }
        public float? Scale { get; set; }
        public float? RotationDegrees { get; set; }
    }

    private sealed class PendingMapObjectCreation
    {
        public PendingMapObjectCreation(string textureKey)
        {
            TextureKey = textureKey;
        }

        public string TextureKey { get; }
    }
}
