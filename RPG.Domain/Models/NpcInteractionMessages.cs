using System;
using System.Collections.Generic;

namespace RPG.Domain.Models;

/// <summary>
///     Message emitted when an NPC starts or updates a dialogue with a player.
/// </summary>
public sealed record NpcDialogueMessage(
    Guid NpcId,
    Guid? CharacterId,
    string ScriptName,
    IReadOnlyDictionary<string, string> Parameters,
    DateTime OccurredAtUtc);

/// <summary>
///     Snapshot of an item available from an NPC merchant.
/// </summary>
public sealed record MerchantItemSnapshot(
    Guid ItemId,
    string Name,
    int Quantity,
    float PriceModifier);

/// <summary>
///     Message emitted when an NPC merchant opens a trading session.
/// </summary>
public sealed record NpcTradeOfferMessage(
    Guid NpcId,
    Guid? CharacterId,
    IReadOnlyCollection<MerchantItemSnapshot> Items,
    float GlobalPriceModifier,
    DateTime OccurredAtUtc);

/// <summary>
///     Message emitted when an NPC offers quests to a player.
/// </summary>
public sealed record NpcQuestOfferMessage(
    Guid NpcId,
    Guid? CharacterId,
    IReadOnlyCollection<Guid> QuestIds,
    DateTime OccurredAtUtc);

/// <summary>
///     Message emitted when an NPC performs a contextual reaction (e.g., emote, animation).
/// </summary>
public sealed record NpcReactionMessage(
    Guid NpcId,
    Guid? CharacterId,
    string ReactionType,
    DateTime OccurredAtUtc,
    IReadOnlyDictionary<string, string>? Metadata);
