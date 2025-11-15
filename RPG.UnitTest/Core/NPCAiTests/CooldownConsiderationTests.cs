using System;
using System.Numerics;
using FluentAssertions;
using RPG.AI.Core;
using RPG.AI.Utility.Considerations;
using RPG.Domain.Models.Npcs;
using RPG.Domain.Models;
using Xunit;

namespace RPG.UnitTest.Core.NPCAiTests;

public class CooldownConsiderationTests
{
    [Fact]
    public void Evaluate_Should_Return_One_When_Skill_Not_On_Cooldown()
    {
        var context = CreateContext();
        var consideration = new CooldownConsideration("test", Guid.NewGuid(), TimeSpan.FromSeconds(5));

        var score = consideration.Evaluate(context);

        score.Should().Be(1f);
    }

    [Fact]
    public void Evaluate_Should_Decay_When_Cooldown_Not_Expired()
    {
        var skillId = Guid.NewGuid();
        var consideration = new CooldownConsideration("test", skillId, TimeSpan.FromSeconds(10));
        var context = CreateContext();
        context.SkillCooldowns[skillId] = DateTime.UtcNow.AddSeconds(5);

        var score = consideration.Evaluate(context);

        score.Should().BeLessThan(1f).And.BeGreaterThan(0f);
    }

    private static AiContext CreateContext()
    {
        var worldId = Guid.NewGuid();
        var spawn = Location.Create(Vector3.Zero, worldId);
        var npc = Npc.Create("test.npc", "Test", spawn, worldId);

        return new AiContext
        {
            Self = npc
        };
    }
}
