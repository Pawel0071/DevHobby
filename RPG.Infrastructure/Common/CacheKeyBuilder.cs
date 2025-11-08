namespace RPG.Infrastructure.Common;

/// <summary>
///     Centralna klasa do budowania kluczy Redis z prefiksami i konwencją nazewnictwa.
/// </summary>
public static class CacheKeyBuilder
{
    private const string Separator = ":";

    // Prefixes for different entity types
    private const string CharacterPrefix = "char";
    private const string ItemPrefix = "item";
    private const string SessionPrefix = "session";
    private const string DictionaryPrefix = "dict";

    public static string Character(Guid characterId)
    {
        return $"{CharacterPrefix}{Separator}{characterId}";
    }

    public static string CharacterInventory(Guid characterId)
    {
        return $"{CharacterPrefix}{Separator}{characterId}{Separator}inventory";
    }

    public static string CharacterStats(Guid characterId)
    {
        return $"{CharacterPrefix}{Separator}{characterId}{Separator}stats";
    }

    public static string Item(string itemId)
    {
        return $"{ItemPrefix}{Separator}{itemId}";
    }

    public static string Session(Guid sessionId)
    {
        return $"{SessionPrefix}{Separator}{sessionId}";
    }

    public static string Dictionary(string dictionaryName)
    {
        return $"{DictionaryPrefix}{Separator}{dictionaryName}";
    }

    public static string Custom(string prefix, params object[] parts)
    {
        var key = prefix;
        foreach (var part in parts) key += $"{Separator}{part}";
        return key;
    }
}
