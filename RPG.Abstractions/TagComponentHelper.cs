using System;
using System.Collections.Generic;
using System.Linq;
using RPG.Domain.Common;
using RPG.Domain.Enums;

namespace RPG.Abstractions;

/// <summary>
///     Helper do wyprowadzania kanonicznych tagów na podstawie typów komponentów
///     oraz generowania aliasów (suffixów bez prefiksu 'target:').
/// </summary>
public static class TagComponentHelper
{
    /// <summary>
    ///     Wyprowadza zestaw tagów (kanonicznych + aliasy) dla podanego targetu
    ///     na podstawie typów komponentów.
    /// </summary>
    public static HashSet<string> ResolveComponentTags(IEnumerable<Type> componentTypes, TagTarget target)
    {
        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (componentTypes is null) return codes;

        var definitions = TagDefinition.Predefined
            .Where(d => d.Target == target && d.ComponentType != null)
            .Select(d => new { d.Code, Type = d.ResolveComponentType() })
            .Where(x => x.Type != null)
            .ToArray();

        foreach (var type in componentTypes)
        {
            foreach (var def in definitions)
            {
                if (def.Type!.IsAssignableFrom(type))
                {
                    codes.Add(def.Code);
                    var colon = def.Code.IndexOf(':');
                    if (colon > 0)
                    {
                        var suffix = def.Code[(colon + 1)..];
                        codes.Add(suffix);
                    }
                }
            }
        }

        return codes;
    }
}

