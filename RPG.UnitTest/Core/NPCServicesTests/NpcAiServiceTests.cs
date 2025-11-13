using System.Numerics;
using System.Reflection;
using FluentAssertions;
using Moq;
using RPG.Abstractions.Interfaces;
using RPG.AI.Core;
using RPG.AI.Directives;
using RPG.Core.Common;
using RPG.Core.Interfaces;
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
        var combat = npc.Components.OfType<CombatComponent>().First();
        var skill = combat.GetSkillsContainer().Skills.Keys.First();
        var target = BuildPlayer(new Vector3(3f, 0f, 0f), npc.WorldId);

        var context = new AiContext { Self = npc };
        context.NearbyPlayers.Add(target);

        var directive = AiDirective.UseSkill(skill, target.Id);
        var directives = new[] { directive };
        var playerLookup = new Dictionary<Guid, Character> { [target.Id] = target };

        var log = await InvokeExecuteDirectivesAsync(service, npc, context, directives, playerLookup, CancellationToken.None);

        mocks.Combat.Verify(c => c.HandleSkillUsageAsync(
            npc,
            It.Is<Skill>(s => s.Id == skill.Id),
            It.Is<Guid?>(id => id == target.Id),
            It.IsAny<CancellationToken>()), Times.Once);

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

        mocks.Movement.Verify(m => m.Move(
            npc,
            It.IsAny<Vector3>(),
            It.Is<float>(delta => Math.Abs(delta - 1f) < 0.001f),
            It.IsAny<float?>(),
            It.IsAny<bool>()), Times.Once);

        log.Should().Contain(entry => entry.StartsWith("Moving towards destination", StringComparison.Ordinal));
    }

    private static (
        NpcAiService Service,
        Mock<IModelRepository> DocumentRepository,
        Mock<IMovementService> Movement,
        Mock<ICharacterStateBroadcaster> Broadcaster,
        Mock<INpcCombatService> Combat,
        Mock<IRabbitMqPublisher> Publisher,
        Mock<ILogger<NpcAiService>> Logger) CreateService()
    {
        var documentRepository = new Mock<IModelRepository>();
        var movement = new Mock<IMovementService>();
        var broadcaster = new Mock<ICharacterStateBroadcaster>();
        var combat = new Mock<INpcCombatService>();
        var publisher = new Mock<IRabbitMqPublisher>();
        var logger = new Mock<ILogger<NpcAiService>>();

        broadcaster.Setup(b => b.GetSnapshots()).Returns(Array.Empty<CharacterStateSnapshot>());

        movement.Setup(m => m.Move(It.IsAny<Npc>(), It.IsAny<Vector3>(), It.IsAny<float>(), It.IsAny<float?>(), It.IsAny<bool>()))
            .Returns((Npc npc, Vector3 _, float _, float? _, bool _) => ServiceResult<Location>.Ok(npc.CurrentLocation));
        movement.Setup(m => m.Stop(It.IsAny<Npc>()))
            .Returns((Npc npc) => ServiceResult<Location>.Ok(npc.CurrentLocation));

        var service = new NpcAiService(
            documentRepository.Object,
            movement.Object,
            broadcaster.Object,
            combat.Object,
            publisher.Object,
            logger.Object);

        return (service, documentRepository, movement, broadcaster, combat, publisher, logger);
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
        npc.SetCurrentLocation(spawn);
        npc.CurrentHealth = 150;
        npc.MaxHealth = 150;

        var combat = new CombatComponent
        {
            AggroRange = 30f,
            LeashRange = 45f
        };

        var skill = Skill.Create("Slash", "Basic attack");
        skill.Tags.Add("basic-attack");
        combat.GetSkillsContainer().LearnSkill(skill);

        npc.Components.Add(combat);
        return npc;
    }

    private static Character BuildPlayer(Vector3 position, Guid worldId)
    {
        var character = new Character(Guid.NewGuid(), CharacterClass.Warrior)
        {
            Id = Guid.NewGuid(),
            Name = $"Player-{Guid.NewGuid():N}"
        };

        character.MaxHealth = 200;
        character.CurrentHealth = 200;

        var location = Location.Create(position, worldId);
        character.SetCurrentLocation(location);
        character.SetMovementState(true);
        return character;
    }
}
