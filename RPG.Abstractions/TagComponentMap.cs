using System;
using System.Collections.Generic;
using System.Linq;
using RPG.Domain.Common;
using RPG.Domain.Enums;

namespace RPG.Abstractions;

/// <summary>
///     Globalny, statyczny słownik powiązań Tag -> Typ komponentu.
///     Budowany na podstawie <see cref="TagDefinition.Predefined"/> (dynamicznie) + aliasy.
///     Dzięki temu wszystkie mappery mogą korzystać z jednej centralnej logiki.
/// </summary>
public static class TagComponentMap
{
    private static readonly Dictionary<string, Type> TagToComponent = BuildMap();
    private static readonly Dictionary<string, string> Aliases = BuildAliases();

    /// <summary>
    ///     Zwraca typ komponentu dla danego tagu (uwzględnia aliasy i normalizację).
    ///     Zwraca null jeśli tag nie powoduje utworzenia komponentu.
    /// </summary>
    public static Type? GetComponentType(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return null;
        var normalized = Normalize(tag);
        if (Aliases.TryGetValue(normalized, out var canonical)) normalized = canonical;
        return TagToComponent.TryGetValue(normalized, out var type) ? type : null;
    }

    /// <summary>
    ///     Zwraca wszystkie typy komponentów wywnioskowane z kolekcji tagów dla podanego celu (target).
    /// </summary>
    public static IEnumerable<Type> GetRequiredComponentTypes(IEnumerable<string> tags, TagTarget target)
    {
        if (tags is null) yield break;
        var seen = new HashSet<Type>();
        foreach (var tag in tags)
        {
            var type = GetComponentType(tag);
            if (type == null) continue;
            // Sprawdź czy tagDefinition ma właściwy target
            var definition = TagDefinition.Predefined.FirstOrDefault(d => string.Equals(d.Code, Normalize(tag), StringComparison.OrdinalIgnoreCase));
            if (definition != null && definition.Target != target) continue;
            if (seen.Add(type)) yield return type;
        }
    }

    private static Dictionary<string, Type> BuildMap()
    {
        var dict = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        foreach (var def in TagDefinition.Predefined)
        {
            var type = def.ResolveComponentType();
            if (type == null) continue;
            var key = def.Code.Trim();
            if (!dict.ContainsKey(key))
            {
                dict[key] = type;
            }
        }
        return dict;
    }

    private static Dictionary<string, string> BuildAliases()
    {
        // Alias bez prefixu -> pełny tag (dla wygody wpisywania np. 'merchant' zamiast 'npc:merchant')
        var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var def in TagDefinition.Predefined)
        {
            var code = def.Code;
            var colonIndex = code.IndexOf(':');
            if (colonIndex > 0)
            {
                var suffix = code[(colonIndex + 1)..];
                // Dodaj alias tylko jeśli nie koliduje
                if (!aliases.ContainsKey(suffix))
                {
                    aliases[suffix] = code;
                }
            }
        }
        return aliases;
    }

    private static string Normalize(string tag) => tag.Trim();
}

