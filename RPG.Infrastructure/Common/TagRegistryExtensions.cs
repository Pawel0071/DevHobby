using System;
using System.Collections.Generic;
using RPG.Abstractions;
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
        return TagRegistryHelper.GetRequiredComponents(tags, target, registry.Get);
    }

    public static bool IsTagMapped(
        this IDictionaryRegistry<TagDefinition> registry,
        string tag,
        TagTarget target = TagTarget.Item)
    {
        return TagRegistryHelper.IsTagMapped(tag, target, registry.Get);
    }
}
