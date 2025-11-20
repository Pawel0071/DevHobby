using System.Numerics;
using System.Reflection;
using FluentAssertions;
using Moq;
using RPG.Abstractions.Interfaces;
using RPG.AI.Core;
using RPG.AI.Directives;
using RPG.AI.Utility;
using RPG.AI.Utility.Actions;
using RPG.Core.Common;
using RPG.Core.Interfaces;
using RPG.Core.Interfaces.NpcServices;
using RPG.Core.Services.NpcServices;
using RPG.Domain.Enums;
using RPG.Domain.Models;
using RPG.Domain.Models.Interaction;
using RPG.Domain.Models.Npcs;
using RPG.Domain.Models.Npcs.NpcComponents;
using RPG.Domain.Models.Skills;
using RPG.Infrastructure.Interfaces;

namespace RPG.UnitTest.Core.NPCServicesTests;

public class NpcAiServiceTests
{
    private static readonly MethodInfo ExecuteDirectivesMethod = typeof(NpcAiService)
        .GetMethod("ExecuteDirectivesAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static readonly MethodInfo PrepareContextMethod = typeof(NpcAiService)
        .GetMethod("PrepareContext", BindingFlags.Instance | BindingFlags.NonPublic)!;

    [Fact]
    public async Task ExecuteDirectivesAsync_UseSkill_ShouldCallCombatService()
    {
        var mocks = CreateService();
        var service = mocks.Service;
        var npc = BuildNpc();
        var skill = npc.Skills.Keys.First();
        var target = BuildPlayer(new Vector3(3f, 0f, 0f), npc.WorldId);

        var context = new AiContext { Self = npc };
        context.NearbyPlayers.Add(target);

        var directive = AiDirective.UseSkill(skill, target.Id);
        var directives = new[] { directive };
        var playerLookup = new Dictionary<Guid, Character> { [target.Id] = target };

        var log = await InvokeExecuteDirectivesAsync(service, npc, context, directives, playerLookup, CancellationToken.None);

        mocks.Combat.Verify(c => c.SkillAttackAsync(
            npc,
            target,
            It.Is<Guid>(id => id == skill.Id)), Times.Once);

        log.Should().ContainSingle(entry => entry.Contains(skill.Name, StringComparison.OrdinalIgnoreCase));
        context.ThreatTable.Should().ContainKey(target.Id);
        context.IsInCombat.Should().BeTrue();
    }

    [Fact]
    public void PrepareContext_ShouldPopulateThreatTableWithHighestThreat()
    {
        var mocks = CreateService();
        var service = mocks.Service;
        var npc = BuildNpc();
        var closePlayer = BuildPlayer(new Vector3(4f, 0f, 0f), npc.WorldId);
        var farPlayer = BuildPlayer(new Vector3(12f, 0f, 0f), npc.WorldId);

        var context = InvokePrepareContext(service, npc, new[] { closePlayer, farPlayer });

        context.ThreatTable.Should().HaveCountGreaterThan(0);
        context.ThreatTable.Should().ContainKey(closePlayer.Id);
        context.ThreatTable[closePlayer.Id].Score.Should().BeGreaterThan(context.ThreatTable[farPlayer.Id].Score);
        context.Blackboard.Should().ContainKey("primaryThreatId");
        context.Blackboard["primaryThreatId"].Should().Be(closePlayer.Id);
        context.IsInCombat.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteDirectivesAsync_MoveTo_ShouldInvokeMovementService()
    {
        var mocks = CreateService();
        var service = mocks.Service;
        var npc = BuildNpc();
        var context = new AiContext { Self = npc };
        var destination = Location.Create(new Vector3(6f, 0f, 0f), npc.WorldId);
        var directive = AiDirective.MoveTo(destination, stopDistance: 0.5f);

        var log = await InvokeExecuteDirectivesAsync(service, npc, context, new[] { directive }, new Dictionary<Guid, Character>(), CancellationToken.None);

        mocks.AiAdapter.Verify(a => a.PublishAsync(
                It.Is<Npc>(n => n.Id == npc.Id),
                It.Is<AiDirective>(d => d.Type == AiDirectiveType.MoveToLocation && d.Destination != null && d.Destination.Position == destination.Position),
                It.IsAny<AiContext>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);

        log.Should().Contain(entry => entry.StartsWith("Requesting move towards destination", StringComparison.Ordinal));
    }

    private static (
        NpcAiService Service,
        Mock<IModelRepository> DocumentRepository,
        Mock<IMovementService> Movement,
        Mock<ICharacterStateBroadcaster> Broadcaster,
        Mock<ICombatService> Combat,
        Mock<IRabbitMqPublisher> Publisher,
        Mock<ILogger<NpcAiService>> Logger,
        Mock<IGameStateBroadcaster> StateBroadcaster,
        Mock<IBehaviorRegistry> BehaviorRegistry,
        Mock<IAiDirectiveEventAdapter> AiAdapter) CreateService()
    {
        var documentRepository = new Mock<IModelRepository>();
        var movement = new Mock<IMovementService>();
        var broadcaster = new Mock<ICharacterStateBroadcaster>();
        var combat = new Mock<ICombatService>();
        var publisher = new Mock<IRabbitMqPublisher>();
        var logger = new Mock<ILogger<NpcAiService>>();
        var stateBroadcaster = new Mock<IGameStateBroadcaster>();
        var aiAdapter = new Mock<IAiDirectiveEventAdapter>();
        var behaviorRegistry = new Mock<IBehaviorRegistry>();
        behaviorRegistry.Setup(r => r.GetOrCreateAgent(It.IsAny<Npc>()))
            .Returns(() => new UtilityAgent("test-agent").Register(UtilityActionCatalog.Idle("idle")));

        broadcaster.Setup(b => b.GetSnapshots()).Returns(Array.Empty<CharacterStateSnapshot>());

        movement.Setup(m => m.Move(It.IsAny<Npc>(), It.IsAny<Vector3>(), It.IsAny<float>(), It.IsAny<float?>(), It.IsAny<bool>()))
            .Returns((Npc npc, Vector3 _, float _, float? _, bool _) => ServiceResult<Location>.Ok(npc.CurrentLocation));
        movement.Setup(m => m.Stop(It.IsAny<Npc>()))
            .Returns((Npc npc) => ServiceResult<Location>.Ok(npc.CurrentLocation));

        aiAdapter.Setup(a => a.PublishAsync(It.IsAny<Npc>(), It.IsAny<AiDirective>(), It.IsAny<AiContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new NpcAiService(
            documentRepository.Object,
            movement.Object,
            broadcaster.Object,
            combat.Object,
            publisher.Object,
            logger.Object,
            stateBroadcaster.Object,
            behaviorRegistry.Object,
            aiAdapter.Object);

        return (service, documentRepository, movement, broadcaster, combat, publisher, logger, stateBroadcaster, behaviorRegistry, aiAdapter);
    }

    private static Task<IReadOnlyList<string>> InvokeExecuteDirectivesAsync(
        NpcAiService service,
        Npc npc,
        AiContext context,
        IReadOnlyList<AiDirective> directives,
        IDictionary<Guid, Character> playerLookup,
        CancellationToken cancellationToken)
    {
        var task = (Task<IReadOnlyList<string>>)ExecuteDirectivesMethod.Invoke(
            service,
            new object[] { npc, context, directives, playerLookup, cancellationToken })!;

        return task;
    }

    private static AiContext InvokePrepareContext(NpcAiService service, Npc npc, IReadOnlyList<Character> players)
    {
        return (AiContext)PrepareContextMethod.Invoke(service, new object[] { npc, players })!;
    }

    private static Npc BuildNpc()
    {
        var worldId = Guid.NewGuid();
        var spawn = Location.Create(Vector3.Zero, worldId);
        var npc = Npc.Create("Test NPC", "Test NPC", spawn, worldId);
        npc.CurrentLocation = spawn;
        npc.CurrentHealth = 150;
        npc.MaxHealth = 150;

        var combat = new CombatComponent
        {
            AggroRange = 30f,
            LeashRange = 45f
        };

        var skill = RPG.Domain.Models.Skills.Skill.Create("Slash", "Basic attack");
        skill.Tags.Add("basic-attack");
        npc.Skills[skill] = RPG.Domain.Enums.SkillAvailability.Available;

        npc.Components.Add(combat);
        return npc;
    }

    private static Character BuildPlayer(Vector3 position, Guid worldId)
    {
        var character = new Character(Guid.NewGuid(), CharacterClass.Warrior)
        {
            Id = Guid.NewGuid(),
            Name = $"Player-{Guid.NewGuid():N}",
            Class = CharacterClass.Warrior
        };

        character.MaxHealth = 200;
        character.CurrentHealth = 200;

        var location = Location.Create(position, worldId);
        character.CurrentLocation = location;
        character.IsMoving = true;
        return character;
    }
}
