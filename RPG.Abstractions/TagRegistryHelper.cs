using System;
using System.Collections.Generic;
using RPG.Domain.Common;
using RPG.Domain.Enums;

namespace RPG.Abstractions;

/// <summary>
///   Neutralny helper do pracy z definicjami tagów niezależny od konkretnych rejestrów/DI.
///   Umożliwia implementacje rozszerzeń na różnych adapterach (np. IDictionaryRegistry).
/// </summary>
public static class TagRegistryHelper
{
    public static IEnumerable<Type> GetRequiredComponents(
        IEnumerable<string> tags,
        TagTarget target,
        Func<string, TagDefinition?> getByCode)
    {
        var result = new HashSet<Type>();
        if (tags is null) return result;

        foreach (var tag in tags)
        {
            var definition = ResolveDefinition(getByCode, tag, target);
            var componentType = definition?.ResolveComponentType();
            if (componentType != null)
            {
                result.Add(componentType);
            }
        }

        return result;
    }

    public static bool IsTagMapped(
        string tag,
        TagTarget target,
        Func<string, TagDefinition?> getByCode)
    {
        var definition = ResolveDefinition(getByCode, tag, target);
        return definition?.ResolveComponentType() is not null;
    }

    public static TagDefinition? ResolveDefinition(
        Func<string, TagDefinition?> getByCode,
        string tag,
        TagTarget target)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return null;
        }

        var direct = getByCode(tag);
        if (direct != null && direct.Target == target)
        {
            return direct;
        }

        if (tag.Contains(':', StringComparison.Ordinal))
        {
            return direct?.Target == target ? direct : null;
        }

        var normalized = Normalize(tag, target);
        var candidate = getByCode(normalized);
        return candidate?.Target == target ? candidate : null;
    }

    public static string Normalize(string tag, TagTarget target)
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

