using System.Text.Json;
using Microsoft.Extensions.Logging;
using RPG.Infrastructure.Documents;
using RPG.PersistenceService.Helpers;
using RPG.PersistenceService.Services;

namespace RPG.PersistenceService.Handlers;

/// <summary>
///     Handles messages from RabbitMQ and saves them to MongoDB
/// </summary>
public class MessageHandler
{
    private readonly ILogger<MessageHandler> _logger;
    private readonly Dictionary<string, IDocumentPersistenceStrategy> _strategies;
    private readonly IServiceProvider _serviceProvider;

    public MessageHandler(
        IEnumerable<IDocumentPersistenceStrategy> strategies,
        ILogger<MessageHandler> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _strategies = strategies.ToDictionary(s => s.CollectionName);
        
        foreach (var strategy in _strategies)
        {
            _logger.LogDebug($"Registered persistence strategy for collection: {strategy.Key}");
        }
    }

    public async Task HandleMessageAsync(string message, string routingKey, CancellationToken cancellationToken)
    {
        try
        {
            var collectionName = DocumentTypeMapper.GetCollectionNameFromRoutingKey(routingKey);
            var operation = DetermineOperation(routingKey);

            _logger.LogDebug($"Processing message. Collection={collectionName}, Operation={operation}, RoutingKey={routingKey}");

            if (!_strategies.TryGetValue(collectionName, out var strategy))
            {
                _logger.LogWarning($"No persistence strategy found for collection: {collectionName}");
                return;
            }

            var documentType = DocumentTypeMapper.GetDocumentTypeFromCollectionName(collectionName);
            if (documentType == null)
            {
                _logger.LogWarning($"No document type mapping found for collection: {collectionName}");
                return;
            }
            
            var document = (IMongoDocument?)JsonSerializer.Deserialize(message, documentType);

            if (document == null)
            {
                _logger.LogWarning("Failed to deserialize message to a known document type.");
                return;
            }

            if (operation == "deleted")
            {
                await HandleDeleteAsync(strategy, document, collectionName, cancellationToken);
            }
            else
            {
                await HandleUpsertAsync(strategy, document, collectionName, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing message. RoutingKey={routingKey}");
            throw;
        }
    }

    private async Task HandleDeleteAsync(
        IDocumentPersistenceStrategy strategy,
        IMongoDocument document,
        string collectionName,
        CancellationToken cancellationToken)
    {
        var id = document.Id.ToString();
        await strategy.DeleteAsync(id, cancellationToken);
        _logger.LogDebug($"Document deleted from MongoDB: {collectionName}/{id}");
    }

    private async Task HandleUpsertAsync(
        IDocumentPersistenceStrategy strategy,
        IMongoDocument document,
        string collectionName,
        CancellationToken cancellationToken)
    {
        await strategy.UpsertAsync(document, cancellationToken);
        _logger.LogDebug($"Document upserted to MongoDB: {collectionName}/{document.Id}");
    }

    private static string DetermineOperation(string routingKey)
    {
        var parts = routingKey.Split('.');
        if (parts.Length > 1)
            return parts[^1].ToLowerInvariant();
        return "created";
    }
}
