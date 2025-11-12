namespace RPG.Domain.Common.Interfaces;

public interface IDictionaryEntry<T>
{
    string Code { get; }
    static abstract IEnumerable<T> Predefined { get; }
}

// Marker interface – typy słowników ładowane wyłącznie z pamięci (bez MongoDB)
public interface IStaticDictionaryDefinition {}
