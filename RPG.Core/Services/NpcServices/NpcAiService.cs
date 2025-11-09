using System.Collections.Concurrent;
using System.Numerics;
using RPG.AI.Core;
using RPG.AI.Directives;
using RPG.AI.Utility;
using RPG.AI.Utility.Actions;
using RPG.AI.Models;
using RPG.Core.Interfaces;
using RPG.Core.Interfaces.NpcServices;
using RPG.Abstractions.Interfaces;
using RPG.Domain.Entities;
using RPG.Domain.Entities.Npcs;
using RPG.Domain.Entities.Npcs.NpcComponents;
using RPG.Domain.Entities.Skills;
using RPG.Domain.Enums;
using RPG.Domain.Models;
using RPG.Infrastructure.Interfaces;

namespace RPG.Core.Services.NpcServices;

public sealed class NpcAiService : INpcAiService
{
    private static readonly Dictionary<string, string> BehaviorAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["aggressive-champion"] = "boss",
        ["aggressive-overdrive"] = "boss",
        ["aggressive"] = "aggressive-melee",
        ["aggressive-basic"] = "aggressive-melee",
        ["aggressive-melee"] = "aggressive-melee",
        ["hostile"] = "aggressive-melee",
        ["hostile-melee"] = "aggressive-melee",
        ["caster-basic"] = "caster",
        ["boss-basic"] = "boss",
        ["friendly-shopkeeper"] = "friendly-merchant",
        ["friendly-greeter"] = "friendly",
        ["friendly-idle"] = "friendly",
        ["quest-giver"] = "friendly-questgiver",
        ["questgiver"] = "friendly-questgiver"
    };

    private const float DefaultDeltaTimeSeconds = 1f;
    private const float ThreatDecayPerSecond = 12f;
    private const float ThreatProximityWeight = 60f;
    private const float ThreatTargetBonus = 25f;
    private const float ThreatMovementBonus = 10f;
    private static readonly TimeSpan ThreatMemoryWindow = TimeSpan.FromSeconds(15);

    private readonly IDocumentRepository _documentRepository;
    private readonly IMovementService _movementService;
    private readonly ICharacterStateBroadcaster _stateBroadcaster;
    private readonly INpcCombatService _combatService;
    private readonly ILogger<NpcAiService> _logger;
    private readonly UtilityAgentSettings _settings;
    private readonly ConcurrentDictionary<Guid, Npc> _npcs = new();
    private readonly ConcurrentDictionary<Guid, UtilityAgent> _agents = new();
    private readonly ConcurrentDictionary<Guid, AiContext> _contexts = new();
    private readonly ConcurrentDictionary<Guid, NpcStateSnapshot> _snapshots = new();
    private readonly ConcurrentDictionary<Guid, Dictionary<Guid, ThreatEntry>> _threatTables = new();
    private readonly SemaphoreSlim _tickGate = new(1, 1);
    private IReadOnlyList<AiEvaluationResult> _lastEvaluations = Array.Empty<AiEvaluationResult>();

    public NpcAiService(
        IDocumentRepository documentRepository,
        IMovementService movementService,
    ICharacterStateBroadcaster stateBroadcaster,
        INpcCombatService combatService,
        ILogger<NpcAiService> logger)
    {
        _documentRepository = documentRepository;
        _movementService = movementService;
        _stateBroadcaster = stateBroadcaster;
        _combatService = combatService;
        _logger = logger;
        _settings = UtilityAgentSettings.Default;
    }

    public async Task<IReadOnlyList<AiEvaluationResult>> TickAsync(CancellationToken cancellationToken = default)
    {
        await _tickGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureNpcCacheAsync(cancellationToken).ConfigureAwait(false);

            var players = GetActivePlayers();
            var playerLookup = players.ToDictionary(p => p.Id, p => p);

            var evaluations = new List<AiEvaluationResult>(_npcs.Count);
            foreach (var npc in _npcs.Values)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var context = PrepareContext(npc, players);
                var agent = _agents.GetOrAdd(npc.Id, _ => CreateAgentFor(npc));
                var decision = agent.Decide(context);
                var directives = context.Directives.ToArray();
                var executionLog = await ExecuteDirectivesAsync(npc, context, directives, playerLookup, cancellationToken).ConfigureAwait(false);

                UpdateNpcSnapshot(npc);
                _npcs[npc.Id] = npc;

                evaluations.Add(new AiEvaluationResult(npc, agent, context, decision, directives, executionLog));
            }

            _lastEvaluations = evaluations.ToArray();
            return _lastEvaluations;
        }
        catch (Exception ex)
        {
            _logger.Error( "NPC AI tick failed.", ex);
            throw;
        }
        finally
        {
            _tickGate.Release();
        }
    }

    public IReadOnlyCollection<NpcStateSnapshot> GetNpcSnapshots()
    {
        return _snapshots.Values.ToList();
    }

    public IReadOnlyCollection<AiEvaluationResult> GetLastEvaluations()
    {
        return _lastEvaluations;
    }

    private async Task EnsureNpcCacheAsync(CancellationToken cancellationToken)
    {
        if (!_npcs.IsEmpty)
        {
            return;
        }

        var npcs = await _documentRepository.GetAllAsync<Npc>(cancellationToken).ConfigureAwait(false);
        foreach (var npc in npcs)
        {
            _npcs[npc.Id] = npc;
        }

        if (npcs.Count == 0)
        {
            _logger.Info("NPC cache is empty. No NPCs loaded for AI evaluation.");
        }
        else
        {
            _logger.Info($"Loaded {npcs.Count} NPCs for AI evaluation.");
        }
    }

    private List<Character> GetActivePlayers()
    {
        var snapshots = _stateBroadcaster.GetSnapshots();
        var players = new List<Character>(snapshots.Count);

        foreach (var snapshot in snapshots)
        {
            var character = CreateCharacterFromSnapshot(snapshot);
            players.Add(character);
        }

        return players;
    }

    private AiContext PrepareContext(Npc npc, IReadOnlyList<Character> players)
    {
        var context = _contexts.GetOrAdd(npc.Id, _ => new AiContext());

        context.Self = npc;
        context.CurrentHealth = npc.CurrentHealth;
        context.MaxHealth = npc.MaxHealth;

        context.NearbyPlayers.Clear();
        if (npc.CurrentLocation?.Position is { } npcPosition)
        {
            var detectionRadius = ResolveDetectionRadius(npc);
            var detectionRadiusSquared = detectionRadius * detectionRadius;

            foreach (var player in players)
            {
                var playerPosition = player.CurrentLocation?.Position ?? Vector3.Zero;
                var distanceSquared = Vector3.DistanceSquared(npcPosition, playerPosition);
                if (distanceSquared <= detectionRadiusSquared)
                {
                    context.NearbyPlayers.Add(player);
                }
            }
        }

        context.NearbyNpcs.Clear();
        foreach (var other in _npcs.Values)
        {
            if (other.Id == npc.Id)
            {
                continue;
            }

            context.NearbyNpcs.Add(other);
        }

        if (context.Target is { } existingTarget)
        {
            var stillNearby = context.NearbyPlayers.FirstOrDefault(p => p.Id == existingTarget.Id);
            context.Target = stillNearby;
        }
        else if (context.Blackboard.TryGetValue("targetId", out var storedObj) && storedObj is Guid storedId)
        {
            var candidate = context.NearbyPlayers.FirstOrDefault(p => p.Id == storedId);
            if (candidate != null)
            {
                context.Target = candidate;
            }
            else
            {
                context.Blackboard.Remove("targetId");
            }
        }

        context.Directives.Clear();
        CleanupCooldowns(context);
        context.UpdateDistanceToTarget();
        UpdateThreatContext(npc, context);

        if (context.Target == null && context.ThreatTable.Count > 0)
        {
            var topThreatId = context.ThreatTable.OrderByDescending(pair => pair.Value.Score).First().Key;
            var fallbackTarget = context.NearbyPlayers.FirstOrDefault(p => p.Id == topThreatId);
            if (fallbackTarget != null)
            {
                context.Target = fallbackTarget;
            }
        }

        return context;
    }

    private void UpdateThreatContext(Npc npc, AiContext context)
    {
        var table = _threatTables.GetOrAdd(npc.Id, _ => new Dictionary<Guid, ThreatEntry>());
        var now = DateTime.UtcNow;
        var detectionRadius = Math.Max(ResolveDetectionRadius(npc), 1f);

        var staleKeys = new List<Guid>();
        foreach (var pair in table)
        {
            var elapsed = (float)(now - pair.Value.LastSeenUtc).TotalSeconds;
            if (elapsed > 0f)
            {
                pair.Value.Score = MathF.Max(0f, pair.Value.Score - ThreatDecayPerSecond * elapsed);
            }

            if (pair.Value.Score <= 0f || now - pair.Value.LastSeenUtc > ThreatMemoryWindow)
            {
                staleKeys.Add(pair.Key);
            }
        }

        foreach (var key in staleKeys)
        {
            table.Remove(key);
        }

        foreach (var player in context.NearbyPlayers)
        {
            var distance = context.CalculateDistanceTo(player);
            if (float.IsInfinity(distance))
            {
                continue;
            }

            var clampedDistance = Math.Clamp(distance, 0f, detectionRadius);
            var proximityFactor = 1f - (clampedDistance / detectionRadius);
            var baseScore = MathF.Max(5f, proximityFactor * ThreatProximityWeight);

            if (context.Target?.Id == player.Id)
            {
                baseScore += ThreatTargetBonus;
            }

            if (player.IsMoving)
            {
                baseScore += ThreatMovementBonus;
            }

            if (table.TryGetValue(player.Id, out var entry))
            {
                entry.Score = MathF.Max(baseScore, entry.Score + baseScore * 0.5f);
                entry.Distance = distance;
                entry.LastSeenUtc = now;
            }
            else
            {
                table[player.Id] = new ThreatEntry
                {
                    Score = baseScore,
                    Distance = distance,
                    LastSeenUtc = now
                };
            }
        }

        context.ThreatTable.Clear();
        foreach (var pair in table.OrderByDescending(p => p.Value.Score))
        {
            context.ThreatTable[pair.Key] = new ThreatInfo(pair.Key, pair.Value.Score, pair.Value.Distance, pair.Value.LastSeenUtc);
        }

        if (context.ThreatTable.Count > 0 || context.Target != null)
        {
            var primary = context.ThreatTable.Count > 0
                ? context.ThreatTable.Values.OrderByDescending(t => t.Score).First()
                : null;

            if (primary != null)
            {
                context.Blackboard["primaryThreatId"] = primary.CharacterId;
                context.Blackboard["primaryThreatScore"] = primary.Score;
            }

            context.IsInCombat = true;
            context.CombatStartTime ??= now;
        }
        else
        {
            context.Blackboard.Remove("primaryThreatId");
            context.Blackboard.Remove("primaryThreatScore");
            context.IsInCombat = false;
            context.CombatStartTime = null;
        }
    }

    private UtilityAgent CreateAgentFor(Npc npc)
    {
        var combat = npc.Components.OfType<CombatComponent>().FirstOrDefault();
        var script = ResolveBehaviorScript(npc, combat);
        var skills = BuildSkillLookup(combat);

        try
        {
            var agent = UtilityAgentFactory.GetByName(script, skills, _settings);
            if (agent != null)
            {
                return agent;
            }
        }
        catch (ArgumentException ex)
        {
            _logger.Error($"Failed to create utility agent for NPC {npc.Id} using script '{script}'.", ex);
        }

        if (combat != null && skills.Values.FirstOrDefault() is { } primarySkill)
        {
            _logger.Info($"Falling back to basic combat utility agent for NPC {npc.Id}");
            return new UtilityAgent("fallback-combat")
                .Register(UtilityActionCatalog.UseSkill(
                    "fallback-attack",
                    primarySkill,
                    _settings.MeleeRange,
                    _settings.MeleeMaxRange,
                    _settings.BasicAttackCooldown,
                    weight: 4f))
                .Register(UtilityActionCatalog.FollowTarget(
                    "fallback-follow",
                    _settings.MeleeRange,
                    _settings.MeleeStopDistance,
                    _settings.ChaseRange,
                    weight: 2f))
                .Register(UtilityActionCatalog.Idle("fallback-idle", _settings.IdleAnimation, weight: 0.3f));
        }

        _logger.Info($"Using idle fallback utility agent for NPC {npc.Id}");
        return new UtilityAgent("fallback-idle")
            .Register(UtilityActionCatalog.Idle("idle", _settings.IdleAnimation, weight: 1f));
    }

    private static IDictionary<string, Skill> BuildSkillLookup(CombatComponent? combat)
    {
        if (combat == null)
        {
            return new Dictionary<string, Skill>(StringComparer.OrdinalIgnoreCase);
        }

        var lookup = new Dictionary<string, Skill>(StringComparer.OrdinalIgnoreCase);
        foreach (var skill in combat.GetSkillsContainer().Skills.Keys)
        {
            foreach (var key in EnumerateSkillKeys(skill))
            {
                lookup.TryAdd(key, skill);
            }
        }

        return lookup;
    }

    private static IEnumerable<string> EnumerateSkillKeys(Skill skill)
    {
        if (!string.IsNullOrWhiteSpace(skill.Name))
        {
            yield return ToKey(skill.Name);
        }

        if (skill.Tags is { Count: > 0 })
        {
            foreach (var tag in skill.Tags)
            {
                if (!string.IsNullOrWhiteSpace(tag))
                {
                    yield return ToKey(tag);
                }
            }
        }

        yield return skill.Id.ToString("N");
    }

    private static string ToKey(string value)
    {
        return value
            .Trim()
            .Replace("::", ":", StringComparison.Ordinal)
            .Replace(':', '-')
            .Replace(' ', '-')
            .ToLowerInvariant();
    }

    private string ResolveBehaviorScript(Npc npc, CombatComponent? combat)
    {
        var script = combat?.AiBehaviorScript;
        if (!string.IsNullOrWhiteSpace(script))
        {
            var normalized = script.Trim().ToLowerInvariant();
            if (BehaviorAliases.TryGetValue(normalized, out var alias))
            {
                return alias;
            }

            if (normalized.Contains("boss", StringComparison.Ordinal))
            {
                return "boss";
            }

            if (normalized.Contains("caster", StringComparison.Ordinal))
            {
                return "caster";
            }

            if (normalized.Contains("healer", StringComparison.Ordinal))
            {
                return "defensive-healer";
            }

            if (normalized.Contains("merchant", StringComparison.Ordinal))
            {
                return "friendly-merchant";
            }

            if (normalized.Contains("quest", StringComparison.Ordinal))
            {
                return "friendly-questgiver";
            }

            if (normalized.Contains("friendly", StringComparison.Ordinal))
            {
                return "friendly";
            }

            if (normalized.Contains("aggressive", StringComparison.Ordinal) || normalized.Contains("hostile", StringComparison.Ordinal))
            {
                return "aggressive-melee";
            }
        }

        if (npc.Tags.Contains("merchant"))
        {
            return "friendly-merchant";
        }

        if (npc.Tags.Any(tag => tag.Contains("quest", StringComparison.OrdinalIgnoreCase)))
        {
            return "friendly-questgiver";
        }

        if (npc.Tags.Contains("boss"))
        {
            return "boss";
        }

        if (npc.Tags.Contains("hostile"))
        {
            return "aggressive-melee";
        }

        return "friendly";
    }

    private async Task<IReadOnlyList<string>> ExecuteDirectivesAsync(
        Npc npc,
        AiContext context,
        IReadOnlyList<AiDirective> directives,
        IDictionary<Guid, Character> playerLookup,
        CancellationToken cancellationToken)
    {
        var log = new List<string>(directives.Count);

        foreach (var directive in directives)
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (directive.Type)
            {
                case AiDirectiveType.MoveToLocation:
                    ExecuteMoveTo(npc, directive, log);
                    break;

                case AiDirectiveType.FollowTarget:
                    ExecuteFollowTarget(npc, context, directive, playerLookup, log);
                    break;

                case AiDirectiveType.StopMovement:
                    ExecuteStopMovement(npc, log);
                    break;

                case AiDirectiveType.UseSkill:
                    await ExecuteUseSkillAsync(npc, context, directive, playerLookup, log, cancellationToken).ConfigureAwait(false);
                    break;

                case AiDirectiveType.Idle:
                    ExecuteIdle(npc, log);
                    break;

                default:
                    log.Add($"No runtime handler for directive '{directive.Type}'.");
                    break;
            }
        }

        if (context.Target != null)
        {
            context.Blackboard["targetId"] = context.Target.Id;
        }
        else
        {
            context.Blackboard.Remove("targetId");
        }

        return log;
    }

    private void ExecuteMoveTo(Npc npc, AiDirective directive, ICollection<string> log)
    {
        if (directive.Destination?.Position is not { } destination)
        {
            log.Add("MoveTo directive missing destination.");
            return;
        }

        var current = npc.CurrentLocation?.Position ?? Vector3.Zero;
        var delta = destination - current;
        if (delta.LengthSquared() <= 0.0001f)
        {
            _movementService.Stop(npc);
            log.Add("NPC already at destination; stopping movement.");
            return;
        }

        var distance = delta.Length();
        var stopDistance = directive.StopDistance ?? 0.5f;
        if (distance <= stopDistance)
        {
            _movementService.Stop(npc);
            log.Add($"Within stop distance ({stopDistance:0.00}); stopping movement.");
            return;
        }

        var direction = delta / distance;
        var result = _movementService.Move(npc, direction, DefaultDeltaTimeSeconds);
        if (!result.Success)
        {
            log.Add($"MoveTo failed: {result.Message ?? result.Error.Message}.");
        }
        else
        {
            log.Add($"Moving towards destination (remaining {distance:0.00}).");
        }
    }

    private void ExecuteFollowTarget(
        Npc npc,
        AiContext context,
        AiDirective directive,
        IDictionary<Guid, Character> playerLookup,
        ICollection<string> log)
    {
        if (directive.TargetId is not Guid targetId)
        {
            log.Add("FollowTarget directive missing target identifier.");
            _movementService.Stop(npc);
            context.Target = null;
            return;
        }

        var target = context.Target;
        if (target == null || target.Id != targetId)
        {
            playerLookup.TryGetValue(targetId, out target);
            context.Target = target;
        }

        if (target == null)
        {
            log.Add($"Target {targetId} not found; stopping movement.");
            _movementService.Stop(npc);
            return;
        }

        var npcPosition = npc.CurrentLocation?.Position ?? Vector3.Zero;
        var targetPosition = target.CurrentLocation?.Position ?? Vector3.Zero;
        var delta = targetPosition - npcPosition;
        var distance = delta.Length();
        var stopDistance = directive.StopDistance ?? 1.5f;

        if (distance <= stopDistance)
        {
            _movementService.Stop(npc);
            log.Add($"Within follow range ({distance:0.00}); stopping movement.");
            return;
        }

        if (directive.Metadata != null &&
            directive.Metadata.TryGetValue("maxRange", out var maxRangeObj) &&
            TryGetFloat(maxRangeObj, out var maxRange) &&
            distance > maxRange)
        {
            _movementService.Stop(npc);
            log.Add($"Exceeded chase range ({distance:0.00} > {maxRange:0.00}); stopping.");
            context.Target = null;
            return;
        }

        var direction = delta / distance;
        var result = _movementService.Move(npc, direction, DefaultDeltaTimeSeconds);
        if (!result.Success)
        {
            log.Add($"FollowTarget failed: {result.Message ?? result.Error.Message}.");
        }
        else
        {
            log.Add($"Chasing target {targetId} (distance {distance:0.00}).");
        }
    }

    private void ExecuteStopMovement(Npc npc, ICollection<string> log)
    {
        var result = _movementService.Stop(npc);
        if (!result.Success)
        {
            log.Add($"StopMovement failed: {result.Message ?? result.Error.Message}.");
        }
        else
        {
            log.Add("Stopped movement.");
        }
    }

    private static void ExecuteIdle(Npc npc, ICollection<string> log)
    {
        npc.SetMovementState(false);
        log.Add("Idling in place.");
    }

    private async Task ExecuteUseSkillAsync(
        Npc npc,
        AiContext context,
        AiDirective directive,
        IDictionary<Guid, Character> playerLookup,
        ICollection<string> log,
        CancellationToken cancellationToken)
    {
        var combat = npc.Components.OfType<CombatComponent>().FirstOrDefault();
        if (combat == null)
        {
            log.Add("UseSkill directive skipped: NPC lacks a combat component.");
            return;
        }

        var skills = BuildSkillLookup(combat);
        Skill? skill = null;
        Guid? skillId = null;
        string skillName = "unknown-skill";

        if (directive.Metadata != null)
        {
            if (directive.Metadata.TryGetValue("skillId", out var idObj) && TryGetGuid(idObj, out var parsedId))
            {
                skillId = parsedId;
                skills.TryGetValue(parsedId.ToString("N"), out skill);
            }

            if (directive.Metadata.TryGetValue("skillName", out var nameObj) && nameObj is string metadataName)
            {
                skillName = metadataName;
                skill ??= skills.TryGetValue(ToKey(metadataName), out var resolvedByName) ? resolvedByName : null;
            }
        }

        skill ??= skills.Values.FirstOrDefault();

        if (skill == null)
        {
            log.Add("UseSkill directive skipped: no matching skill available.");
            return;
        }

        skillName = string.IsNullOrWhiteSpace(skillName) ? skill.Name : skillName;
        var targetId = directive.TargetId;

        if (targetId is Guid typedTargetId)
        {
            if (!playerLookup.TryGetValue(typedTargetId, out var target))
            {
                log.Add($"UseSkill target {typedTargetId} not found in active players.");
            }
            else
            {
                context.Target = target;
            }
        }

        npc.SetMovementState(false);

        await _combatService.HandleSkillUsageAsync(npc, skill, targetId, cancellationToken).ConfigureAwait(false);

        if (targetId is Guid threatId)
        {
            var distance = context.Target != null && context.Target.Id == threatId
                ? context.DistanceToTarget
                : (playerLookup.TryGetValue(threatId, out var targetCharacter)
                    ? context.CalculateDistanceTo(targetCharacter)
                    : float.PositiveInfinity);

            var boostedEntry = BoostThreat(npc.Id, threatId, ThreatTargetBonus, distance);

            if (!float.IsInfinity(boostedEntry.Distance))
            {
                context.ThreatTable[threatId] = new ThreatInfo(threatId, boostedEntry.Score, boostedEntry.Distance, boostedEntry.LastSeenUtc);
            }
        }

        context.IsInCombat = true;
        context.CombatStartTime ??= DateTime.UtcNow;

        if (skillId == null)
        {
            skillId = skill.Id;
        }

        var targetDescription = targetId is Guid guid ? guid.ToString() : "no-target";
        var skillIdText = skillId?.ToString() ?? skill.Id.ToString("N");
        log.Add($"Using skill '{skillName}' ({skillIdText}) targeting {targetDescription}.");
    }

    private ThreatEntry BoostThreat(Guid npcId, Guid targetId, float bonus, float? distance = null)
    {
        var table = _threatTables.GetOrAdd(npcId, _ => new Dictionary<Guid, ThreatEntry>());
        var now = DateTime.UtcNow;

        if (table.TryGetValue(targetId, out var entry))
        {
            entry.Score += bonus;
            if (distance.HasValue && !float.IsInfinity(distance.Value))
            {
                entry.Distance = distance.Value;
            }

            entry.LastSeenUtc = now;
            return entry;
        }

        var created = new ThreatEntry
        {
            Score = bonus,
            Distance = distance ?? float.PositiveInfinity,
            LastSeenUtc = now
        };

        table[targetId] = created;
        return created;
    }

    private void UpdateNpcSnapshot(Npc npc)
    {
        var location = CloneLocation(npc.CurrentLocation);
        var snapshot = new NpcStateSnapshot(
            npc.Id,
            string.IsNullOrWhiteSpace(npc.DisplayName) ? npc.Name : npc.DisplayName,
            location,
            npc.IsMoving,
            npc.IsRotating,
            location.Rotation,
            DateTime.UtcNow);

        _snapshots[npc.Id] = snapshot;
    }

    private static void CleanupCooldowns(AiContext context)
    {
        if (context.SkillCooldowns.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var expired = context.SkillCooldowns
            .Where(pair => pair.Value <= now)
            .Select(pair => pair.Key)
            .ToArray();

        foreach (var skillId in expired)
        {
            context.SkillCooldowns.Remove(skillId);
        }
    }

    private static float ResolveDetectionRadius(Npc npc)
    {
        var combat = npc.Components.OfType<CombatComponent>().FirstOrDefault();
        if (combat?.AggroRange > 0)
        {
            return combat.AggroRange;
        }

        return UtilityAgentSettings.Default.AggroRadius;
    }

    private static Character CreateCharacterFromSnapshot(CharacterStateSnapshot snapshot)
    {
        var character = new Character(Guid.Empty, CharacterClass.Warrior)
        {
            Id = snapshot.CharacterId,
            Name = $"Player-{snapshot.CharacterId.ToString()[..8]}"
        };

        character.SetCurrentLocation(CloneLocation(snapshot.Location));
        character.SetMovementState(snapshot.IsMoving);
        character.SetRotationState(snapshot.IsRotating);
        character.CurrentHealth = 0;
        character.MaxHealth = 0;

        return character;
    }

    private static Location CloneLocation(Location? source)
    {
        if (source == null)
        {
            return new Location();
        }

        return new Location
        {
            Position = source.Position,
            Rotation = source.Rotation,
            MapId = source.MapId,
            ZoneName = source.ZoneName,
            WorldId = source.WorldId
        };
    }

    private static bool TryGetFloat(object? value, out float result)
    {
        switch (value)
        {
            case null:
                result = 0;
                return false;
            case float f:
                result = f;
                return true;
            case double d:
                result = (float)d;
                return true;
            case int i:
                result = i;
                return true;
            case long l:
                result = l;
                return true;
            case string s when float.TryParse(s, out var parsed):
                result = parsed;
                return true;
            default:
                result = 0;
                return false;
        }
    }

    private static bool TryGetGuid(object? value, out Guid result)
    {
        switch (value)
        {
            case null:
                result = Guid.Empty;
                return false;
            case Guid guid:
                result = guid;
                return true;
            case string s when Guid.TryParse(s, out var parsed):
                result = parsed;
                return true;
            default:
                result = Guid.Empty;
                return false;
        }
    }

    private sealed class ThreatEntry
    {
        public float Score { get; set; }
        public float Distance { get; set; }
        public DateTime LastSeenUtc { get; set; }
    }
}
