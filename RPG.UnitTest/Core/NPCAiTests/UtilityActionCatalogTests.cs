using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FluentAssertions;
using RPG.AI.Core;
using RPG.AI.Utility;
using RPG.AI.Utility.Actions;
using RPG.Domain.Models;
using RPG.Domain.Models.Npcs;
using RPG.Domain.Models.Skills;
using Xunit;

namespace RPG.UnitTest.Core.NPCAiTests;

public class UtilityActionCatalogTests
{
    [Fact]
    public void Patrol_Should_Issue_Move_When_Route_Available()
    {
        var context = CreateContext();
        var action = UtilityActionCatalog.Patrol("patrol", radius: 5f, waypointCount: 2, stopDistance: 0.5f, dwellTime: TimeSpan.Zero);
        var definition = new UtilityActionDefinition("patrol-wrapper", ctx => action.Execute(ctx));

        var directives = definition.Execute(context).ToArray();

        directives.Should().ContainSingle();
        directives[0].Type.Should().Be(RPG.AI.Directives.AiDirectiveType.MoveToLocation);
    }

    [Fact]
    public void Patrol_Should_Stop_At_Waypoint_When_Within_StopDistance()
    {
        var context = CreateContext();
        var action = UtilityActionCatalog.Patrol("patrol", radius: 5f, waypointCount: 2, stopDistance: 1f, dwellTime: TimeSpan.FromSeconds(2));

        // Pierwszy tick - powinien wygenerować ruch
        var directives1 = action.Execute(context).ToArray();
        directives1.Should().ContainSingle();
        directives1[0].Type.Should().Be(RPG.AI.Directives.AiDirectiveType.MoveToLocation);

        // Symulujemy, że NPC dotarł do waypointa (ustawiamy pozycję blisko)
        context.TryGetBlackboardValue<Vector3>("patrol.current_wp", out var waypoint).Should().BeTrue();
        context.Self.SetCurrentLocation(Location.Create(waypoint, context.Self.WorldId));
        context.SetBlackboardValue("patrol.arrival_time", DateTime.UtcNow);

        // Drugi tick - w trakcie dwell powinien generować Idle
        var directives2 = action.Execute(context).ToArray();
        directives2.Should().ContainSingle();
        directives2[0].Type.Should().Be(RPG.AI.Directives.AiDirectiveType.Idle);
    }

    [Fact]
    public void Patrol_Should_Move_To_Next_Waypoint_After_Dwell()
    {
        var context = CreateContext();
        var action = UtilityActionCatalog.Patrol("patrol", radius: 5f, waypointCount: 3, stopDistance: 1f, dwellTime: TimeSpan.FromSeconds(1));

        // Pierwszy waypoint
        var directives1 = action.Execute(context).ToArray();
        context.TryGetBlackboardValue<Vector3>("patrol.current_wp", out var wp1).Should().BeTrue();

        // Dotarł
        context.Self.CurrentLocation = Location.Create(wp1, context.Self.WorldId);
        context.SetBlackboardValue("patrol.arrival_time", DateTime.UtcNow.AddSeconds(-2)); // dwell minął

        // Powinien przejść do kolejnego waypointa
        var directives2 = action.Execute(context).ToArray();
        directives2.Should().ContainSingle();
        directives2[0].Type.Should().Be(RPG.AI.Directives.AiDirectiveType.MoveToLocation);

        context.TryGetBlackboardValue<Vector3>("patrol.current_wp", out var wp2).Should().BeTrue();
        wp2.Should().NotBe(wp1); // nowy waypoint
    }

    [Fact]
    public void Patrol_Should_Cycle_Through_Waypoints()
    {
        var context = CreateContext();
        var waypointCount = 3;
        var action = UtilityActionCatalog.Patrol("patrol", radius: 5f, waypointCount: waypointCount, stopDistance: 1f, dwellTime: TimeSpan.Zero);

        var visitedWaypoints = new HashSet<Vector3>();

        for (int i = 0; i < waypointCount + 1; i++)
        {
            action.Execute(context).ToArray();
            context.TryGetBlackboardValue<Vector3>("patrol.current_wp", out var wp).Should().BeTrue();

            // Symuluj dotarcie
            context.Self.CurrentLocation = Location.Create(wp, context.Self.WorldId);

            if (i < waypointCount)
            {
                visitedWaypoints.Add(wp);
            }
            else
            {
                // Po pełnym cyklu powinien wrócić do pierwszego
                visitedWaypoints.Should().Contain(wp);
            }
        }

        visitedWaypoints.Should().HaveCount(waypointCount);
    }

    private static AiContext CreateContext()
    {
        var worldId = Guid.NewGuid();
        var spawn = Location.Create(Vector3.Zero, worldId);
        var npc = Npc.Create("patrol.npc", "Patroller", spawn, worldId);
        return new AiContext
        {
            Self = npc,
            CurrentHealth = 100,
            MaxHealth = 100
        };
    }
}
