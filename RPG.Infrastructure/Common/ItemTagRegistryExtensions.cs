using RPG.Domain.Common;
using RPG.Domain.Entities.Items.ItemComponent;
using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.Common;

public static class ItemTagRegistryExtensions
{
    private static readonly Dictionary<string, Type> TagToComponentMap = new()
    {
        ["equippable"] = typeof(EquippableComponent),
        ["grants:skill"] = typeof(SkillGrantComponent),
        ["socketable"] = typeof(SocketComponent),
        ["stats"] = typeof(StatsComponent),
        ["material"] = typeof(CraftMaterialComponent),
        ["quest"] = typeof(QuestItemComponent)
    };

    public static IEnumerable<Type> GetRequiredComponents(this IDictionaryRegistry<ItemTagDefinition> registry,
        IEnumerable<string> tags)
    {
        var result = new HashSet<Type>();

        foreach (var tag in tags)
            if (TagToComponentMap.TryGetValue(tag, out var componentType) && registry.IsValid(tag))
                result.Add(componentType);

        return result;
    }

    public static bool IsTagMapped(this IDictionaryRegistry<ItemTagDefinition> registry, string tag)
    {
        return TagToComponentMap.ContainsKey(tag) && registry.IsValid(tag);
    }
}
