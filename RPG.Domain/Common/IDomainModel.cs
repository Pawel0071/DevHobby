namespace RPG.Domain.Common;

/// <summary>
///     Bazowy kontrakt dla encji domenowych. Zapewnia niezmienność tożsamości oraz neutralność względem mechanizmów serializacji.
///     Implementacje nie powinny zawierać bezpośrednich atrybutów (np. Bson/Json) – mapowanie realizuje warstwa infrastruktury.
/// </summary>
public interface IDomainModel
{
    /// <summary>
    ///     Unikalny identyfikator encji domenowej (GUID). Ustalany przy tworzeniu / hydracji.
    /// </summary>
    Guid Id { get; }
}
