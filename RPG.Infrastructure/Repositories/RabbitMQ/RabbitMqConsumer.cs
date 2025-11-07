using RPG.Infrastructure.Configuration;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.Repositories.RabbitMQ;

/// <summary>
/// RabbitMQ consumer for processing messages and persisting to MongoDB.
/// </summary>
public class RabbitMqConsumer : IRabbitMqConsumer
{
    private readonly IChannel _channel;
    private readonly IDocumentRepository _documentRepository;
    private readonly Interfaces.ILogger<RabbitMqConsumer> _logger;
    private readonly string _exchangeName;
    private readonly string _queueName;
    private readonly string _routingKey;
    private string? _consumerTag;

    public RabbitMqConsumer(
        IChannel channel,
        IDocumentRepository documentRepository,
        Interfaces.ILogger<RabbitMqConsumer> logger,
        RabbitMqSettings settings)
    {
        _channel = channel;
        _documentRepository = documentRepository;
        _logger = logger;
        _exchangeName = settings.ExchangeName;
        _queueName = settings.QueueName ?? "rpg_persistence_queue";
        _routingKey = settings.RoutingKey ?? "#";
    }

    public async Task StartConsumingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Deklaracja exchange
            await _channel.ExchangeDeclareAsync(
                exchange: _exchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken);

            // Deklaracja queue
            await _channel.QueueDeclareAsync(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: cancellationToken);

            // Bindowanie queue do exchange
            await _channel.QueueBindAsync(
                queue: _queueName,
                exchange: _exchangeName,
                routingKey: _routingKey,
                cancellationToken: cancellationToken);

            _logger.Info($"RabbitMQ configured: Exchange={_exchangeName}, Queue={_queueName}, RoutingKey={_routingKey}");

            // Quality of Service
            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: cancellationToken);

            // Consumer setup
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (sender, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                _logger.Info($"Received message. RoutingKey={ea.RoutingKey}, Size={body.Length} bytes");

                try
                {
                    await ProcessMessageAsync(message, ea.RoutingKey, cancellationToken);

                    await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);

                    _logger.Info($"Message acknowledged. DeliveryTag={ea.DeliveryTag}");
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error processing message. DeliveryTag={ea.DeliveryTag}", ex);

                    await _channel.BasicNackAsync(
                        deliveryTag: ea.DeliveryTag,
                        multiple: false,
                        requeue: true,
                        cancellationToken: cancellationToken);
                }
            };

            // Start consuming
            _consumerTag = await _channel.BasicConsumeAsync(
                queue: _queueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: cancellationToken);

            _logger.Info($"Started consuming messages from queue: {_queueName}, ConsumerTag={_consumerTag}");
            
            // Log that consumer is fully ready
            _logger.Info($"Consumer READY: Exchange={_exchangeName}, Queue={_queueName}, RoutingKey={_routingKey}, Tag={_consumerTag}");
        }
        catch (Exception ex)
        {
            _logger.Error("Error starting RabbitMQ consumer", ex);
            throw;
        }
    }

    public async Task StopConsumingAsync()
    {
        try
        {
            if (_consumerTag != null)
            {
                await _channel.BasicCancelAsync(_consumerTag);
                _logger.Info($"Stopped consuming messages. ConsumerTag={_consumerTag}");
            }
        }
        catch (Exception ex)
        {
            _logger.Error("Error stopping RabbitMQ consumer", ex);
            throw;
        }
    }

    private async Task ProcessMessageAsync(string message, string routingKey, CancellationToken cancellationToken)
    {
        try
        {
            var collectionName = DetermineCollectionName(routingKey);
            var operation = DetermineOperation(routingKey);

            _logger.Info($"Processing message. Collection={collectionName}, Operation={operation}, RoutingKey={routingKey}");

            var document = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(message);

            if (document == null)
            {
                _logger.Warn("Failed to deserialize message");
                return;
            }

            // Obsługa operacji
            if (operation == "deleted")
            {
                if (document.TryGetValue("Id", out var idElement) || document.TryGetValue("id", out idElement))
                {
                    // Handle MongoDB ObjectId format { "$oid": "..." }
                    Guid id;
                    if (idElement.ValueKind == JsonValueKind.Object && idElement.TryGetProperty("$oid", out var oidProperty))
                    {
                        // MongoDB ObjectId - use string value as GUID
                        var oidString = oidProperty.GetString();
                        id = Guid.Parse(oidString ?? throw new InvalidOperationException("ObjectId is null"));
                    }
                    else if (idElement.ValueKind == JsonValueKind.String)
                    {
                        id = Guid.Parse(idElement.GetString() ?? throw new InvalidOperationException("Id is null"));
                    }
                    else
                    {
                        id = idElement.GetGuid();
                    }
                    
                    await _documentRepository.DeleteAsync(collectionName, id, cancellationToken);
                }
                else
                {
                    _logger.Warn($"Cannot delete document without ID. RoutingKey={routingKey}");
                }
            }
            else
            {
                await _documentRepository.UpsertAsync(collectionName, document, cancellationToken);
            }

            // Audit w Outbox
            await _documentRepository.SaveToOutboxAsync(routingKey, message, cancellationToken);
        }
        catch (JsonException ex)
        {
            _logger.Error("Error deserializing message", ex);
            throw;
        }
    }

    private string DetermineCollectionName(string routingKey)
    {
        var parts = routingKey.Split('.');
        if (parts.Length > 0)
        {
            var entityType = parts[0];
            return entityType switch
            {
                "character" => "Characters",
                "item" => "Items",
                "skill" => "Skills",
                "quest" => "Quests",
                "world" => "Worlds",
                _ => "GenericDocuments"
            };
        }

        return "GenericDocuments";
    }

    private string DetermineOperation(string routingKey)
    {
        var parts = routingKey.Split('.');
        if (parts.Length > 1)
        {
            return parts[^1].ToLowerInvariant();
        }
        return "created";
    }
}
