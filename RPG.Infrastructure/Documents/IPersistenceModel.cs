using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RPG.Infrastructure.Documents;

/// <summary>
///     Kontrakt modelu trwałości przechowywanego w MongoDB/Redis i przesyłanego przez RabbitMQ.
///     Wymagany do łatwej serializacji do JSON/BSON oraz budowania nazw kolekcji.
/// </summary>
public interface IPersistenceModel
{
    /// <summary>
    ///     Nazwa kolekcji w MongoDB. Statyczna własność implementacji typu dokumentu.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    static abstract string CollectionName { get; }

    /// <summary>
    ///     Identyfikator encji, serializowany jako string GUID (kompatybilny z JSON/BSON).
    /// </summary>
    Guid Id { get; set; }
}
