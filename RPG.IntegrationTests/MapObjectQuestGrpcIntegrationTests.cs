using Microsoft.Extensions.DependencyInjection;
using RPG.GameServer.QueryProtos;
using RPG.Infrastructure.Interfaces;
using FluentAssertions;
using System.Numerics;

namespace RPG.IntegrationTests;

public class MapObjectQuestGrpcIntegrationTests : IClassFixture<TestContainersFixture>
{
    private readonly TestContainersFixture _fixture;

    public MapObjectQuestGrpcIntegrationTests(TestContainersFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task MapObjectQuery_Should_Return_TypedComponents_And_JsonComponents()
    {
        await using var factory = new GameServerFactory(_fixture);
        using var scope = factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IModelRepository>();

        // seed minimal MapObject with Container/Lockable/Door if not present
        var worldId = Guid.NewGuid();
        var mo = RPG.Domain.Models.MapObjects.MapObject.Create("Chest", new RPG.Domain.Models.Location { Position = Vector3.One, WorldId = worldId }, worldId);
        mo.Components.Add(new RPG.Domain.Models.MapObjects.MapObjectComponents.ContainerComponent());
        mo.Components.Add(new RPG.Domain.Models.MapObjects.MapObjectComponents.LockableComponent { IsLocked = true, RequiredKeyItemId = "key-1", LockpickDifficulty = 3, CanBeLockpicked = true });
        mo.Components.Add(new RPG.Domain.Models.MapObjects.MapObjectComponents.DoorComponent { IsOpen = false, OpenAngle = 90 });
        await repo.UpsertAsync(mo);
        // Force persistence to Mongo if not yet flushed (List* queries read from Mongo via ModelRepositoryHandler)
        var mongo = scope.ServiceProvider.GetRequiredService<RPG.Infrastructure.Interfaces.IMongoRepository>();
        var mapperMo = scope.ServiceProvider.GetRequiredService<RPG.Infrastructure.Interfaces.IModelMapper<RPG.Domain.Models.MapObjects.MapObject, RPG.Infrastructure.Models.MapObjectDocument>>();
        var countMo = await mongo.CountAsync<RPG.Infrastructure.Models.MapObjectDocument>();
        if (countMo == 0)
        {
            var doc = mapperMo.ToPersistence(mo);
            await mongo.UpsertAsync(doc);
        }

        var auth = await factory.CreateAuthenticatedChannelAsync("mapobject-query-tester");
        using var channel = auth.Channel;
        var sessionHeaders = auth.Headers;
        var client = new MapObjectQuery.MapObjectQueryClient(channel);
        var single = await client.GetMapObjectAsync(new MapObjectGetByIdRequest { Id = mo.Id.ToString() }, sessionHeaders);
        single?.Mo.Should().NotBeNull();
        single!.Mo.Id.Should().Be(mo.Id.ToString());
        single.Mo.Container.Should().NotBeNull();
        single.Mo.Lockable.Should().NotBeNull();
        single.Mo.Door.Should().NotBeNull();
        single.Mo.Components.Count.Should().BeGreaterOrEqualTo(3); // JSON components as well

        var list = await client.ListMapObjectsAsync(new MapObjectListRequest(), sessionHeaders);
        list.Mos.Count.Should().BeGreaterOrEqualTo(1);

        var many = await client.GetMapObjectsByIdsAsync(new MapObjectGetByIdsRequest { Ids = { mo.Id.ToString() } }, sessionHeaders);
        many.Mos.Should().ContainSingle();
    }

    [Fact]
    public async Task QuestQuery_Should_Return_TypedComponents_And_JsonComponents()
    {
        await using var factory = new GameServerFactory(_fixture);
        using var scope = factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IModelRepository>();

        // seed minimal Quest with LevelRequirement and ItemRewards
        var q = RPG.Domain.Models.Quests.Quest.Create("Test Quest", "Desc", "Giver", new RPG.Domain.Models.Location { Position = Vector3.Zero });
        q.Components.Add(new RPG.Domain.Models.Quests.QuestComponents.LevelRequirementComponent { MinLevel = 5, MaxLevel = 10 });
        var rewards = new RPG.Domain.Models.Quests.QuestComponents.ItemRewardsComponent();
        rewards.GuaranteedItems.Add(new RPG.Domain.Common.InventorySlot { Quantity = 1 });
        rewards.ChoiceItems.Add(new RPG.Domain.Common.InventorySlot { Quantity = 2 });
        q.Components.Add(rewards);
        await repo.UpsertAsync(q);
        var mongoQ = scope.ServiceProvider.GetRequiredService<RPG.Infrastructure.Interfaces.IMongoRepository>();
        var mapperQ = scope.ServiceProvider.GetRequiredService<RPG.Infrastructure.Interfaces.IModelMapper<RPG.Domain.Models.Quests.Quest, RPG.Infrastructure.Models.QuestDocument>>();
        var countQ = await mongoQ.CountAsync<RPG.Infrastructure.Models.QuestDocument>();
        if (countQ == 0)
        {
            var docQ = mapperQ.ToPersistence(q);
            await mongoQ.UpsertAsync(docQ);
        }

        var auth = await factory.CreateAuthenticatedChannelAsync("quest-query-tester");
        using var channel = auth.Channel;
        var sessionHeaders = auth.Headers;
        var client = new QuestQuery.QuestQueryClient(channel);
        var single = await client.GetQuestAsync(new QuestGetByIdRequest { Id = q.Id.ToString() }, sessionHeaders);
        single?.Quest.Should().NotBeNull();
        single!.Quest.Id.Should().Be(q.Id.ToString());
        single.Quest.LevelRequirement.Should().NotBeNull();
        single.Quest.ItemRewards.Should().NotBeNull();
        single.Quest.Components.Count.Should().BeGreaterOrEqualTo(2); // JSON components as well

        var list = await client.ListQuestsAsync(new QuestListRequest(), sessionHeaders);
        list.Quests.Count.Should().BeGreaterOrEqualTo(1);

        var many = await client.GetQuestsByIdsAsync(new QuestGetByIdsRequest { Ids = { q.Id.ToString() } }, sessionHeaders);
        many.Quests.Should().ContainSingle();
    }
}
