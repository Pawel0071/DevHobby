using System;
using System.Collections.Generic;
using RPG.Domain.Common;
using RPG.Domain.Enums;
using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.Common;

public static class TagRegistryExtensions
{
    public static IEnumerable<Type> GetRequiredComponents(
        this IDictionaryRegistry<TagDefinition> registry,
        IEnumerable<string> tags,
        TagTarget target = TagTarget.Item)
    {
        var result = new HashSet<Type>();

        foreach (var tag in tags)
        {
            var definition = ResolveDefinition(registry, tag, target);
            if (definition is null)
            {
                continue;
            }

            var componentType = definition.ResolveComponentType();
            if (componentType != null)
            {
                result.Add(componentType);
            }
        }

        return result;
    }

    public static bool IsTagMapped(
        this IDictionaryRegistry<TagDefinition> registry,
        string tag,
        TagTarget target = TagTarget.Item)
    {
        var definition = ResolveDefinition(registry, tag, target);
        if (definition is null)
        {
            return false;
        }

        return definition.ResolveComponentType() is not null;
    }

    private static TagDefinition? ResolveDefinition(
        IDictionaryRegistry<TagDefinition> registry,
        string tag,
        TagTarget target)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return null;
        }

        var direct = registry.Get(tag);
        if (direct != null && direct.Target == target)
        {
            return direct;
        }

        if (tag.Contains(':', StringComparison.Ordinal))
        {
            return direct?.Target == target ? direct : null;
        }

        var normalized = Normalize(tag, target);
        var candidate = registry.Get(normalized);
        return candidate?.Target == target ? candidate : null;
    }

    private static string Normalize(string tag, TagTarget target)
    {
        var prefix = target switch
        {
            TagTarget.Item => "item",
            TagTarget.Skill => "skill",
            TagTarget.Quest => "quest",
            TagTarget.Npc => "npc",
            TagTarget.MapObject => "map",
            _ => "tag"
        };

        return $"{prefix}:{tag}";
    }
}
