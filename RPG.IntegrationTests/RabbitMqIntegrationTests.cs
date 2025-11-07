using System.Text;
using FluentAssertions;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RPG.IntegrationTests;

public class RabbitMqIntegrationTests : IClassFixture<TestContainersFixture>
{
    private readonly TestContainersFixture _fixture;
    private readonly IChannel _channel;

    public RabbitMqIntegrationTests(TestContainersFixture fixture)
    {
        _fixture = fixture;
        _channel = _fixture.RabbitChannel;
    }

    [Fact]
    public void ShouldConnectToRabbitMq()
    {
        // Assert
        _fixture.RabbitConnection.IsOpen.Should().BeTrue();
        _channel.IsOpen.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldDeclareExchange()
    {
        // Arrange
        var exchangeName = "test_exchange";

        // Act
        await _channel.ExchangeDeclareAsync(
            exchange: exchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false
        );

        // Assert - No exception means success
        _channel.IsOpen.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldDeclareQueue()
    {
        // Arrange
        var queueName = "test_queue";

        // Act
        var result = await _channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false
        );

        // Assert
        result.QueueName.Should().Be(queueName);
        result.MessageCount.Should().Be(0);
    }

    [Fact]
    public async Task ShouldBindQueueToExchange()
    {
        // Arrange
        var exchangeName = "test_bind_exchange";
        var queueName = "test_bind_queue";
        var routingKey = "test.routing.key";

        await _channel.ExchangeDeclareAsync(exchangeName, ExchangeType.Direct, true, false);
        await _channel.QueueDeclareAsync(queueName, true, false, false);

        // Act
        await _channel.QueueBindAsync(queueName, exchangeName, routingKey);

        // Assert - No exception means success
        _channel.IsOpen.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldPublishAndConsumeMessage()
    {
        // Arrange
        var exchangeName = "test_pubsub_exchange";
        var queueName = "test_pubsub_queue";
        var routingKey = "test.message";
        var messageBody = "Hello RabbitMQ!";

        await _channel.ExchangeDeclareAsync(exchangeName, ExchangeType.Direct, true, false);
        await _channel.QueueDeclareAsync(queueName, true, false, false);
        await _channel.QueueBindAsync(queueName, exchangeName, routingKey);

        var tcs = new TaskCompletionSource<string>();

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += (sender, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            tcs.TrySetResult(message);
            return Task.CompletedTask;
        };

        await _channel.BasicConsumeAsync(queueName, autoAck: true, consumer: consumer);

        // Act
        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "text/plain"
        };

        await _channel.BasicPublishAsync(
            exchange: exchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: Encoding.UTF8.GetBytes(messageBody)
        );

        // Assert
        var receivedMessage = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        receivedMessage.Should().Be(messageBody);
    }

    [Fact]
    public async Task ShouldPublishMultipleMessages()
    {
        // Arrange
        var exchangeName = "test_multi_exchange";
        var queueName = "test_multi_queue";
        var routingKey = "test.multi";

        await _channel.ExchangeDeclareAsync(exchangeName, ExchangeType.Direct, true, false);
        await _channel.QueueDeclareAsync(queueName, true, false, false);
        await _channel.QueueBindAsync(queueName, exchangeName, routingKey);

        var receivedMessages = new List<string>();
        var messageCount = 5;
        var tcs = new TaskCompletionSource<bool>();

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += (sender, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            receivedMessages.Add(message);

            if (receivedMessages.Count == messageCount)
            {
                tcs.TrySetResult(true);
            }

            return Task.CompletedTask;
        };

        await _channel.BasicConsumeAsync(queueName, autoAck: true, consumer: consumer);

        // Act
        for (int i = 0; i < messageCount; i++)
        {
            var message = $"Message {i + 1}";
            await _channel.BasicPublishAsync(
                exchange: exchangeName,
                routingKey: routingKey,
                mandatory: false,
                body: Encoding.UTF8.GetBytes(message)
            );
        }

        // Assert
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
        receivedMessages.Should().HaveCount(messageCount);
        receivedMessages.Should().Contain("Message 1");
        receivedMessages.Should().Contain("Message 5");
    }

    [Fact]
    public async Task ShouldWorkWithFanoutExchange()
    {
        // Arrange
        var exchangeName = "test_fanout_exchange";
        var queue1 = "test_fanout_queue1";
        var queue2 = "test_fanout_queue2";
        var messageBody = "Fanout message";

        await _channel.ExchangeDeclareAsync(exchangeName, ExchangeType.Fanout, true, false);
        await _channel.QueueDeclareAsync(queue1, true, false, false);
        await _channel.QueueDeclareAsync(queue2, true, false, false);
        await _channel.QueueBindAsync(queue1, exchangeName, string.Empty);
        await _channel.QueueBindAsync(queue2, exchangeName, string.Empty);

        var received1 = new TaskCompletionSource<string>();
        var received2 = new TaskCompletionSource<string>();

        var consumer1 = new AsyncEventingBasicConsumer(_channel);
        consumer1.ReceivedAsync += (sender, ea) =>
        {
            var message = Encoding.UTF8.GetString(ea.Body.ToArray());
            received1.TrySetResult(message);
            return Task.CompletedTask;
        };

        var consumer2 = new AsyncEventingBasicConsumer(_channel);
        consumer2.ReceivedAsync += (sender, ea) =>
        {
            var message = Encoding.UTF8.GetString(ea.Body.ToArray());
            received2.TrySetResult(message);
            return Task.CompletedTask;
        };

        await _channel.BasicConsumeAsync(queue1, autoAck: true, consumer: consumer1);
        await _channel.BasicConsumeAsync(queue2, autoAck: true, consumer: consumer2);

        // Act
        await _channel.BasicPublishAsync(
            exchange: exchangeName,
            routingKey: string.Empty,
            mandatory: false,
            body: Encoding.UTF8.GetBytes(messageBody)
        );

        // Assert
        var msg1 = await received1.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var msg2 = await received2.Task.WaitAsync(TimeSpan.FromSeconds(5));

        msg1.Should().Be(messageBody);
        msg2.Should().Be(messageBody);
    }

    [Fact]
    public async Task ShouldGetQueueMessageCount()
    {
        // Arrange
        var queueName = "test_count_queue";
        await _channel.QueueDeclareAsync(queueName, true, false, false);

        // Publish 3 messages
        for (int i = 0; i < 3; i++)
        {
            await _channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queueName,
                mandatory: false,
                body: Encoding.UTF8.GetBytes($"Message {i}")
            );
        }

        // Wait a bit for messages to be queued
        await Task.Delay(100);

        // Act
        var result = await _channel.QueueDeclarePassiveAsync(queueName);

        // Assert
        result.MessageCount.Should().Be(3);
    }
}
