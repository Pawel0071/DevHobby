// filepath: /Volumes/Data/Repositories/DevHobby/RPG.Application/Queries/QuestQueries.cs
using RPG.Application.Interfaces;
using RPG.Domain.Models.Quests;
using RPG.Domain.Models.Quests.QuestComponents;
using RPG.Infrastructure.Interfaces;
using System.Text.Json;

namespace RPG.Application.Queries;

public sealed record GetQuestQuery(Guid QuestId) : IQuery<QuestReadDto>;
public sealed record GetQuestsQuery() : IQuery<IReadOnlyList<QuestReadDto>>;
public sealed record GetQuestsByIdsQuery(IReadOnlyCollection<Guid> QuestIds) : IQuery<IReadOnlyList<QuestReadDto>>;

public sealed class QuestReadDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string QuestGiverName { get; init; } = string.Empty;
    public Guid? QuestGiverId { get; init; }
    public LocationReadDto StartLocation { get; init; } = new();
    public LocationReadDto? TurnInLocation { get; init; }
    public IReadOnlyCollection<string> Tags { get; init; } = Array.Empty<string>();
    public IReadOnlyList<ComponentReadDto> Components { get; init; } = new List<ComponentReadDto>();
    public LevelRequirementDto? LevelRequirement { get; set; }
    public ItemRewardsDto? ItemRewards { get; set; }
    public KillObjectiveDto? KillObjective { get; set; }
    public CollectObjectiveDto? CollectObjective { get; set; }
    public DeliverObjectiveDto? DeliverObjective { get; set; }
    public ExploreObjectiveDto? ExploreObjective { get; set; }
    public PrerequisiteQuestsDto? PrerequisiteQuests { get; set; }
    public ReputationRewardsDto? ReputationRewards { get; set; }
    public RepeatableQuestDto? Repeatable { get; set; }
    public TimeLimitDto? TimeLimit { get; set; }
}

public sealed class LevelRequirementDto
{
    public int MinLevel { get; init; }
    public int? MaxLevel { get; init; }
}

public sealed class ItemRewardsDto
{
    public IReadOnlyList<InventorySlotDto> Guaranteed { get; init; } = new List<InventorySlotDto>();
    public IReadOnlyList<InventorySlotDto> Choice { get; init; } = new List<InventorySlotDto>();
    public int ChoiceCount { get; init; }
}

public sealed class KillObjectiveDto { public Guid TargetNpcId { get; init; } public string TargetNpcName { get; init; } = string.Empty; public int RequiredCount { get; init; } public int CurrentCount { get; init; } }
public sealed class CollectObjectiveDto { public IReadOnlyList<InventorySlotDto> RequiredItems { get; init; } = new List<InventorySlotDto>(); }
public sealed class DeliverObjectiveDto { public IReadOnlyList<InventorySlotDto> ItemsToDeliver { get; init; } = new List<InventorySlotDto>(); public Guid DeliverToNpcId { get; init; } public string DeliverToNpcName { get; init; } = string.Empty; }
public sealed class ExploreObjectiveDto { public LocationReadDto TargetLocation { get; init; } = new(); public string LocationName { get; init; } = string.Empty; public float ProximityRadius { get; init; } public bool IsVisited { get; init; } }
public sealed class PrerequisiteQuestsDto { public IReadOnlyList<Guid> RequiredQuestIds { get; init; } = new List<Guid>(); }
public sealed class ReputationRewardsDto { public IReadOnlyDictionary<string,int> FactionReputations { get; init; } = new Dictionary<string,int>(); }
public sealed class RepeatableQuestDto { public int CooldownHours { get; init; } public DateTime? LastCompletedTime { get; init; } }
public sealed class TimeLimitDto { public int TimeLimitMinutes { get; init; } public DateTime? StartTime { get; init; } }

// Reuse InventorySlotDto from Common/ReadDtos
public sealed class GetQuestQueryHandler(IModelRepository repo) : IQueryHandler<GetQuestQuery, QuestReadDto>
{
    public async Task<QuestReadDto> HandleAsync(GetQuestQuery query, CancellationToken ct = default)
    {
        var quest = await repo.GetByIdAsync<Quest>(query.QuestId, ct) ?? throw new KeyNotFoundException("Quest not found");
        return QuestQueriesMapper.Map(quest);
    }
}

public sealed class GetQuestsQueryHandler(IModelRepository repo) : IQueryHandler<GetQuestsQuery, IReadOnlyList<QuestReadDto>>
{
    public async Task<IReadOnlyList<QuestReadDto>> HandleAsync(GetQuestsQuery query, CancellationToken ct = default)
    {
        var all = await repo.GetAllAsync<Quest>(ct);
        return all.Select(QuestQueriesMapper.Map).ToList();
    }
}

public sealed class GetQuestsByIdsQueryHandler(IModelRepository repo) : IQueryHandler<GetQuestsByIdsQuery, IReadOnlyList<QuestReadDto>>
{
    public async Task<IReadOnlyList<QuestReadDto>> HandleAsync(GetQuestsByIdsQuery query, CancellationToken ct = default)
    {
        var list = new List<QuestReadDto>(query.QuestIds.Count);
        foreach (var id in query.QuestIds)
        {
            var quest = await repo.GetByIdAsync<Quest>(id, ct);
            if (quest != null) list.Add(QuestQueriesMapper.Map(quest));
        }
        return list;
    }
}

internal static class QuestQueriesMapper
{
    public static QuestReadDto Map(Quest quest)
    {
        var dto = new QuestReadDto
        {
            Id = quest.Id,
            Title = quest.Title,
            Description = quest.Description,
            QuestGiverName = quest.QuestGiverName,
            QuestGiverId = quest.QuestGiverId,
            StartLocation = LocationReadDto.FromDomain(quest.StartLocation),
            TurnInLocation = quest.TurnInLocation != null ? LocationReadDto.FromDomain(quest.TurnInLocation) : null,
            Tags = quest.Tags.ToList(),
            Components = quest.Components.Select(c => new ComponentReadDto(c.GetType().Name, JsonSerializer.Serialize(c, c.GetType()))).ToList()
        };
        if (quest.GetComponent<LevelRequirementComponent>() is { } lvl)
            dto.LevelRequirement = new LevelRequirementDto { MinLevel = lvl.MinLevel, MaxLevel = lvl.MaxLevel };
        if (quest.GetComponent<ItemRewardsComponent>() is { } rewards)
            dto.ItemRewards = new ItemRewardsDto
            {
                Guaranteed = rewards.GuaranteedItems.Select(s => new InventorySlotDto { ItemId = s.Item?.Id, Quantity = s.Quantity }).ToList(),
                Choice = rewards.ChoiceItems.Select(s => new InventorySlotDto { ItemId = s.Item?.Id, Quantity = s.Quantity }).ToList(),
                ChoiceCount = rewards.ChoiceCount
            };
        if (quest.GetComponent<KillObjectiveComponent>() is { } kill)
            dto.KillObjective = new KillObjectiveDto { TargetNpcId = kill.TargetNpcId, TargetNpcName = kill.TargetNpcName, RequiredCount = kill.RequiredCount, CurrentCount = kill.CurrentCount };
        if (quest.GetComponent<CollectObjectiveComponent>() is { } collect)
            dto.CollectObjective = new CollectObjectiveDto { RequiredItems = collect.RequiredItems.Select(s => new InventorySlotDto { ItemId = s.Item?.Id, Quantity = s.Quantity }).ToList() };
        if (quest.GetComponent<DeliverObjectiveComponent>() is { } deliver)
            dto.DeliverObjective = new DeliverObjectiveDto { ItemsToDeliver = deliver.ItemsToDeliver.Select(s => new InventorySlotDto { ItemId = s.Item?.Id, Quantity = s.Quantity }).ToList(), DeliverToNpcId = deliver.DeliverToNpcId, DeliverToNpcName = deliver.DeliverToNpcName };
        if (quest.GetComponent<ExploreObjectiveComponent>() is { } explore)
            dto.ExploreObjective = new ExploreObjectiveDto { TargetLocation = LocationReadDto.FromDomain(explore.TargetLocation), LocationName = explore.LocationName, ProximityRadius = explore.ProximityRadius, IsVisited = explore.IsVisited };
        if (quest.GetComponent<PrerequisiteQuestsComponent>() is { } prereq)
            dto.PrerequisiteQuests = new PrerequisiteQuestsDto { RequiredQuestIds = prereq.RequiredQuestIds.ToList() };
        if (quest.GetComponent<ReputationRewardsComponent>() is { } rep)
            dto.ReputationRewards = new ReputationRewardsDto { FactionReputations = rep.FactionReputations.ToDictionary(kv => kv.Key, kv => kv.Value) };
        if (quest.GetComponent<RepeatableQuestComponent>() is { } repeat)
            dto.Repeatable = new RepeatableQuestDto { CooldownHours = repeat.CooldownHours, LastCompletedTime = repeat.LastCompletedTime };
        if (quest.GetComponent<TimeLimitComponent>() is { } timeLimit)
            dto.TimeLimit = new TimeLimitDto { TimeLimitMinutes = timeLimit.TimeLimitMinutes, StartTime = timeLimit.StartTime };

        // Walidacja logiczna:
        var now = DateTime.UtcNow;
        if (dto.TimeLimit?.StartTime.HasValue == true && dto.TimeLimit.StartTime.Value > now.AddMinutes(5))
            throw new InvalidOperationException("Quest TimeLimit StartTime jest w przyszłości zbyt daleko.");
        if (dto.Repeatable != null && dto.Repeatable.CooldownHours <= 0)
            throw new InvalidOperationException("Repeatable quest musi mieć dodatni cooldown.");

        return dto;
    }
}
