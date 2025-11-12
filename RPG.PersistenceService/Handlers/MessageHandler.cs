using System.Text.Json;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;
using RPG.PersistenceService.Helpers;
using RPG.PersistenceService.Services;

namespace RPG.PersistenceService.Handlers;

/// <summary>
///     Handles messages from RabbitMQ and saves them to MongoDB
/// </summary>
public class MessageHandler
{
    private readonly RPG.Infrastructure.Interfaces.ILogger<MessageHandler> _logger;
    private readonly Dictionary<string, IDocumentPersistenceStrategy> _strategies;
    private readonly IServiceProvider _serviceProvider;

    public MessageHandler(
    IEnumerable<IDocumentPersistenceStrategy> strategies,
    RPG.Infrastructure.Interfaces.ILogger<MessageHandler> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _strategies = strategies.ToDictionary(s => s.CollectionName);
        
        foreach (var strategy in _strategies)
        {
            _logger.Debug($"Registered persistence strategy for collection: {strategy.Key}");
        }
    }

    public async Task HandleMessageAsync(string message, string routingKey, CancellationToken cancellationToken)
    {
        try
        {
            var collectionName = DocumentTypeMapper.GetCollectionNameFromRoutingKey(routingKey);
            var operation = DetermineOperation(routingKey);

            _logger.Info($"Processing message. Collection={collectionName}, Operation={operation}, RoutingKey={routingKey}");

            if (!_strategies.TryGetValue(collectionName, out var strategy))
            {
                _logger.Warn($"No persistence strategy found for collection: {collectionName}");
                return;
            }

            var documentType = DocumentTypeMapper.GetDocumentTypeFromCollectionName(collectionName);
            if (documentType == null)
            {
                _logger.Warn($"No document type mapping found for collection: {collectionName}");
                return;
            }
            
            var document = (IPersistenceModel?)JsonSerializer.Deserialize(message, documentType);

            if (document == null)
            {
                _logger.Warn("Failed to deserialize message to a known document type.");
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
            _logger.Error($"Error processing message. RoutingKey={routingKey}", ex);
            throw;
        }
    }

    private async Task HandleDeleteAsync(
        IDocumentPersistenceStrategy strategy,
        IPersistenceModel document,
        string collectionName,
        CancellationToken cancellationToken)
    {
        var id = document.Id.ToString();
        await strategy.DeleteAsync(id, cancellationToken);
        _logger.Info($"Document deleted from MongoDB: {collectionName}/{id}");
    }

    private async Task HandleUpsertAsync(
        IDocumentPersistenceStrategy strategy,
        IPersistenceModel document,
        string collectionName,
        CancellationToken cancellationToken)
    {
        await strategy.UpsertAsync(document, cancellationToken);
        _logger.Info($"Document upserted to MongoDB: {collectionName}/{document.Id}");
    }

    private static string DetermineOperation(string routingKey)
    {
        var parts = routingKey.Split('.');
        if (parts.Length > 1)
            return parts[^1].ToLowerInvariant();
        return "created";
    }
}
