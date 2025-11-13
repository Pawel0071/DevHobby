using Grpc.Core;
using RPG.GameServer.QueryProtos;
using RPG.Application.Interfaces;
using RPG.Application.Queries;

namespace RPG.GameServer.Controllers;

public class QuestQueryServiceImpl(IQueryBus queryBus) : QuestQuery.QuestQueryBase
{
    public override async Task<QuestSingleReply> GetQuest(QuestGetByIdRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var id)) throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Id"));
        try
        {
            var dto = await queryBus.ExecuteAsync<GetQuestQuery, QuestReadDto>(new GetQuestQuery(id), context.CancellationToken);
            return new QuestSingleReply { Quest = Map(dto) };
        }
        catch (KeyNotFoundException)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Quest not found"));
        }
    }

    public override async Task<QuestListReply> ListQuests(QuestListRequest request, ServerCallContext context)
    {
        var list = await queryBus.ExecuteAsync<GetQuestsQuery, IReadOnlyList<QuestReadDto>>(new GetQuestsQuery(), context.CancellationToken);
        var reply = new QuestListReply();
        reply.Quests.AddRange(list.Select(Map));
        return reply;
    }

    public override async Task<QuestListReply> GetQuestsByIds(QuestGetByIdsRequest request, ServerCallContext context)
    {
        var ids = request.Ids.Select(s => Guid.TryParse(s, out var g) ? g : (Guid?)null).Where(g => g.HasValue).Select(g => g!.Value).ToArray();
        var list = await queryBus.ExecuteAsync<GetQuestsByIdsQuery, IReadOnlyList<QuestReadDto>>(new GetQuestsByIdsQuery(ids), context.CancellationToken);
        var reply = new QuestListReply();
        reply.Quests.AddRange(list.Select(Map));
        return reply;
    }

    private static Quest Map(QuestReadDto dto)
    {
        var msg = new Quest
        {
            Id = dto.Id.ToString(),
            Title = dto.Title,
            Description = dto.Description,
            QuestGiverName = dto.QuestGiverName,
            QuestGiverId = dto.QuestGiverId?.ToString() ?? string.Empty,
            StartLocation = new Location
            {
                X = dto.StartLocation.X,
                Y = dto.StartLocation.Y,
                Z = dto.StartLocation.Z,
                WorldId = dto.StartLocation.WorldId ?? string.Empty,
                MapId = dto.StartLocation.MapId,
                ZoneName = dto.StartLocation.ZoneName,
                Rotation = dto.StartLocation.Rotation
            }
        };
        if (dto.TurnInLocation != null)
        {
            msg.TurnInLocation = new Location
            {
                X = dto.TurnInLocation.X,
                Y = dto.TurnInLocation.Y,
                Z = dto.TurnInLocation.Z,
                WorldId = dto.TurnInLocation.WorldId ?? string.Empty,
                MapId = dto.TurnInLocation.MapId,
                ZoneName = dto.TurnInLocation.ZoneName,
                Rotation = dto.TurnInLocation.Rotation
            };
        }
        if (dto.LevelRequirement != null)
        {
            msg.LevelRequirement = new LevelRequirementTyped
            {
                MinLevel = dto.LevelRequirement.MinLevel,
                MaxLevel = dto.LevelRequirement.MaxLevel ?? 0
            };
        }
        if (dto.ItemRewards != null)
        {
            var rewards = new ItemRewardsTyped { ChoiceCount = dto.ItemRewards.ChoiceCount };
            foreach (var s in dto.ItemRewards.Guaranteed)
                rewards.Guaranteed.Add(new InventorySlot { ItemId = s.ItemId?.ToString() ?? string.Empty, Quantity = s.Quantity });
            foreach (var s in dto.ItemRewards.Choice)
                rewards.Choice.Add(new InventorySlot { ItemId = s.ItemId?.ToString() ?? string.Empty, Quantity = s.Quantity });
            msg.ItemRewards = rewards;
        }
        if (dto.KillObjective != null)
        {
            msg.KillObjective = new KillObjectiveTyped
            {
                TargetNpcId = dto.KillObjective.TargetNpcId.ToString(),
                TargetNpcName = dto.KillObjective.TargetNpcName,
                RequiredCount = dto.KillObjective.RequiredCount,
                CurrentCount = dto.KillObjective.CurrentCount
            };
        }
        if (dto.CollectObjective != null)
        {
            var collect = new CollectObjectiveTyped();
            foreach (var it in dto.CollectObjective.RequiredItems)
            {
                collect.RequiredItems.Add(new InventorySlot { ItemId = it.ItemId?.ToString() ?? string.Empty, Quantity = it.Quantity });
            }
            msg.CollectObjective = collect;
        }
        if (dto.DeliverObjective != null)
        {
            var deliver = new DeliverObjectiveTyped { DeliverToNpcId = dto.DeliverObjective.DeliverToNpcId.ToString(), DeliverToNpcName = dto.DeliverObjective.DeliverToNpcName };
            foreach (var it in dto.DeliverObjective.ItemsToDeliver)
            {
                deliver.ItemsToDeliver.Add(new InventorySlot { ItemId = it.ItemId?.ToString() ?? string.Empty, Quantity = it.Quantity });
            }
            msg.DeliverObjective = deliver;
        }
        if (dto.ExploreObjective != null)
        {
            msg.ExploreObjective = new ExploreObjectiveTyped
            {
                TargetLocation = new Location
                {
                    X = dto.ExploreObjective.TargetLocation.X,
                    Y = dto.ExploreObjective.TargetLocation.Y,
                    Z = dto.ExploreObjective.TargetLocation.Z,
                    WorldId = dto.ExploreObjective.TargetLocation.WorldId ?? string.Empty,
                    MapId = dto.ExploreObjective.TargetLocation.MapId,
                    ZoneName = dto.ExploreObjective.TargetLocation.ZoneName,
                    Rotation = dto.ExploreObjective.TargetLocation.Rotation
                },
                LocationName = dto.ExploreObjective.LocationName,
                ProximityRadius = dto.ExploreObjective.ProximityRadius,
                IsVisited = dto.ExploreObjective.IsVisited
            };
        }
        if (dto.PrerequisiteQuests != null)
        {
            var pre = new PrerequisiteQuestsTyped();
            foreach (var id in dto.PrerequisiteQuests.RequiredQuestIds)
                pre.RequiredQuestIds.Add(id.ToString());
            msg.PrerequisiteQuests = pre;
        }
        if (dto.ReputationRewards != null)
        {
            var rep = new ReputationRewardsTyped();
            foreach (var kv in dto.ReputationRewards.FactionReputations)
                rep.FactionReputations[kv.Key] = kv.Value;
            msg.ReputationRewards = rep;
        }
        if (dto.Repeatable != null)
        {
            msg.Repeatable = new RepeatableQuestTyped
            {
                CooldownHours = dto.Repeatable.CooldownHours,
                LastCompletedUnixMs = dto.Repeatable.LastCompletedTime.HasValue ? new DateTimeOffset(dto.Repeatable.LastCompletedTime.Value).ToUnixTimeMilliseconds() : 0
            };
        }
        if (dto.TimeLimit != null)
        {
            msg.TimeLimit = new TimeLimitTyped
            {
                TimeLimitMinutes = dto.TimeLimit.TimeLimitMinutes,
                StartTimeUnixMs = dto.TimeLimit.StartTime.HasValue ? new DateTimeOffset(dto.TimeLimit.StartTime.Value).ToUnixTimeMilliseconds() : 0
            };
        }
        msg.Tags.AddRange(dto.Tags);
        foreach (var c in dto.Components)
        {
            msg.Components.Add(new Component { Type = c.Type, Data = c.Data });
        }
        return msg;
    }
}
