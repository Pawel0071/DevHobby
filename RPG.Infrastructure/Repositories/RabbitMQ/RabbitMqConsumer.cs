using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RPG.Infrastructure.Configuration;
using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.Repositories.RabbitMQ;

/// <summary>
///     RabbitMQ consumer for processing messages and persisting to MongoDB.
/// </summary>
public class RabbitMqConsumer : IRabbitMqConsumer
{
    private readonly IChannel _channel;
    private readonly string _exchangeName;
    private readonly ILogger<RabbitMqConsumer> _logger;
    private readonly string _queueName;
    private readonly string _routingKey;
    private readonly IActivityScope _activityScope;
    private string? _consumerTag;
    private Func<string, string, CancellationToken, Task>? _messageHandler;

    public RabbitMqConsumer(
        IChannel channel,
        ILogger<RabbitMqConsumer> logger,
        RabbitMqSettings settings,
        IActivityScope activityScope)
    {
        _channel = channel;
        _logger = logger;
        _exchangeName = settings.ExchangeName;
        _queueName = settings.QueueName ?? "rpg_persistence_queue";
        _routingKey = settings.RoutingKey ?? "#";
        _activityScope = activityScope;
    }

    public void SetMessageHandler(Func<string, string, CancellationToken, Task> handler)
    {
        _messageHandler = handler;
    }

    public async Task StartConsumingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Deklaracja exchange
            await _channel.ExchangeDeclareAsync(
                _exchangeName,
                ExchangeType.Topic,
                true,
                false,
                cancellationToken: cancellationToken);

            // Deklaracja queue
            await _channel.QueueDeclareAsync(
                _queueName,
                true,
                false,
                false,
                cancellationToken: cancellationToken);

            // Bindowanie queue do exchange
            await _channel.QueueBindAsync(
                _queueName,
                _exchangeName,
                _routingKey,
                cancellationToken: cancellationToken);

            _logger.Info(
                $"RabbitMQ configured: Exchange={_exchangeName}, Queue={_queueName}, RoutingKey={_routingKey}");

            // Quality of Service
            await _channel.BasicQosAsync(0, 1, false, cancellationToken);

            // Consumer setup
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (sender, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                using var activity = _activityScope.Start("rabbitmq.consume", new Dictionary<string, object>
                {
                    ["messaging.system"] = "rabbitmq",
                    ["messaging.destination"] = _queueName,
                    ["messaging.destination_kind"] = "queue",
                    ["messaging.operation"] = "process",
                    ["messaging.rabbitmq.routing_key"] = ea.RoutingKey,
                    ["messaging.message.payload_size_bytes"] = body.Length
                });

                _logger.Info($"Received message. RoutingKey={ea.RoutingKey}, Size={body.Length} bytes");

                try
                {
                    await ProcessMessageAsync(message, ea.RoutingKey, cancellationToken);

                    await _channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);

                    _logger.Info($"Message acknowledged. DeliveryTag={ea.DeliveryTag}");
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error processing message. DeliveryTag={ea.DeliveryTag}", ex);

                    await _channel.BasicNackAsync(
                        ea.DeliveryTag,
                        false,
                        true,
                        cancellationToken);
                }
            };

            // Start consuming
            _consumerTag = await _channel.BasicConsumeAsync(
                _queueName,
                false,
                consumer,
                cancellationToken);

            _logger.Info($"Started consuming messages from queue: {_queueName}, ConsumerTag={_consumerTag}");

            // Log that consumer is fully ready
            _logger.Info(
                $"Consumer READY: Exchange={_exchangeName}, Queue={_queueName}, RoutingKey={_routingKey}, Tag={_consumerTag}");
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
            using var activity = _activityScope.Start("rabbitmq.handle", new Dictionary<string, object>
            {
                ["messaging.system"] = "rabbitmq",
                ["messaging.destination"] = _queueName,
                ["messaging.operation"] = operationFromRoutingKey(routingKey),
                ["messaging.rabbitmq.routing_key"] = routingKey
            });

            _logger.Info($"Processing message. RoutingKey={routingKey}");

            if (_messageHandler != null)
            {
                await _messageHandler(message, routingKey, cancellationToken);
            }
            else
            {
                _logger.Warn("No message handler set - message will be ignored");
            }
        }
        catch (JsonException ex)
        {
            _logger.Error("Error deserializing message", ex);
            throw;
        }
    }

    private static string operationFromRoutingKey(string routingKey)
    {
        var parts = routingKey.Split('.');
        return parts.Length > 0 ? parts[^1] : "process";
    }
}
