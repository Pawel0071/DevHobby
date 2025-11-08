namespace RPG.Domain.Common.Interfaces;

public interface IDictionaryEntry<T>
{
    string Code { get; }
    static abstract IEnumerable<T> Predefined { get; }
}
