using RPG.Domain.Common.Interfaces;

namespace RPG.Infrastructure.Interfaces;

public interface IDictionaryRegistry<T> where T : IDictionaryEntry<T>
{
    void Load(IEnumerable<T> entries);
    bool IsValid(string code);
    T? Get(string code);
    IReadOnlyCollection<string> Codes { get; }
    IReadOnlyCollection<T> All { get; }
}