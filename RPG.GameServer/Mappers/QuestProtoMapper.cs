using System.Text.Json;
using RPG.Domain.Models.Quests.QuestComponents;
using DomainQuest = RPG.Domain.Models.Quests.Quest;
using RPG.Infrastructure.Interfaces;
using RPG.GameServer.QueryProtos;

namespace RPG.GameServer.Mappers;

/// <summary>
/// Mapper for Quest domain model to proto message
/// </summary>
public class QuestProtoMapper : IProtoMapper<DomainQuest, Quest>
{
    private readonly Infrastructure.Interfaces.ILogger<QuestProtoMapper> _logger;
    private readonly LocationProtoMapper _locationMapper;

    public QuestProtoMapper(Infrastructure.Interfaces.ILogger<QuestProtoMapper> logger,
        LocationProtoMapper locationMapper)
    {
        _logger = logger;
        _locationMapper = locationMapper;
    }

    public Quest ToProto(DomainQuest domain)
    {
        _logger.Debug($"Converting Quest to proto. Id={domain.Id}, Title={domain.Title}");

        var proto = new Quest
        {
            Id = domain.Id.ToString(),
            Title = domain.Title,
            Description = domain.Description,
            QuestGiverName = domain.QuestGiverName,
            QuestGiverId = domain.QuestGiverId?.ToString() ?? string.Empty,
            StartLocation = _locationMapper.ToProto(domain.StartLocation)
        };

        if (domain.TurnInLocation != null)
        {
            proto.TurnInLocation = _locationMapper.ToProto(domain.TurnInLocation);
        }

        proto.Tags.AddRange(domain.Tags);

        // Components
        if (domain.GetComponent<LevelRequirementComponent>() is { } lvl)
        {
            proto.LevelRequirement = new LevelRequirementTyped
            {
                MinLevel = lvl.MinLevel,
                MaxLevel = lvl.MaxLevel ?? 0
            };
            proto.Components.Add(new Component { Type = nameof(LevelRequirementComponent), DataJson = JsonSerializer.Serialize(lvl, typeof(LevelRequirementComponent)) });
        }

        if (domain.GetComponent<ItemRewardsComponent>() is { } rewards)
        {
            var r = new ItemRewardsTyped { ChoiceCount = rewards.ChoiceCount };
            foreach (var slot in rewards.GuaranteedItems)
                r.Guaranteed.Add(new InventorySlot
                {
                    ItemId = slot.Item?.Id.ToString() ?? string.Empty,
                    Quantity = slot.Quantity
                });
            foreach (var slot in rewards.ChoiceItems)
                r.Choice.Add(new InventorySlot
                {
                    ItemId = slot.Item?.Id.ToString() ?? string.Empty,
                    Quantity = slot.Quantity
                });
            proto.ItemRewards = r;
            proto.Components.Add(new Component { Type = nameof(ItemRewardsComponent), DataJson = JsonSerializer.Serialize(rewards, typeof(ItemRewardsComponent)) });
        }

        if (domain.GetComponent<KillObjectiveComponent>() is { } kill)
        {
            proto.KillObjective = new KillObjectiveTyped
            {
                TargetNpcId = kill.TargetNpcId.ToString(),
                TargetNpcName = kill.TargetNpcName,
                RequiredCount = kill.RequiredCount,
                CurrentCount = kill.CurrentCount
            };
            proto.Components.Add(new Component { Type = nameof(KillObjectiveComponent), DataJson = JsonSerializer.Serialize(kill, typeof(KillObjectiveComponent)) });
        }

        if (domain.GetComponent<CollectObjectiveComponent>() is { } collect)
        {
            var c = new CollectObjectiveTyped();
            foreach (var slot in collect.RequiredItems)
            {
                c.RequiredItems.Add(new InventorySlot
                {
                    ItemId = slot.Item?.Id.ToString() ?? string.Empty,
                    Quantity = slot.Quantity
                });
            }
            proto.CollectObjective = c;
            proto.Components.Add(new Component { Type = nameof(CollectObjectiveComponent), DataJson = JsonSerializer.Serialize(collect, typeof(CollectObjectiveComponent)) });
        }

        if (domain.GetComponent<DeliverObjectiveComponent>() is { } deliver)
        {
            var d = new DeliverObjectiveTyped
            {
                DeliverToNpcId = deliver.DeliverToNpcId.ToString(),
                DeliverToNpcName = deliver.DeliverToNpcName
            };
            foreach (var slot in deliver.ItemsToDeliver)
            {
                d.ItemsToDeliver.Add(new InventorySlot
                {
                    ItemId = slot.Item?.Id.ToString() ?? string.Empty,
                    Quantity = slot.Quantity
                });
            }
            proto.DeliverObjective = d;
            proto.Components.Add(new Component { Type = nameof(DeliverObjectiveComponent), DataJson = JsonSerializer.Serialize(deliver, typeof(DeliverObjectiveComponent)) });
        }

        if (domain.GetComponent<ExploreObjectiveComponent>() is { } explore)
        {
            proto.ExploreObjective = new ExploreObjectiveTyped
            {
                TargetLocation = _locationMapper.ToProto(explore.TargetLocation),
                LocationName = explore.LocationName,
                ProximityRadius = explore.ProximityRadius,
                IsVisited = explore.IsVisited
            };
            proto.Components.Add(new Component { Type = nameof(ExploreObjectiveComponent), DataJson = JsonSerializer.Serialize(explore, typeof(ExploreObjectiveComponent)) });
        }

        if (domain.GetComponent<PrerequisiteQuestsComponent>() is { } prereq)
        {
            var p = new PrerequisiteQuestsTyped();
            foreach (var id in prereq.RequiredQuestIds)
                p.RequiredQuestIds.Add(id.ToString());
            proto.PrerequisiteQuests = p;
            proto.Components.Add(new Component { Type = nameof(PrerequisiteQuestsComponent), DataJson = JsonSerializer.Serialize(prereq, typeof(PrerequisiteQuestsComponent)) });
        }

        if (domain.GetComponent<ReputationRewardsComponent>() is { } rep)
        {
            var r = new ReputationRewardsTyped();
            foreach (var kv in rep.FactionReputations)
                r.FactionReputations[kv.Key] = kv.Value;
            proto.ReputationRewards = r;
            proto.Components.Add(new Component { Type = nameof(ReputationRewardsComponent), DataJson = JsonSerializer.Serialize(rep, typeof(ReputationRewardsComponent)) });
        }

        if (domain.GetComponent<RepeatableQuestComponent>() is { } repeat)
        {
            proto.Repeatable = new RepeatableQuestTyped
            {
                CooldownHours = repeat.CooldownHours,
                LastCompletedUnixMs = repeat.LastCompletedTime.HasValue
                    ? new DateTimeOffset(repeat.LastCompletedTime.Value).ToUnixTimeMilliseconds()
                    : 0
            };
            proto.Components.Add(new Component { Type = nameof(RepeatableQuestComponent), DataJson = JsonSerializer.Serialize(repeat, typeof(RepeatableQuestComponent)) });
        }

        if (domain.GetComponent<TimeLimitComponent>() is { } timeLimit)
        {
            proto.TimeLimit = new TimeLimitTyped
            {
                TimeLimitMinutes = timeLimit.TimeLimitMinutes,
                StartTimeUnixMs = timeLimit.StartTime.HasValue
                    ? new DateTimeOffset(timeLimit.StartTime.Value).ToUnixTimeMilliseconds()
                    : 0
            };
            proto.Components.Add(new Component { Type = nameof(TimeLimitComponent), DataJson = JsonSerializer.Serialize(timeLimit, typeof(TimeLimitComponent)) });
        }

        // Generic components as JSON (avoid duplicating already serialized typed ones)
        foreach (var component in domain.Components)
        {
            var typeName = component.GetType().Name;
            if (proto.Components.Any(c => c.Type == typeName)) continue; // already added
            proto.Components.Add(new Component
            {
                Type = typeName,
                DataJson = JsonSerializer.Serialize(component, component.GetType())
            });
        }

        _logger.Debug($"Quest proto created. Id={proto.Id}");
        return proto;
    }

    public DomainQuest ToDomain(Quest proto)
    {
        _logger.Debug($"Converting Quest proto to domain. Id={proto.Id}, Title={proto.Title}");

        var id = Guid.TryParse(proto.Id, out var parsed) ? parsed : Guid.NewGuid();
        var questGiverId = Guid.TryParse(proto.QuestGiverId, out var gId) ? gId : (Guid?)null;
        var startLocation = _locationMapper.ToDomain(proto.StartLocation);
        var turnInLocation = proto.TurnInLocation is not null
            ? _locationMapper.ToDomain(proto.TurnInLocation)
            : null;

        var quest = DomainQuest.Create(
            proto.Title,
            proto.Description,
            proto.QuestGiverName,
            startLocation
        );

        // Set additional properties
        quest.QuestGiverId = questGiverId;
        quest.TurnInLocation = turnInLocation;

        // Override Id
        typeof(DomainQuest).GetProperty(nameof(DomainQuest.Id))?.SetValue(quest, id);

        foreach (var tag in proto.Tags)
        {
            quest.Tags.Add(tag);
        }

        // Components
        if (proto.LevelRequirement is not null)
        {
            quest.Components.Add(new LevelRequirementComponent
            {
                MinLevel = proto.LevelRequirement.MinLevel,
                MaxLevel = proto.LevelRequirement.MaxLevel > 0 ? proto.LevelRequirement.MaxLevel : null
            });
        }

        if (proto.ItemRewards is not null)
        {
            // Note: InventorySlot in proto only has item_id (string) and quantity
            // Domain ItemRewardsComponent expects List<InventorySlot> but we can't fully populate Item here
            // This is a simplified version - full implementation would need IItemRepository to resolve Items
            var component = new ItemRewardsComponent
            {
                ChoiceCount = proto.ItemRewards.ChoiceCount
            };

            // Since GuaranteedItems and ChoiceItems are readonly, we need to use the Add method or constructor
            // For now, we'll skip full item resolution - this is a DTO → Domain conversion limitation
            // In production, you'd resolve items from IItemRepository by ItemId

            quest.Components.Add(component);
        }

        if (proto.KillObjective is not null)
        {
            var npcId = Guid.TryParse(proto.KillObjective.TargetNpcId, out var nId) ? nId : Guid.Empty;
            quest.Components.Add(new KillObjectiveComponent
            {
                TargetNpcId = npcId,
                TargetNpcName = proto.KillObjective.TargetNpcName,
                RequiredCount = proto.KillObjective.RequiredCount,
                CurrentCount = proto.KillObjective.CurrentCount
            });
        }

        if (proto.CollectObjective is not null)
        {
            // CollectObjectiveComponent has readonly RequiredItems collection
            // We would need to resolve items from repository or use constructor that accepts items
            // For now, create empty component - full implementation needs IItemRepository
            var component = new CollectObjectiveComponent();
            quest.Components.Add(component);
        }

        if (proto.DeliverObjective is not null)
        {
            var npcId = Guid.TryParse(proto.DeliverObjective.DeliverToNpcId, out var nId) ? nId : Guid.Empty;

            // DeliverObjectiveComponent has readonly ItemsToDeliver collection
            // Full implementation needs IItemRepository to resolve items by ID
            var component = new DeliverObjectiveComponent
            {
                DeliverToNpcId = npcId,
                DeliverToNpcName = proto.DeliverObjective.DeliverToNpcName
            };
            quest.Components.Add(component);
        }

        if (proto.ExploreObjective is not null)
        {
            var targetLocation = _locationMapper.ToDomain(proto.ExploreObjective.TargetLocation);
            quest.Components.Add(new ExploreObjectiveComponent
            {
                TargetLocation = targetLocation,
                LocationName = proto.ExploreObjective.LocationName,
                ProximityRadius = proto.ExploreObjective.ProximityRadius,
                IsVisited = proto.ExploreObjective.IsVisited
            });
        }

        if (proto.PrerequisiteQuests is not null)
        {
            var ids = proto.PrerequisiteQuests.RequiredQuestIds
                .Select(s => Guid.TryParse(s, out var qId) ? qId : (Guid?)null)
                .Where(g => g.HasValue)
                .Select(g => g!.Value)
                .ToList();

            quest.Components.Add(new PrerequisiteQuestsComponent { RequiredQuestIds = ids });
        }

        if (proto.ReputationRewards is not null)
        {
            quest.Components.Add(new ReputationRewardsComponent
            {
                FactionReputations = proto.ReputationRewards.FactionReputations.ToDictionary(kv => kv.Key, kv => kv.Value)
            });
        }

        if (proto.Repeatable is not null)
        {
            var lastCompleted = proto.Repeatable.LastCompletedUnixMs > 0
                ? DateTimeOffset.FromUnixTimeMilliseconds(proto.Repeatable.LastCompletedUnixMs).UtcDateTime
                : (DateTime?)null;

            quest.Components.Add(new RepeatableQuestComponent
            {
                CooldownHours = proto.Repeatable.CooldownHours,
                LastCompletedTime = lastCompleted
            });
        }

        if (proto.TimeLimit is not null)
        {
            var startTime = proto.TimeLimit.StartTimeUnixMs > 0
                ? DateTimeOffset.FromUnixTimeMilliseconds(proto.TimeLimit.StartTimeUnixMs).UtcDateTime
                : (DateTime?)null;

            quest.Components.Add(new TimeLimitComponent
            {
                TimeLimitMinutes = proto.TimeLimit.TimeLimitMinutes,
                StartTime = startTime
            });
        }

        _logger.Debug($"Quest domain created. Id={quest.Id}");
        return quest;
    }
}
