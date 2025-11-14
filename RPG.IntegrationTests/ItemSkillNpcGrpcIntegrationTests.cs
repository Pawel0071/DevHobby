using System;
using System.Threading.Tasks;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;
using RPG.GameServer.QueryProtos;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Models;
using DomainItem = RPG.Domain.Models.Items.Item;
using DomainSkill = RPG.Domain.Models.Skills.Skill;
using DomainNpc = RPG.Domain.Models.Npcs.Npc;
using DomainLocation = RPG.Domain.Models.Location;
using Xunit;

namespace RPG.IntegrationTests;

public class ItemSkillNpcGrpcIntegrationTests : IClassFixture<TestContainersFixture>
{
    private readonly TestContainersFixture _fixture;

    public ItemSkillNpcGrpcIntegrationTests(TestContainersFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ItemQuery_Should_Preserve_Components_And_Tags()
    {
        await using var factory = new GameServerFactory(_fixture);
        using var scope = factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IModelRepository>();
        var mongo = scope.ServiceProvider.GetRequiredService<IMongoRepository>();
        var itemMapper = scope.ServiceProvider.GetRequiredService<IModelMapper<DomainItem, ItemDocument>>();

        var item = new DomainItem(Guid.NewGuid(), "weapon.test")
        {
            Name = "Test sword",
            RequiredLevel = 1,
            StackSize = 1
        };
        item.Tags.Add("weapon");
        item.Tags.Add("melee");

        await repo.UpsertAsync(item);
        await mongo.UpsertAsync(itemMapper.ToPersistence(item));

        var auth = await factory.CreateAuthenticatedChannelAsync("item-query-tester");
        using var channel = auth.Channel;
        var headers = auth.Headers;
        var client = new ItemQuery.ItemQueryClient(channel);

        var single = await client.GetItemAsync(new ItemGetByIdRequest { Id = item.Id.ToString() }, headers);
        single.Item.Should().NotBeNull();
        single.Item.Id.Should().Be(item.Id.ToString());
        single.Item.Tags.Should().Contain(new[] { "weapon", "melee" });

        var list = await client.ListItemsAsync(new ItemListRequest(), headers);
        list.Items.Should().NotBeEmpty();

        var idsRequest = new ItemGetByIdsRequest();
        idsRequest.Ids.Add(item.Id.ToString());
        var many = await client.GetItemsByIdsAsync(idsRequest, headers);
        many.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task SkillQuery_Should_Preserve_Tags()
    {
        await using var factory = new GameServerFactory(_fixture);
        using var scope = factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IModelRepository>();
        var mongo = scope.ServiceProvider.GetRequiredService<IMongoRepository>();
        var skillMapper = scope.ServiceProvider.GetRequiredService<IModelMapper<DomainSkill, SkillDocument>>();

        var skill = DomainSkill.Create("Fireball", "Test skill");
        skill.Tags.Add("magic");
        skill.Tags.Add("aoe");

        await repo.UpsertAsync(skill);
        await mongo.UpsertAsync(skillMapper.ToPersistence(skill));

        var auth = await factory.CreateAuthenticatedChannelAsync("skill-query-tester");
        using var channel = auth.Channel;
        var headers = auth.Headers;
        var client = new SkillQuery.SkillQueryClient(channel);

        var single = await client.GetSkillAsync(new SkillGetByIdRequest { Id = skill.Id.ToString() }, headers);
        single.Skill.Should().NotBeNull();
        single.Skill.Id.Should().Be(skill.Id.ToString());
        single.Skill.Tags.Should().Contain(new[] { "magic", "aoe" });

        var list = await client.ListSkillsAsync(new SkillListRequest(), headers);
        list.Skills.Should().NotBeEmpty();

        var idsRequest = new SkillGetByIdsRequest();
        idsRequest.Ids.Add(skill.Id.ToString());
        var many = await client.GetSkillsByIdsAsync(idsRequest, headers);
        many.Skills.Should().ContainSingle();
    }

    [Fact]
    public async Task NpcQuery_Should_Preserve_Tags()
    {
        await using var factory = new GameServerFactory(_fixture);
        using var scope = factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IModelRepository>();
        var mongo = scope.ServiceProvider.GetRequiredService<IMongoRepository>();
        var npcMapper = scope.ServiceProvider.GetRequiredService<IModelMapper<DomainNpc, NpcDocument>>();

        var worldId = Guid.NewGuid();
        var location = DomainLocation.Create(System.Numerics.Vector3.Zero, worldId);
        var npc = DomainNpc.Create("Goblin", "A sneaky goblin", location, worldId);
        npc.Tags.Add("enemy");
        npc.Tags.Add("melee");

        await repo.UpsertAsync(npc);
        await mongo.UpsertAsync(npcMapper.ToPersistence(npc));

        var auth = await factory.CreateAuthenticatedChannelAsync("npc-query-tester");
        using var channel = auth.Channel;
        var headers = auth.Headers;
        var client = new NpcQuery.NpcQueryClient(channel);

        var single = await client.GetNpcAsync(new NpcGetByIdRequest { Id = npc.Id.ToString() }, headers);
        single.Npc.Should().NotBeNull();
        single.Npc.Id.Should().Be(npc.Id.ToString());
        single.Npc.Tags.Should().Contain(new[] { "enemy", "melee" });

        var list = await client.ListNpcsAsync(new NpcListRequest(), headers);
        list.Npcs.Should().NotBeEmpty();

        var idsRequest = new NpcGetByIdsRequest();
        idsRequest.Ids.Add(npc.Id.ToString());
        var many = await client.GetNpcsByIdsAsync(idsRequest, headers);
        many.Npcs.Should().ContainSingle();
    }
}
