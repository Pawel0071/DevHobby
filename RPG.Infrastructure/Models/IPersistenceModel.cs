using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RPG.Infrastructure.Models;

/// <summary>
///     Kontrakt modelu trwałości przechowywanego w MongoDB/Redis i przesyłanego przez RabbitMQ.
///     Wymagany do łatwej serializacji do JSON/BSON oraz budowania nazw kolekcji.
/// </summary>
public interface IPersistenceModel
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    static abstract string CollectionName { get; }

    Guid Id { get; set; }
}
