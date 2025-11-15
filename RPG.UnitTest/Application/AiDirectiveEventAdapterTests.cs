// filepath: /Volumes/Data/Repositories/DevHobby/RPG.UnitTest/Application/AiDirectiveEventAdapterTests.cs
using System.Numerics;
using Moq;
using RPG.Abstractions.Interfaces;
using RPG.AI.Core;
using RPG.AI.Directives;
using RPG.Application.Events.Adapters;
using RPG.Domain.Models;
using RPG.Domain.Models.Npcs;
using FluentAssertions;

namespace RPG.UnitTest.Application;

public class AiDirectiveEventAdapterTests
{
    [Fact]
    public async Task Publish_MoveTo_Calls_RequestMoveAsync()
    {
        var npcOps = new Mock<INpcRequestedOperations>();
        var adapter = new AiDirectiveEventAdapter(npcOps.Object);
        var worldId = Guid.NewGuid();
        var npc = Npc.Create("test.npc", "Test NPC", Location.Create(Vector3.Zero, worldId), worldId);
        var dest = Location.Create(new Vector3(10, 0, 0), worldId);
        var directive = AiDirective.MoveTo(dest, 1f);

        var ok = await adapter.PublishAsync(npc, directive, new AiContext { Self = npc });

        ok.Should().BeTrue();
        npcOps.Verify(o => o.RequestMoveAsync(npc.Id, It.Is<Location>(l => l.Position == dest.Position), 1.0f, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Publish_Idle_Calls_RequestIdleAsync()
    {
        var npcOps = new Mock<INpcRequestedOperations>();
        var adapter = new AiDirectiveEventAdapter(npcOps.Object);
        var worldId = Guid.NewGuid();
        var npc = Npc.Create("test.npc", "Test NPC", Location.Create(Vector3.Zero, worldId), worldId);
        var directive = AiDirective.Idle();

        var ok = await adapter.PublishAsync(npc, directive, new AiContext { Self = npc });

        ok.Should().BeTrue();
        npcOps.Verify(o => o.RequestIdleAsync(npc.Id, 0f, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Publish_UseSkill_Calls_RequestUseSkillAsync()
    {
        var npcOps = new Mock<INpcRequestedOperations>();
        var adapter = new AiDirectiveEventAdapter(npcOps.Object);
        var worldId = Guid.NewGuid();
        var npc = Npc.Create("test.npc", "Test NPC", Location.Create(Vector3.Zero, worldId), worldId);
        var skillId = Guid.NewGuid();
        var directive = new AiDirective(AiDirectiveType.UseSkill, TargetId: Guid.NewGuid(), Metadata: new Dictionary<string, object?> { ["skillId"] = skillId });

        var ok = await adapter.PublishAsync(npc, directive, new AiContext { Self = npc });

        ok.Should().BeTrue();
        npcOps.Verify(o => o.RequestUseSkillAsync(npc.Id, skillId, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Publish_FollowTarget_Calls_RequestFollowAsync()
    {
        var npcOps = new Mock<INpcRequestedOperations>();
        var adapter = new AiDirectiveEventAdapter(npcOps.Object);
        var worldId = Guid.NewGuid();
        var npc = Npc.Create("test.npc", "Test NPC", Location.Create(Vector3.Zero, worldId), worldId);
        var targetId = Guid.NewGuid();
        var directive = new AiDirective(
            AiDirectiveType.FollowTarget,
            TargetId: targetId,
            DesiredRange: 2f,
            StopDistance: 2f,
            Metadata: new Dictionary<string, object?> { ["maxRange"] = 10f });

        var ok = await adapter.PublishAsync(npc, directive, new AiContext { Self = npc });

        ok.Should().BeTrue();
        npcOps.Verify(o => o.RequestFollowAsync(npc.Id, targetId, 2f, 2f, 10f, It.IsAny<CancellationToken>()), Times.Once);
    }
}
