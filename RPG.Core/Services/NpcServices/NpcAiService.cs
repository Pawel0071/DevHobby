using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Numerics;
using RPG.AI.Core;
using RPG.AI.Directives;
using RPG.AI.Utility;
using RPG.AI.Utility.Actions;
using RPG.AI.Models;
using RPG.Core.Interfaces;
using RPG.Core.Interfaces.NpcServices;
using RPG.Abstractions.Interfaces;
using RPG.Abstractions.SharedModel;
using RPG.Domain.Enums;
using RPG.Domain.Models;
using RPG.Domain.Models.Interaction;
using RPG.Domain.Models.Items;
using RPG.Domain.Models.Npcs;
using RPG.Domain.Models.Npcs.NpcComponents;
using RPG.Domain.Models.Skills;
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

    private readonly IModelRepository _modelRepository;
    private readonly IMovementService _movementService;
    private readonly ICharacterStateBroadcaster _stateBroadcaster;
    private readonly INpcCombatService _combatService;
    private readonly IRabbitMqPublisher _publisher;
    private readonly ILogger<NpcAiService> _logger;
    private readonly UtilityAgentSettings _settings;
    private readonly ConcurrentDictionary<Guid, Npc> _npcs = new();
    private readonly ConcurrentDictionary<Guid, UtilityAgent> _agents = new();
    private readonly ConcurrentDictionary<Guid, AiContext> _contexts = new();
    private readonly ConcurrentDictionary<Guid, NpcStateSnapshot> _snapshots = new();
    private readonly ConcurrentDictionary<Guid, Dictionary<Guid, ThreatEntry>> _threatTables = new();
    private readonly SemaphoreSlim _tickGate = new(1, 1);
    private IReadOnlyList<AiEvaluationResult> _lastEvaluations = Array.Empty<AiEvaluationResult>();
    private readonly IGameStateBroadcaster _gameStateBroadcaster;
    private readonly IBehaviorRegistry _behaviorRegistry;
    private readonly IAiDirectiveEventAdapter _aiDirectiveAdapter;

    public NpcAiService(
        IModelRepository modelRepository,
        IMovementService movementService,
        ICharacterStateBroadcaster stateBroadcaster,
        INpcCombatService combatService,
        IRabbitMqPublisher publisher,
        ILogger<NpcAiService> logger,
        IGameStateBroadcaster gameStateBroadcaster,
        IBehaviorRegistry behaviorRegistry,
        IAiDirectiveEventAdapter aiDirectiveAdapter)
    {
        _modelRepository = modelRepository;
        _movementService = movementService;
        _stateBroadcaster = stateBroadcaster;
        _combatService = combatService;
        _publisher = publisher;
        _logger = logger;
        _settings = UtilityAgentSettings.Default;
        _gameStateBroadcaster = gameStateBroadcaster;
        _behaviorRegistry = behaviorRegistry;
        _aiDirectiveAdapter = aiDirectiveAdapter ?? throw new ArgumentNullException(nameof(aiDirectiveAdapter));
    }

    public async Task<IReadOnlyList<AiEvaluationResult>> TickAsync(CancellationToken cancellationToken = default)
    {
        await _tickGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureNpcCacheAsync(cancellationToken).ConfigureAwait(false);

            var players = GetActivePlayers();
            var evaluations = new List<AiEvaluationResult>(_npcs.Count);

            foreach (var npc in _npcs.Values)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var context = PrepareContext(npc, players);
                var agent = _behaviorRegistry.GetOrCreateAgent(npc);
                var decision = agent.Decide(context);

                // Publikuj dyrektywy jako requested events przez adapter
                var sequence = new AiDirectiveSequence(context.Directives.ToArray());
                var publishResult = await _aiDirectiveAdapter
                    .PublishSequenceAsync(npc, sequence, context, DirectivePublishOptions.Default, cancellationToken)
                    .ConfigureAwait(false);

                var executionLog = publishResult.Succeeded
                    ? context.Directives.Select(d => $"published:{d.Type}").ToList()
                    : new List<string> { $"publish-failed:{publishResult.FailureReason}" };

                UpdateNpcSnapshot(npc);
                _npcs[npc.Id] = npc;

                // Delta broadcast (lokacja, stan itp.)
                if (npc.CurrentLocation != null)
                {
                    var delta = new GameDeltaUpdate
                    {
                        WorldId = npc.WorldId,
                        NpcChanges = new[]
                        {
                            new NpcDelta
                            {
                                NpcId = npc.Id,
                                Location = npc.CurrentLocation,
                                IsAlive = npc.IsAlive
                            }
                        }
                    };
                    await _gameStateBroadcaster.BroadcastDeltaAsync(delta, cancellationToken).ConfigureAwait(false);
                }

                evaluations.Add(new AiEvaluationResult(npc, agent, context, decision, context.Directives.ToArray(), executionLog));
            }

            _lastEvaluations = evaluations.ToArray();
            return _lastEvaluations;
        }
        catch (Exception ex)
        {
            _logger.Error("NPC AI tick failed.", ex);
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

    public void RegisterExternalThreat(Guid npcId, Guid characterId, float threatAmount, float? distance = null)
    {
        if (npcId == Guid.Empty || characterId == Guid.Empty)
        {
            return;
        }

        if (threatAmount <= 0f)
        {
            return;
        }

        var entry = BoostThreat(npcId, characterId, threatAmount, distance);
        _logger.Debug($"Registered external threat {entry.Score:0.##} for NPC {npcId} from character {characterId}.");
    }

    private async Task EnsureNpcCacheAsync(CancellationToken cancellationToken)
    {
        if (!_npcs.IsEmpty)
        {
            return;
        }

        var npcs = await _modelRepository.GetAllAsync<Npc>(cancellationToken).ConfigureAwait(false);
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
                    await ExecuteMoveToAsync(npc, context, directive, log, cancellationToken).ConfigureAwait(false);
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

                case AiDirectiveType.BeginDialogue:
                    await ExecuteBeginDialogueAsync(npc, context, directive, log, cancellationToken).ConfigureAwait(false);
                    break;

                case AiDirectiveType.OpenShop:
                    await ExecuteOpenShopAsync(npc, context, directive, log, cancellationToken).ConfigureAwait(false);
                    break;

                case AiDirectiveType.OfferQuest:
                    await ExecuteOfferQuestAsync(npc, context, directive, log, cancellationToken).ConfigureAwait(false);
                    break;

                case AiDirectiveType.Reaction:
                    await ExecuteReactionAsync(npc, context, directive, log, cancellationToken).ConfigureAwait(false);
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

    private async Task ExecuteMoveToAsync(Npc npc, AiContext context, AiDirective directive, ICollection<string> log, CancellationToken cancellationToken)
    {
        // delegujemy ruch do adaptera eventów, żeby zachować kolejkę RequestedEvent
        var published = await _aiDirectiveAdapter.PublishAsync(npc, directive, context, cancellationToken).ConfigureAwait(false);
        if (published)
        {
            log.Add("Requesting move towards destination (via event adapter).");
        }
        else
        {
            log.Add("MoveTo directive was not published by AI adapter.");
        }
    }

    // poprzednia, bezpośrednia implementacja ruchu pozostaje na razie nieużywana
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

    private async Task ExecuteBeginDialogueAsync(
        Npc npc,
        AiContext context,
        AiDirective directive,
        ICollection<string> log,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        var targetId = directive.TargetId ?? context.Target?.Id;
        if (targetId is null)
        {
            log.Add("BeginDialogue directive skipped: target not available.");
            return;
        }

        var dialogue = npc.Components.OfType<DialogueComponent>().FirstOrDefault();
        var scriptName = !string.IsNullOrWhiteSpace(directive.ScriptName)
            ? directive.ScriptName!
            : !string.IsNullOrWhiteSpace(dialogue?.DialogueScript)
                ? dialogue!.DialogueScript
                : _settings.DialogueScript;

    var baseParameters = ToParameterDictionary(dialogue?.ScriptParameters);
        var parameters = MergeParameters(baseParameters, directive.Metadata);

        var message = new NpcDialogueMessage(
            npc.Id,
            targetId,
            scriptName,
            parameters,
            DateTime.UtcNow);

        await _publisher.PublishAsync("npc.interaction.dialogue", message).ConfigureAwait(false);

        context.SetBlackboardValue("activeDialogueScript", scriptName);
        log.Add($"Initiated dialogue '{scriptName}' with character {targetId}.");
    }

    private async Task ExecuteOpenShopAsync(
        Npc npc,
        AiContext context,
        AiDirective directive,
        ICollection<string> log,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        var targetId = directive.TargetId ?? context.Target?.Id;
        if (targetId is null)
        {
            log.Add("OpenShop directive skipped: target not available.");
            return;
        }

        var merchant = npc.Components.OfType<MerchantComponent>().FirstOrDefault();
        if (merchant == null)
        {
            log.Add("OpenShop directive skipped: NPC lacks merchant component.");
            return;
        }

        var items = BuildMerchantInventorySnapshot(merchant);
        var message = new NpcTradeOfferMessage(
            npc.Id,
            targetId,
            items,
            merchant.GlobalPriceModifier == 0 ? 1f : merchant.GlobalPriceModifier,
            DateTime.UtcNow);

        await _publisher.PublishAsync("npc.interaction.trade", message).ConfigureAwait(false);

        context.SetBlackboardValue("merchantSession", targetId.Value);
        log.Add($"Opened merchant interface for character {targetId} with {items.Count} items available.");
    }

    private async Task ExecuteOfferQuestAsync(
        Npc npc,
        AiContext context,
        AiDirective directive,
        ICollection<string> log,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        var targetId = directive.TargetId ?? context.Target?.Id;
        if (targetId is null)
        {
            log.Add("OfferQuest directive skipped: target not available.");
            return;
        }

        var questGiver = npc.Components.OfType<QuestGiverComponent>().FirstOrDefault();
        var questIds = ExtractQuestIds(directive.Metadata)
            ?? (questGiver?.AvailableQuests as IEnumerable<Guid>)
            ?? Array.Empty<Guid>();

        var questArray = questIds.ToArray();
        if (questArray.Length == 0)
        {
            log.Add("OfferQuest directive skipped: no quests available.");
            return;
        }

        var message = new NpcQuestOfferMessage(
            npc.Id,
            targetId,
            questArray,
            DateTime.UtcNow);

        await _publisher.PublishAsync("npc.interaction.quest", message).ConfigureAwait(false);

        log.Add($"Offered {questArray.Length} quest(s) to character {targetId}.");
    }

    private async Task ExecuteReactionAsync(
        Npc npc,
        AiContext context,
        AiDirective directive,
        ICollection<string> log,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        var reaction = directive.Metadata != null && directive.Metadata.TryGetValue("reaction", out var value)
            ? Convert.ToString(value, CultureInfo.InvariantCulture)
            : null;

        if (string.IsNullOrWhiteSpace(reaction))
        {
            log.Add("Reaction directive skipped: missing reaction type.");
            return;
        }

        var targetId = directive.TargetId ?? context.Target?.Id;
        var metadata = ConvertMetadataToStrings(directive.Metadata);

        var message = new NpcReactionMessage(
            npc.Id,
            targetId,
            reaction!,
            DateTime.UtcNow,
            metadata);

        await _publisher.PublishAsync("npc.interaction.reaction", message).ConfigureAwait(false);

        log.Add($"Performed reaction '{reaction}' targeting {(targetId?.ToString() ?? "world")}.");
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

    private static IReadOnlyDictionary<string, object?> ToParameterDictionary(Dictionary<string, object>? source)
    {
        if (source is { Count: > 0 })
        {
            return source.ToDictionary(
                pair => pair.Key,
                pair => (object?)pair.Value,
                StringComparer.OrdinalIgnoreCase);
        }

        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, string> MergeParameters(
        IReadOnlyDictionary<string, object?> baseParameters,
        IReadOnlyDictionary<string, object?>? overrides)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in baseParameters)
        {
            result[pair.Key] = ToInvariantString(pair.Value);
        }

        if (overrides != null)
        {
            foreach (var pair in overrides)
            {
                result[pair.Key] = ToInvariantString(pair.Value);
            }
        }

        return result;
    }

    private static List<MerchantItemSnapshot> BuildMerchantInventorySnapshot(MerchantComponent merchant)
    {
        var items = new List<MerchantItemSnapshot>(merchant.MerchantInventory.Count);

        foreach (var slot in merchant.MerchantInventory)
        {
            if (slot.IsEmpty || slot.Item is not { } item)
            {
                continue;
            }

            var quantity = Math.Max(1, slot.Quantity);
            var modifier = ResolvePriceModifier(merchant, item);
            items.Add(new MerchantItemSnapshot(item.Id, item.Name, quantity, modifier));
        }

        return items;
    }

    private static float ResolvePriceModifier(MerchantComponent merchant, Item item)
    {
        if (merchant.PriceModifiers is { Count: > 0 })
        {
            var idKey = item.Id.ToString("N", CultureInfo.InvariantCulture);
            if (merchant.PriceModifiers.TryGetValue(idKey, out var byId))
            {
                return byId;
            }

            if (!string.IsNullOrWhiteSpace(item.Name))
            {
                var nameKey = item.Name.ToLowerInvariant();
                if (merchant.PriceModifiers.TryGetValue(nameKey, out var byName))
                {
                    return byName;
                }
            }
        }

        return merchant.GlobalPriceModifier == 0 ? 1f : merchant.GlobalPriceModifier;
    }

    private static IEnumerable<Guid>? ExtractQuestIds(IReadOnlyDictionary<string, object?>? metadata)
    {
        if (metadata == null || !metadata.TryGetValue("quests", out var value) || value == null)
        {
            return null;
        }

        return value switch
        {
            IEnumerable<Guid> guidEnumerable => guidEnumerable,
            IEnumerable<object> objects => objects
                .Select(TryConvertGuid)
                .Where(guid => guid != Guid.Empty)
                .ToArray(),
            Guid guid => new[] { guid },
            string s when Guid.TryParse(s, out var parsed) => new[] { parsed },
            _ => null
        };
    }

    private static IReadOnlyDictionary<string, string>? ConvertMetadataToStrings(IReadOnlyDictionary<string, object?>? metadata)
    {
        if (metadata == null || metadata.Count == 0)
        {
            return null;
        }

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in metadata)
        {
            result[pair.Key] = ToInvariantString(pair.Value);
        }

        return result;
    }

    private static Guid TryConvertGuid(object value)
    {
        return value switch
        {
            Guid guid => guid,
            string s when Guid.TryParse(s, out var parsed) => parsed,
            _ => Guid.Empty
        };
    }

    private static string ToInvariantString(object? value)
    {
        return value switch
        {
            null => string.Empty,
            string s => s,
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
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
