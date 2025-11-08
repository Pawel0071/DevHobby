using RPG.Infrastructure.Helpers;

namespace RPG.PersistenceService.Helpers;

public static class DocumentTypeMapper
{
    public static string GetCollectionNameFromRoutingKey(string routingKey)
    {
        var mapping = TryGetMappingFromRoutingKey(routingKey);
        if (mapping != null)
        {
            return mapping.CollectionName;
        }
        throw new InvalidOperationException($"Unknown routing key: {routingKey}");
    }

    public static Type? GetDocumentTypeFromCollectionName(string collectionName)
    {
        return DocumentMappingRegistry.TryGetByCollectionName(collectionName)?.DocumentType;
    }

    public static Type? GetEntityTypeFromCollectionName(string collectionName)
    {
        return DocumentMappingRegistry.TryGetByCollectionName(collectionName)?.EntityType;
    }

    public static DocumentMappingDefinition? TryGetMappingFromRoutingKey(string routingKey)
    {
        var entityKey = GetEntityKeyFromRoutingKey(routingKey);
        if (string.IsNullOrWhiteSpace(entityKey))
        {
            return null;
        }

        return DocumentMappingRegistry.TryGetByEntityKey(entityKey);
    }

    public static DocumentMappingDefinition? TryGetMappingFromCollectionName(string collectionName)
    {
        return DocumentMappingRegistry.TryGetByCollectionName(collectionName);
    }

    private static string GetEntityKeyFromRoutingKey(string routingKey)
    {
        var parts = routingKey.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length > 0 ? parts[0].ToLowerInvariant() : string.Empty;
    }
}