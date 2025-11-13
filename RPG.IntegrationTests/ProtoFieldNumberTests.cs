using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace RPG.IntegrationTests;

public class ProtoFieldNumberTests
{
    [Fact]
    public void QuestProto_FieldNumbers_Should_Not_Change()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "RPG.GameServer", "Protos", "v1", "quest.proto"));
        File.Exists(path).Should().BeTrue($"quest.proto not found at {path}");
        var proto = File.ReadAllText(path);

        // Wyciągamy tylko blok message Quest { ... }
        var questBlockRegex = new Regex(@"message\s+Quest\s*{(?<body>[\s\S]*?)}", RegexOptions.Multiline);
        var questMatch = questBlockRegex.Match(proto);
        questMatch.Success.Should().BeTrue("Could not locate 'message Quest' block in quest.proto");
        var body = questMatch.Groups["body"].Value;

        var fieldRegex = new Regex(@"^\s*(repeated\s+)?(\w+)\s+(\w+)\s*=\s*(\d+);", RegexOptions.Multiline);
        var matches = fieldRegex.Matches(body);
        var current = new Dictionary<int,string>();
        foreach (Match m in matches)
        {
            var name = m.Groups[3].Value;
            var number = int.Parse(m.Groups[4].Value);
            if (!current.ContainsKey(number)) current[number] = name;
        }

        var expected = new Dictionary<int,string>
        {
            [1] = "id",
            [2] = "title",
            [3] = "description",
            [4] = "quest_giver_name",
            [5] = "quest_giver_id",
            [6] = "start_location",
            [7] = "turn_in_location",
            [8] = "tags",
            [9] = "components",
            [10] = "level_requirement",
            [11] = "item_rewards",
            [12] = "kill_objective",
            [13] = "collect_objective",
            [14] = "deliver_objective",
            [15] = "explore_objective",
            [16] = "prerequisite_quests",
            [17] = "reputation_rewards",
            [18] = "repeatable",
            [19] = "time_limit"
        };

        // Porównanie – jeśli się różni wypisz różnice dla diagnostyki.
        try
        {
            current.Should().BeEquivalentTo(expected);
        }
        catch (Exception)
        {
            var diff = new List<string>();
            foreach (var kv in expected)
            {
                if (!current.TryGetValue(kv.Key, out var found)) diff.Add($"Missing field number {kv.Key} expected '{kv.Value}'");
                else if (found != kv.Value) diff.Add($"Field {kv.Key} name mismatch expected '{kv.Value}' got '{found}'");
            }
            foreach (var kv in current)
            {
                if (!expected.ContainsKey(kv.Key)) diff.Add($"Unexpected field number {kv.Key} name '{kv.Value}'");
            }
            diff.Add("Captured fields line-by-line:");
            foreach (var c in current) diff.Add($"{c.Key} => {c.Value}");
            Assert.True(false, string.Join("\n", diff));
        }
    }
}
