// filepath: /Volumes/Data/Repositories/DevHobby/RPG.Application/Events/Adapters/AiDirectiveEventAdapter.cs
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using RPG.Abstractions.Interfaces;
using RPG.AI.Core;
using RPG.AI.Directives;
using RPG.Core.Interfaces.NpcServices;
using RPG.Domain.Models;
using RPG.Domain.Models.Npcs;
using RPG.Abstractions.SharedModel;

namespace RPG.Application.Events.Adapters;

public sealed class AiDirectiveEventAdapter : IAiDirectiveEventAdapter
{
    private readonly INpcRequestedOperations _npcOps;

    public AiDirectiveEventAdapter(INpcRequestedOperations npcOps)
    {
        _npcOps = npcOps;
    }

    public Task<bool> PublishAsync(Npc npc, AiDirective directive, AiContext context, CancellationToken ct = default)
        => PublishAsyncInternal(npc, directive, context, DirectivePublishOptions.Default, ct)
            .ContinueWith(static t => t.Result.Succeeded, ct, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

    public Task<DirectivePublishResult> PublishAsync(
        Npc npc,
        AiDirective directive,
        AiContext context,
        DirectivePublishOptions? options,
        CancellationToken ct = default)
        => PublishAsyncInternal(npc, directive, context, options ?? DirectivePublishOptions.Default, ct);

    public async Task<DirectivePublishResult> PublishSequenceAsync(
        Npc npc,
        AiDirectiveSequence sequence,
        AiContext context,
        DirectivePublishOptions? options = null,
        CancellationToken ct = default)
    {
        if (sequence.Directives.Count == 0)
        {
            return DirectivePublishResult.Success(Array.Empty<AiDirective>());
        }

        var opts = options ?? DirectivePublishOptions.Default.WithContinueOnFailure(sequence.ContinueOnFailure);
        var processed = new List<AiDirective>(sequence.Directives.Count);

        foreach (var directive in sequence.Directives)
        {
            ct.ThrowIfCancellationRequested();
            var result = await PublishAsyncInternal(npc, directive, context, opts, ct).ConfigureAwait(false);
            processed.Add(directive);

            if (!result.Succeeded && !opts.ContinueOnFailure)
            {
                return DirectivePublishResult.Failure(processed, directive, result.FailureReason, result.Exception);
            }
        }

        return DirectivePublishResult.Success(processed);
    }

    public Task<DirectivePublishResult> PublishFollowAsync(Npc npc, Guid targetId, float desiredRange, float stopDistance, float? maxRange, AiContext context, CancellationToken ct = default)
    {
        var directive = new AiDirective(
            AiDirectiveType.FollowTarget,
            TargetId: targetId,
            DesiredRange: desiredRange,
            StopDistance: stopDistance,
            Metadata: maxRange.HasValue ? new Dictionary<string, object?> { ["maxRange"] = maxRange.Value } : null);
        return PublishAsync(npc, directive, context, DirectivePublishOptions.Default, ct);
    }

    public Task<DirectivePublishResult> PublishEngageAsync(Npc npc, Guid targetId, AiContext context, CancellationToken ct = default)
    {
        var directive = new AiDirective(
            AiDirectiveType.Reaction,
            TargetId: targetId,
            Metadata: new Dictionary<string, object?> { ["reaction"] = "engage" });
        return PublishAsync(npc, directive, context, DirectivePublishOptions.Default, ct);
    }

    public Task<DirectivePublishResult> PublishDisengageAsync(Npc npc, AiContext context, CancellationToken ct = default)
    {
        var directive = new AiDirective(
            AiDirectiveType.Reaction,
            Metadata: new Dictionary<string, object?> { ["reaction"] = "disengage" });
        return PublishAsync(npc, directive, context, DirectivePublishOptions.Default, ct);
    }

    private async Task<DirectivePublishResult> PublishAsyncInternal(
        Npc npc,
        AiDirective directive,
        AiContext context,
        DirectivePublishOptions options,
        CancellationToken ct)
    {
        try
        {
            var handled = await HandleDirectiveAsync(npc, directive, context, ct).ConfigureAwait(false);
            if (handled)
            {
                if (options.OnSuccessAsync != null)
                {
                    await options.OnSuccessAsync(directive, ct).ConfigureAwait(false);
                }

                return DirectivePublishResult.Success(new[] { directive });
            }

            var reason = $"Directive '{directive.Type}' not handled";
            if (options.OnFailureAsync != null)
            {
                var ctx = new DirectiveFailureContext(reason, null);
                await options.OnFailureAsync(directive, ctx, ct).ConfigureAwait(false);
            }

            return DirectivePublishResult.Failure(Array.Empty<AiDirective>(), directive, reason);
        }
        catch (Exception ex)
        {
            if (options.OnFailureAsync != null)
            {
                var ctx = new DirectiveFailureContext(ex.Message, ex);
                await options.OnFailureAsync(directive, ctx, ct).ConfigureAwait(false);
            }

            return DirectivePublishResult.Failure(Array.Empty<AiDirective>(), directive, ex.Message, ex);
        }
    }

    private async Task<bool> HandleDirectiveAsync(Npc npc, AiDirective directive, AiContext context, CancellationToken ct)
    {
        switch (directive.Type)
        {
            case AiDirectiveType.MoveToLocation:
                if (directive.Destination is { } dest)
                {
                    await _npcOps.RequestMoveAsync(npc.Id, dest, speed: 1.0f, ct).ConfigureAwait(false);
                    return true;
                }
                return false;
            case AiDirectiveType.FollowTarget:
                return await HandleFollowDirectiveAsync(npc, directive, context, ct).ConfigureAwait(false);
            case AiDirectiveType.Reaction:
                await _npcOps.RequestIdleAsync(npc.Id, 0f, ct).ConfigureAwait(false);
                return true;
            case AiDirectiveType.StopMovement:
                await _npcOps.RequestIdleAsync(npc.Id, 0f, ct).ConfigureAwait(false);
                return true;
            case AiDirectiveType.Idle:
                await _npcOps.RequestIdleAsync(npc.Id, 0f, ct).ConfigureAwait(false);
                return true;
            case AiDirectiveType.UseSkill:
                return await HandleUseSkillAsync(npc, directive, ct).ConfigureAwait(false);
            default:
                return false;
        }
    }

    private async Task<bool> HandleFollowDirectiveAsync(Npc npc, AiDirective directive, AiContext context, CancellationToken ct)
    {
        if (directive.TargetId is not Guid targetId)
        {
            return false;
        }

        var desiredRange = directive.DesiredRange ?? 2.0f;
        var stopDistance = directive.StopDistance ?? desiredRange;
        var maxRange = TryGetFloat(directive.Metadata, "maxRange", out var mr) ? mr : (float?)null;

        await _npcOps.RequestFollowAsync(npc.Id, targetId, desiredRange, stopDistance, maxRange, ct).ConfigureAwait(false);
        return true;
    }

    private async Task<bool> HandleUseSkillAsync(Npc npc, AiDirective directive, CancellationToken ct)
    {
        if (!TryResolveSkillId(directive.Metadata, out var skillId) || skillId == Guid.Empty)
        {
            return false;
        }

        await _npcOps.RequestUseSkillAsync(npc.Id, skillId, directive.TargetId, ct).ConfigureAwait(false);
        return true;
    }

    private static bool TryResolveSkillId(IReadOnlyDictionary<string, object?>? metadata, out Guid skillId)
    {
        skillId = Guid.Empty;
        if (metadata == null)
        {
            return false;
        }

        if (metadata.TryGetValue("skillId", out var raw) && raw is Guid g)
        {
            skillId = g;
            return true;
        }

        if (rawIsStringGuid(metadata, "skillId", out var parsed))
        {
            skillId = parsed;
            return true;
        }

        return false;
    }

    private static bool rawIsStringGuid(IReadOnlyDictionary<string, object?> metadata, string key, out Guid parsed)
    {
        parsed = Guid.Empty;
        return metadata.TryGetValue(key, out var raw) && raw is string s && Guid.TryParse(s, out parsed);
    }

    private static bool TryGetFloat(IReadOnlyDictionary<string, object?>? metadata, string key, out float value)
    {
        value = default;
        if (metadata == null)
        {
            return false;
        }

        if (metadata.TryGetValue(key, out var raw))
        {
            switch (raw)
            {
                case float f:
                    value = f;
                    return true;
                case double d:
                    value = (float)d;
                    return true;
                case string s when float.TryParse(s, CultureInfo.InvariantCulture, out var parsed):
                    value = parsed;
                    return true;
            }
        }

        return false;
    }
}
