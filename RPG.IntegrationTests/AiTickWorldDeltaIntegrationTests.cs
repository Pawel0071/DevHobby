using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RPG.GameServer.Protos;
using RPG.Domain.Models.Npcs;
using RPG.Core.Interfaces.NpcServices;
using System.Numerics;
using RPG.Infrastructure.Interfaces;
using System.Reflection;

namespace RPG.IntegrationTests;

// Test integracyjny: pojedynczy tick AI powinien wygenerować deltę NPC w strumieniu WorldState.
// Pipeline: NpcAiService.TickAsync -> IAiDirectiveEventAdapter.PublishSequenceAsync -> GameStateBroadcastAdapter.Enqueue -> GameDeltaBuffer -> WorldService.StreamWorldState
public sealed class AiTickWorldDeltaIntegrationTests : IClassFixture<TestContainersFixture>
{
    private readonly TestContainersFixture _fixture;

    public AiTickWorldDeltaIntegrationTests(TestContainersFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AiTick_Should_Produce_NpcDelta_In_WorldStream()
    {
        using var factory = new GameServerFactory(_fixture);
        var (channel, headers) = await factory.CreateAuthenticatedChannelAsync("ai-tick-char", CancellationToken.None);

        var worldClient = new WorldService.WorldServiceClient(channel);
        var sessionId = headers.GetValue("x-session-id")!;

        // Ustal worldId poprzez JoinWorld (preferowany istniejący świat sesji)
        var joinReply = await worldClient.JoinWorldAsync(new JoinWorldRequest { SessionId = sessionId }, headers);
        var worldId = Guid.Parse(joinReply.Snapshot.Metadata.WorldId);

        // Seed NPC w pamięci AI (repo upsert nie trafia do Mongo w trybie testowym)
        using var scope = factory.Services.CreateScope();
        var npcAi = scope.ServiceProvider.GetRequiredService<INpcAiService>();

        var spawn = RPG.Domain.Models.Location.Create(Vector3.Zero, worldId);
        var npc = Npc.Create("Integration NPC", "Integration NPC", spawn, worldId);
        npc.SetCurrentLocation(spawn);

        // Refleksyjnie dodaj do cache AI
        var field = npcAi.GetType().GetField("_npcs", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null)
        {
            var dict = (System.Collections.Concurrent.ConcurrentDictionary<Guid, Npc>)field.GetValue(npcAi)!;
            dict[npc.Id] = npc;
        }

        // Tick AI (powinien opublikować deltę dla npc)
        await npcAi.TickAsync(CancellationToken.None);

        var streamCall = worldClient.StreamWorldState(new WorldStreamRequest
        {
            SessionId = sessionId,
            WorldId = worldId.ToString(),
            IntervalMilliseconds = 250
        }, headers);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var moveNext = await streamCall.ResponseStream.MoveNext(cts.Token);
        moveNext.Should().BeTrue();
        var update = streamCall.ResponseStream.Current;

        update.Delta.Should().NotBeNull();
        update.Delta.Npcs.Count.Should().BeGreaterThan(0);
        update.Delta.Npcs.Any(d => d.NpcId == npc.Id.ToString()).Should().BeTrue();
        update.Delta.Npcs.First(d => d.NpcId == npc.Id.ToString()).Location.Should().NotBeNull();
    }
}
