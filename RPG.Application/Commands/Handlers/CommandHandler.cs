using System.Diagnostics;
using System.Numerics;
using RPG.Abstractions.Interfaces;
using RPG.Application.Diagnostics;
using RPG.Application.Events;
using RPG.Application.Events.RequestedEvents;
using RPG.Application.Infrastructure;
using RPG.Application.Interfaces;

namespace RPG.Application.Commands.Handlers;

public class CommandHandler : ICommandHandler<EquipItemCommand>,
    ICommandHandler<UnequipItemCommand>,
    ICommandHandler<PutItemToBankCommand>,
    ICommandHandler<GetItemFromBankCommand>,
    ICommandHandler<UseItemCommand>,
    ICommandHandler<DropItemCommand>,
    ICommandHandler<PickUpItemCommand>,
    ICommandHandler<GainExperienceCommand>,
    ICommandHandler<LevelUpCommand>,
    ICommandHandler<StartMovementCommand>,
    ICommandHandler<StopMovementCommand>,
    ICommandHandler<StartRotationCommand>,
    ICommandHandler<StopRotationCommand>,
    ICommandHandler<CreateCharacterCommand>,
    ICommandHandler<UseSkillCommand>,
    ICommandHandler<LearnSkillCommand>,
    ICommandHandler<LevelUpSkillCommand>,
    ICommandHandler<UnlearnSkillCommand>,
    ICommandHandler<DieCommand>,
    ICommandHandler<AcceptQuestCommand>,
    ICommandHandler<CompleteQuestCommand>,
    ICommandHandler<UpdateQuestProgressCommand>,
    ICommandHandler<AttackNpcCommand>,
    ICommandHandler<NpcDamageReportedCommand>,
    ICommandHandler<NpcRespawnCommand>

{
    private readonly IRequestEventQueue _requestQueue;
    private readonly IEventIdProvider _eventIdProvider;
    private readonly IEventSequenceStore _sequenceStore;
    private readonly IRequestedEventInlineDispatcher _inlineDispatcher;

    public CommandHandler(
        IRequestEventQueue requestQueue,
        IEventIdProvider eventIdProvider,
        IEventSequenceStore sequenceStore,
        IRequestedEventInlineDispatcher inlineDispatcher)
    {
        _requestQueue = requestQueue;
        _eventIdProvider = eventIdProvider;
        _sequenceStore = sequenceStore;
        _inlineDispatcher = inlineDispatcher;
    }

    private void EnsureMetadata(IMetadataCommand? cmd, Guid correlationId)
    {
        if (cmd == null) return;
        if (cmd.Metadata == null)
        {
            cmd.Metadata = new CommandMetadata(Guid.NewGuid(), correlationId, null, DateTime.UtcNow);
        }
    }

    private async Task<TEvent> PublishEventAsync<TEvent>(IMetadataCommand? cmd, Func<EventMetadata, TEvent> factory, CancellationToken ct)
        where TEvent : IGameEventWithMetadata
    {
        var correlationId = cmd?.Metadata?.CorrelationId ?? Guid.NewGuid();
        var causationId = cmd?.Metadata?.CommandId;
        var sequence = _sequenceStore.NextSequence(correlationId);
        var occurred = DateTime.UtcNow;
        EnsureMetadata(cmd, correlationId);
        var provisionalMeta = new EventMetadata(Guid.Empty, correlationId, causationId, sequence, occurred);
        var provisional = factory(provisionalMeta);
        var eventId = _eventIdProvider.Generate(provisional, occurred, sequence, correlationId);
        var finalMeta = provisionalMeta with { EventId = eventId };
        var finalEvent = factory(finalMeta);
        _requestQueue.Enqueue(finalEvent);
        // inline processing to reduce latency and make state visible immediately
        await _inlineDispatcher.TryHandleAsync(finalEvent, ct).ConfigureAwait(false);
        return finalEvent;
    }

    private async Task<CommandResult> HandleSimple<TCommand>(TCommand command, Guid characterId, string activityName,
        Func<EventMetadata, IGameEventWithMetadata> eventFactory,
        Func<TCommand, (bool success, CommandError? error, string? message)>? validate = null,
        CancellationToken ct = default)
    {
        using var activity = StartCommandActivity(activityName, characterId);
        var validation = validate?.Invoke(command) ?? (true, null, null);
        if (!validation.success)
        {
            return CommandResult.Fail(validation.error ?? CommandError.InvalidOperation, validation.message ?? "Validation failed");
        }
        await PublishEventAsync(command as IMetadataCommand, eventFactory, ct).ConfigureAwait(false);
        return CommandResult.Ok();
    }

    public Task<CommandResult> HandleAsync(StartMovementCommand command, CancellationToken cancellationToken = default)
    {
        if (!TryGetDirectionVector(command.Direction, out _))
            return Task.FromResult(CommandResult.Fail(CommandError.InvalidOperation, "Niepoprawny kierunek ruchu."));
        return HandleSimple(command, command.CharacterId, "CommandHandler.StartMovement",
            meta => new MovementStartRequestedEvent(meta, command.CharacterId, command.Direction, command.PreserveFacing), null, cancellationToken);
    }

    public Task<CommandResult> HandleAsync(StopMovementCommand command, CancellationToken cancellationToken = default)
        => HandleSimple(command, command.CharacterId, "CommandHandler.StopMovement",
            meta => new MovementStopRequestedEvent(meta, command.CharacterId), null, cancellationToken);

    public Task<CommandResult> HandleAsync(StartRotationCommand command, CancellationToken cancellationToken = default)
    {
        if (!TryGetDirectionVector(command.Direction, out _))
            return Task.FromResult(CommandResult.Fail(CommandError.InvalidOperation, "Niepoprawny kierunek rotacji."));
        return HandleSimple(command, command.CharacterId, "CommandHandler.StartRotation",
            meta => new RotationStartRequestedEvent(meta, command.CharacterId, command.Direction), null, cancellationToken);
    }

    public Task<CommandResult> HandleAsync(StopRotationCommand command, CancellationToken cancellationToken = default)
        => HandleSimple(command, command.CharacterId, "CommandHandler.StopRotation",
            meta => new RotationStopRequestedEvent(meta, command.CharacterId), null, cancellationToken);

    public Task<CommandResult> HandleAsync(EquipItemCommand command, CancellationToken cancellationToken = default)
        => HandleSimple(command, command.CharacterId, "CommandHandler.EquipItem",
            meta => new ItemEquipRequestedEvent(meta, command.CharacterId, command.Slot, command.ItemId), null, cancellationToken);

    public Task<CommandResult> HandleAsync(UnequipItemCommand command, CancellationToken cancellationToken = default)
        => HandleSimple(command, command.CharacterId, "CommandHandler.UnequipItem",
            meta => new ItemUnequipRequestedEvent(meta, command.CharacterId, command.Slot), null, cancellationToken);

    public Task<CommandResult> HandleAsync(PickUpItemCommand command, CancellationToken cancellationToken = default)
        => HandleSimple(command, command.CharacterId, "CommandHandler.PickUpItem",
            meta => new ItemPickupRequestedEvent(meta, command.CharacterId, command.ItemId), null, cancellationToken);

    public Task<CommandResult> HandleAsync(DropItemCommand command, CancellationToken cancellationToken = default)
        => HandleSimple(command, command.CharacterId, "CommandHandler.DropItem",
            meta => new DropItemRequestedEvent(meta, command.CharacterId, command.ItemId), null, cancellationToken);

    public Task<CommandResult> HandleAsync(PutItemToBankCommand command, CancellationToken cancellationToken = default)
        => HandleSimple(command, command.CharacterId, "CommandHandler.PutItemToBank",
            meta => new PutItemToBankRequestedEvent(meta, command.CharacterId, command.ItemId), null, cancellationToken);

    public Task<CommandResult> HandleAsync(GetItemFromBankCommand command, CancellationToken cancellationToken = default)
        => HandleSimple(command, command.CharacterId, "CommandHandler.GetItemFromBank",
            meta => new GetItemFromBankRequestedEvent(meta, command.CharacterId, command.ItemId), null, cancellationToken);

    public Task<CommandResult> HandleAsync(UseItemCommand command, CancellationToken cancellationToken = default)
        => HandleSimple(command, command.CharacterId, "CommandHandler.UseItem",
            meta => new UseItemRequestedEvent(meta, command.CharacterId, command.ItemId), null, cancellationToken);

    public Task<CommandResult> HandleAsync(UseSkillCommand command, CancellationToken cancellationToken = default)
        => HandleSimple(command, command.CharacterId, "CommandHandler.UseSkill",
            meta => new SkillUsageRequestedEvent(meta, command.CharacterId, command.SkillId, command.TargetId), null, cancellationToken);

    public Task<CommandResult> HandleAsync(LearnSkillCommand command, CancellationToken cancellationToken = default)
        => HandleSimple(command, command.CharacterId, "CommandHandler.LearnSkill",
            meta => new SkillLearnRequestedEvent(meta, command.CharacterId, command.Skill), null, cancellationToken);

    public Task<CommandResult> HandleAsync(LevelUpSkillCommand command, CancellationToken cancellationToken = default)
        => HandleSimple(command, command.CharacterId, "CommandHandler.LevelUpSkill",
            meta => new SkillLevelUpRequestedEvent(meta, command.CharacterId, command.SkillId), null, cancellationToken);

    public Task<CommandResult> HandleAsync(UnlearnSkillCommand command, CancellationToken cancellationToken = default)
        => HandleSimple(command, command.CharacterId, "CommandHandler.UnlearnSkill",
            meta => new SkillUnlearnRequestedEvent(meta, command.CharacterId, command.SkillId), null, cancellationToken);

    public Task<CommandResult> HandleAsync(DieCommand command, CancellationToken cancellationToken = default)
        => HandleSimple(command, command.CharacterId, "CommandHandler.Die",
            meta => new CharacterDeathRequestedEvent(meta, command.CharacterId, command.KillerId), null, cancellationToken);

    public Task<CommandResult> HandleAsync(GainExperienceCommand command, CancellationToken cancellationToken = default)
        => HandleSimple(command, command.CharacterId, "CommandHandler.GainExperience",
            meta => new ExperienceGainRequestedEvent(meta, command.CharacterId, command.ExperienceAmount), null, cancellationToken);

    public Task<CommandResult> HandleAsync(LevelUpCommand command, CancellationToken cancellationToken = default)
        => HandleSimple(command, command.CharacterId, "CommandHandler.LevelUp",
            meta => new CharacterLevelUpRequestedEvent(meta, command.CharacterId), null, cancellationToken);

    public Task<CommandResult> HandleAsync(AcceptQuestCommand command, CancellationToken cancellationToken = default)
        => HandleSimple(command, command.CharacterId, "CommandHandler.AcceptQuest",
            meta => new QuestAcceptRequestedEvent(meta, command.CharacterId, command.QuestId), null, cancellationToken);

    public Task<CommandResult> HandleAsync(CompleteQuestCommand command, CancellationToken cancellationToken = default)
        => HandleSimple(command, command.CharacterId, "CommandHandler.CompleteQuest",
            meta => new QuestCompleteRequestedEvent(meta, command.CharacterId, command.QuestId), null, cancellationToken);

    public Task<CommandResult> HandleAsync(UpdateQuestProgressCommand command, CancellationToken cancellationToken = default)
        => HandleSimple(command, command.CharacterId, "CommandHandler.UpdateQuestProgress",
            meta => new QuestProgressUpdateRequestedEvent(meta, command.CharacterId, command.QuestId, command.ObjectiveType, command.Progress), null, cancellationToken);

    public async Task<CommandResult> HandleAsync(CreateCharacterCommand command, CancellationToken cancellationToken = default)
    {
        using var activity = StartCommandActivity("CommandHandler.CreateCharacter", command.Character.Id);
        await PublishEventAsync(command as IMetadataCommand, meta => new CharacterCreateRequestedEvent(meta, command.Character), cancellationToken).ConfigureAwait(false);
        return CommandResult.Ok();
    }

    public Task<CommandResult> HandleAsync(AttackNpcCommand command, CancellationToken cancellationToken = default)
        => HandleSimple(command, command.CharacterId, "CommandHandler.AttackNpc",
            meta => new CharacterAttackRequestedEvent(meta, command.CharacterId, command.NpcId), null, cancellationToken);

    public Task<CommandResult> HandleAsync(NpcDamageReportedCommand command, CancellationToken cancellationToken = default)
        => HandleSimple(command, command.CharacterId, "CommandHandler.NpcDamageReport",
            meta => new NpcDamageRequestedEvent(meta, command.NpcId, command.CharacterId, command.DamageAmount), null, cancellationToken);

    public Task<CommandResult> HandleAsync(NpcRespawnCommand command, CancellationToken cancellationToken = default)
        => HandleSimple(command, command.NpcId, "CommandHandler.NpcRespawn",
            meta => new NpcRespawnRequestedEvent(meta, command.NpcId), null, cancellationToken);

    private static bool TryGetDirectionVector(int direction, out Vector3 vector)
    {
        vector = direction switch
        {
            1 => new Vector3(0f, 1f, 0f),
            2 => new Vector3(1f, 1f, 0f),
            3 => new Vector3(1f, 0f, 0f),
            4 => new Vector3(1f, -1f, 0f),
            5 => new Vector3(0f, -1f, 0f),
            6 => new Vector3(-1f, -1f, 0f),
            7 => new Vector3(-1f, 0f, 0f),
            8 => new Vector3(-1f, 1f, 0f),
            _ => Vector3.Zero
        };
        return vector != Vector3.Zero;
    }

    private static Activity? StartCommandActivity(string operation, Guid characterId)
    {
        var activity = ApplicationDiagnostics.ActivitySource.StartActivity(operation);
        activity?.SetTag("rpg.character.id", characterId);
        return activity;
    }
}
