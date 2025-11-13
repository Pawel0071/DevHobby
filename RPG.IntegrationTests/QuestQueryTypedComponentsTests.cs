using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;
using RPG.GameServer;
using RPG.GameServer.QueryProtos;
using RPG.Infrastructure.Interfaces;
using System.Numerics;

namespace RPG.IntegrationTests;

public class QuestQueryTypedComponentsTests : IClassFixture<TestContainersFixture>
{
    private readonly TestContainersFixture _fixture;

    public QuestQueryTypedComponentsTests(TestContainersFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetQuest_Should_Map_All_Typed_Components()
    {
        await using var factory = new GameServerFactory(_fixture);
        using var scope = factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IModelRepository>();

        var quest = RPG.Domain.Models.Quests.Quest.Create("Mega Quest", "Full test", "Giver", new RPG.Domain.Models.Location { Position = Vector3.Zero });
        quest.Components.Add(new RPG.Domain.Models.Quests.QuestComponents.LevelRequirementComponent { MinLevel = 10, MaxLevel = 20 });
        var rewards = new RPG.Domain.Models.Quests.QuestComponents.ItemRewardsComponent();
        rewards.GuaranteedItems.Add(new RPG.Domain.Common.InventorySlot { Quantity = 1 });
        rewards.ChoiceItems.Add(new RPG.Domain.Common.InventorySlot { Quantity = 2 });
        quest.Components.Add(rewards);
        quest.Components.Add(new RPG.Domain.Models.Quests.QuestComponents.KillObjectiveComponent { TargetNpcId = Guid.NewGuid(), TargetNpcName = "Orc", RequiredCount = 5, CurrentCount = 1 });
        var collect = new RPG.Domain.Models.Quests.QuestComponents.CollectObjectiveComponent();
        collect.RequiredItems.Add(new RPG.Domain.Common.InventorySlot { Quantity = 3 });
        quest.Components.Add(collect);
        var deliver = new RPG.Domain.Models.Quests.QuestComponents.DeliverObjectiveComponent { DeliverToNpcId = Guid.NewGuid(), DeliverToNpcName = "King" };
        deliver.ItemsToDeliver.Add(new RPG.Domain.Common.InventorySlot { Quantity = 4 });
        quest.Components.Add(deliver);
        quest.Components.Add(new RPG.Domain.Models.Quests.QuestComponents.ExploreObjectiveComponent { LocationName = "Cave", ProximityRadius = 15f });
        quest.Components.Add(new RPG.Domain.Models.Quests.QuestComponents.PrerequisiteQuestsComponent { RequiredQuestIds = { Guid.NewGuid(), Guid.NewGuid() } });
        quest.Components.Add(new RPG.Domain.Models.Quests.QuestComponents.ReputationRewardsComponent { FactionReputations = { ["Knights"] = 100, ["Mages"] = 50 } });
        quest.Components.Add(new RPG.Domain.Models.Quests.QuestComponents.RepeatableQuestComponent { CooldownHours = 24 });
        quest.Components.Add(new RPG.Domain.Models.Quests.QuestComponents.TimeLimitComponent { TimeLimitMinutes = 60 });
        await repo.UpsertAsync(quest);

        var client = new QuestQuery.QuestQueryClient(CreateChannel(factory));
        var reply = await client.GetQuestAsync(new QuestGetByIdRequest { Id = quest.Id.ToString() });
        reply.Quest.Should().NotBeNull();
        var q = reply.Quest;
        q.LevelRequirement.Should().NotBeNull();
        q.ItemRewards.Should().NotBeNull();
        q.KillObjective.Should().NotBeNull();
        q.CollectObjective.Should().NotBeNull();
        q.DeliverObjective.Should().NotBeNull();
        q.ExploreObjective.Should().NotBeNull();
        q.PrerequisiteQuests.Should().NotBeNull();
        q.ReputationRewards.Should().NotBeNull();
        q.Repeatable.Should().NotBeNull();
        q.TimeLimit.Should().NotBeNull();
        q.Components.Count.Should().BeGreaterOrEqualTo(10);
    }

    [Fact]
    public async Task GetQuest_Snapshot_Should_Match_Expected_Structure()
    {
        await using var factory = new GameServerFactory(_fixture);
        using var scope = factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IModelRepository>();

        var quest = RPG.Domain.Models.Quests.Quest.Create("Snapshot Quest", "Desc", "Giver", new RPG.Domain.Models.Location { Position = Vector3.Zero });
        quest.Components.Add(new RPG.Domain.Models.Quests.QuestComponents.LevelRequirementComponent { MinLevel = 5 });
        await repo.UpsertAsync(quest);

        var client = new QuestQuery.QuestQueryClient(CreateChannel(factory));
        var reply = await client.GetQuestAsync(new QuestGetByIdRequest { Id = quest.Id.ToString() });
        reply.Quest.Should().NotBeNull();

        var json = JsonSerializer.Serialize(reply.Quest, new JsonSerializerOptions { WriteIndented = true });
        json.Should().Contain("\"LevelRequirement\""); // PascalCase zgodnie z generatorem protosów
        json.Should().Contain("\"Id\": \"" + quest.Id.ToString()); // dopasowanie do rzeczywistego klucza (format: "Id": "...")
        json.Should().Contain("\"Title\": \"Snapshot Quest\"");
    }

    [Fact]
    public async Task GetQuest_FullSnapshot_Should_Match_File()
    {
        await using var factory = new GameServerFactory(_fixture);
        using var scope = factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IModelRepository>();

        var quest = RPG.Domain.Models.Quests.Quest.Create("Mega Quest", "Full test", "Giver", new RPG.Domain.Models.Location { Position = Vector3.Zero });
        quest.Components.Add(new RPG.Domain.Models.Quests.QuestComponents.LevelRequirementComponent { MinLevel = 10, MaxLevel = 20 });
        var rewards = new RPG.Domain.Models.Quests.QuestComponents.ItemRewardsComponent();
        rewards.GuaranteedItems.Add(new RPG.Domain.Common.InventorySlot { Quantity = 1 });
        rewards.ChoiceItems.Add(new RPG.Domain.Common.InventorySlot { Quantity = 2 });
        quest.Components.Add(rewards);
        var kill = new RPG.Domain.Models.Quests.QuestComponents.KillObjectiveComponent { TargetNpcId = Guid.NewGuid(), TargetNpcName = "Orc", RequiredCount = 5, CurrentCount = 1 };
        quest.Components.Add(kill);
        var collect = new RPG.Domain.Models.Quests.QuestComponents.CollectObjectiveComponent();
        collect.RequiredItems.Add(new RPG.Domain.Common.InventorySlot { Quantity = 3 });
        quest.Components.Add(collect);
        var deliver = new RPG.Domain.Models.Quests.QuestComponents.DeliverObjectiveComponent { DeliverToNpcId = Guid.NewGuid(), DeliverToNpcName = "King" };
        deliver.ItemsToDeliver.Add(new RPG.Domain.Common.InventorySlot { Quantity = 4 });
        quest.Components.Add(deliver);
        quest.Components.Add(new RPG.Domain.Models.Quests.QuestComponents.ExploreObjectiveComponent { LocationName = "Cave", ProximityRadius = 15f });
        quest.Components.Add(new RPG.Domain.Models.Quests.QuestComponents.PrerequisiteQuestsComponent { RequiredQuestIds = { Guid.NewGuid(), Guid.NewGuid() } });
        quest.Components.Add(new RPG.Domain.Models.Quests.QuestComponents.ReputationRewardsComponent { FactionReputations = { ["Knights"] = 100, ["Mages"] = 50 } });
        quest.Components.Add(new RPG.Domain.Models.Quests.QuestComponents.RepeatableQuestComponent { CooldownHours = 24 });
        quest.Components.Add(new RPG.Domain.Models.Quests.QuestComponents.TimeLimitComponent { TimeLimitMinutes = 60 });
        await repo.UpsertAsync(quest);

        var client = new QuestQuery.QuestQueryClient(CreateChannel(factory));
        var reply = await client.GetQuestAsync(new QuestGetByIdRequest { Id = quest.Id.ToString() });

        var raw = JsonSerializer.Serialize(reply.Quest, new JsonSerializerOptions { WriteIndented = true });

        string NormalizeDynamic(string json)
        {
            // Zamien GUID-y na __DYNAMIC__ dla porównania snapshotu
            return System.Text.RegularExpressions.Regex.Replace(json, "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}", "__DYNAMIC__");
        }

        var normalized = NormalizeDynamic(raw);
        var snapshotDir = Path.Combine(AppContext.BaseDirectory, "Snapshots");
        var snapshotPath = Path.Combine(snapshotDir, "quest_full_snapshot.json");
        if (!Directory.Exists(snapshotDir)) Directory.CreateDirectory(snapshotDir);
        if (!File.Exists(snapshotPath))
        {
            // Pierwsze uruchomienie: zapisz baseline, aby test był deterministyczny w kolejnych biegach
            File.WriteAllText(snapshotPath, normalized);
        }

        File.Exists(snapshotPath).Should().BeTrue("snapshot file must exist");
        var expected = File.ReadAllText(snapshotPath);
        expected.Should().NotBeNullOrWhiteSpace();
        NormalizeDynamic(expected).Should().Be(normalized);
    }

    private static GrpcChannel CreateChannel(GameServerFactory factory)
    {
        return GrpcChannel.ForAddress(factory.Server.BaseAddress, new GrpcChannelOptions { HttpClient = factory.CreateDefaultClient() });
    }
}
