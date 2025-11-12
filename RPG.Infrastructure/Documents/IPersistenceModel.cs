using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RPG.Infrastructure.Documents;

public interface IPersistenceModel
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    static abstract string CollectionName { get; }

    Guid Id { get; set; }
}
