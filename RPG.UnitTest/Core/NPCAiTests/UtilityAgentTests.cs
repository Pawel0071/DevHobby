using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FluentAssertions;
using RPG.AI.Core;
using RPG.AI.Directives;
using RPG.AI.Utility;
using RPG.AI.Utility.Actions;
using RPG.Domain.Models;
using RPG.Domain.Models.Npcs;
using Xunit;

namespace RPG.UnitTest.Core.NPCAiTests;

public class UtilityAgentTests
{
    [Fact]
    public void Decide_Should_Select_Highest_Scoring_Action()
    {
        var agent = new UtilityAgent("test")
            .Register(new UtilityActionDefinition(
                "low",
                _ => new[] { AiDirective.Idle("low") },
                weight: 0.5f,
                predicate: _ => true))
            .Register(new UtilityActionDefinition(
                "high",
                _ => new[] { AiDirective.Idle("high") },
                weight: 2f,
                predicate: _ => true));

        var context = CreateContext();

        var decision = agent.Decide(context);

        decision.HasAction.Should().BeTrue();
        decision.Action!.Name.Should().Be("high");
        context.Directives.Should().ContainSingle();
    }

    [Fact]
    public void Decide_Should_Return_Empty_When_All_Actions_Blocked()
    {
        var agent = new UtilityAgent("test")
            .Register(new UtilityActionDefinition(
                "blocked",
                _ => Array.Empty<AiDirective>(),
                predicate: _ => false));

        var decision = agent.Decide(CreateContext());

        decision.HasAction.Should().BeFalse();
        decision.Directives.Should().BeEmpty();
    }

    private static AiContext CreateContext()
    {
        var worldId = Guid.NewGuid();
        var spawn = Location.Create(Vector3.Zero, worldId);
        var npc = Npc.Create("test.npc", "Test", spawn, worldId);
        return new AiContext { Self = npc };
    }
}

