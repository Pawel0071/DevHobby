using RPG.Domain.Common.Interfaces;
using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.Common;

public class DictionaryRegistry<T> : IDictionaryRegistry<T> where T : IDictionaryEntry<T>
{
    private readonly Dictionary<string, T> _entries = new();
    private readonly ILogger<DictionaryRegistry<T>> _logger;

    public DictionaryRegistry(ILogger<DictionaryRegistry<T>> logger)
    {
        _logger = logger;
    }

    public void Load(IEnumerable<T> entries)
    {
        _logger.Info($"Loading dictionary entries for {typeof(T).Name}");
        _entries.Clear();

        var predefinedCount = 0;
        foreach (var predefined in T.Predefined)
        {
            _entries[predefined.Code] = predefined;
            predefinedCount++;
        }

        var loadedCount = 0;
        foreach (var entry in entries)
        {
            _entries[entry.Code] = entry;
            loadedCount++;
        }

        _logger.Info(
            $"Dictionary {typeof(T).Name} loaded: {predefinedCount} predefined, {loadedCount} from storage, {_entries.Count} total");
    }

    public bool IsValid(string code)
    {
        return _entries.ContainsKey(code);
    }

    public T? Get(string code)
    {
        return _entries.TryGetValue(code, out var entry) ? entry : default;
    }

    public IReadOnlyCollection<string> Codes => _entries.Keys;

    public IReadOnlyCollection<T> All => _entries.Values;
}
