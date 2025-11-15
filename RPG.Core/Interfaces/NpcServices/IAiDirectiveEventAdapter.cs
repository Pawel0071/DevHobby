// filepath: /Volumes/Data/Repositories/DevHobby/RPG.Core/Interfaces/NpcServices/IAiDirectiveEventAdapter.cs

using System.Diagnostics.CodeAnalysis;
using RPG.AI.Core;
using RPG.AI.Directives;
using RPG.Domain.Models.Npcs;

namespace RPG.Core.Interfaces.NpcServices;

public interface IAiDirectiveEventAdapter
{
    Task<bool> PublishAsync(Npc npc, AiDirective directive, AiContext context, CancellationToken ct = default);
    Task<DirectivePublishResult> PublishAsync(
        Npc npc,
        AiDirective directive,
        AiContext context,
        DirectivePublishOptions? options,
        CancellationToken ct = default);
    Task<DirectivePublishResult> PublishSequenceAsync(
        Npc npc,
        AiDirectiveSequence sequence,
        AiContext context,
        DirectivePublishOptions? options = null,
        CancellationToken ct = default);
    // Wygodne skróty dla typowych dyrektyw AI
    Task<DirectivePublishResult> PublishFollowAsync(Npc npc, Guid targetId, float desiredRange, float stopDistance, float? maxRange, AiContext context, CancellationToken ct = default);
    Task<DirectivePublishResult> PublishEngageAsync(Npc npc, Guid targetId, AiContext context, CancellationToken ct = default);
    Task<DirectivePublishResult> PublishDisengageAsync(Npc npc, AiContext context, CancellationToken ct = default);
}

public sealed record AiDirectiveSequence(
    IReadOnlyList<AiDirective> Directives,
    bool ContinueOnFailure = false,
    string? SequenceName = null)
{
    public static AiDirectiveSequence Empty { get; } = new(Array.Empty<AiDirective>());
}

public sealed record DirectivePublishOptions(
    bool ContinueOnFailure = false,
    Func<AiDirective, DirectiveFailureContext, CancellationToken, Task>? OnFailureAsync = null,
    Func<AiDirective, CancellationToken, Task>? OnSuccessAsync = null)
{
    public static DirectivePublishOptions Default { get; } = new();

    public DirectivePublishOptions WithContinueOnFailure(bool continueOnFailure)
        => this with { ContinueOnFailure = continueOnFailure };
}

[SuppressMessage("Usage", "CA1815", Justification = "Diagnostic-only access")]
public sealed record DirectiveFailureContext(string? Reason, Exception? Exception);

public sealed record DirectivePublishResult(
    bool Succeeded,
    IReadOnlyList<AiDirective> ProcessedDirectives,
    AiDirective? FailedDirective,
    string? FailureReason,
    Exception? Exception)
{
    public static DirectivePublishResult Success(IReadOnlyList<AiDirective> directives)
        => new(true, directives, null, null, null);

    public static DirectivePublishResult Failure(
        IReadOnlyList<AiDirective> processed,
        AiDirective failed,
        string? reason,
        Exception? exception = null)
        => new(false, processed, failed, reason, exception);

    public DirectivePublishResult WithProcessedDirectives(IReadOnlyList<AiDirective> directives)
        => this with { ProcessedDirectives = directives };
}
