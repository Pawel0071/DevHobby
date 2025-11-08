using FluentAssertions;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RPG.Infrastructure.Configuration;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Repositories.RabbitMQ;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RPG.UnitTest.Infrastructure.Repositories.RabbitMQ
{
    public class RabbitMqConsumerTests
    {
        private const string ConsumerTag = "consumer-tag";
        private readonly RabbitMqSettings _settings = new()
        {
            ExchangeName = "rpg.exchange",
            QueueName = "rpg.queue",
            RoutingKey = "rpg.#"
        };

        [Fact]
        public async Task StartConsumingAsync_ShouldConfigureTopologyAndStart()
        {
            AsyncEventingBasicConsumer? consumerCapture = null;
            var channel = CreateChannelMock(out var logger, consumer => consumerCapture = consumer);

            var subject = new RabbitMqConsumer(channel.Object, logger.Object, _settings);

            await subject.StartConsumingAsync();

            channel.Verify(c => c.ExchangeDeclareAsync(
                _settings.ExchangeName,
                ExchangeType.Topic,
                true,
                false,
                It.Is<IDictionary<string, object?>?>(args => args == null),
                false,
                false,
                It.IsAny<CancellationToken>()), Times.Once);
            channel.Verify(c => c.QueueDeclareAsync(
                _settings.QueueName!,
                true,
                false,
                false,
                It.Is<IDictionary<string, object?>?>(args => args == null),
                false,
                false,
                It.IsAny<CancellationToken>()), Times.Once);
            channel.Verify(c => c.QueueBindAsync(
                _settings.QueueName!,
                _settings.ExchangeName,
                _settings.RoutingKey!,
                It.Is<IDictionary<string, object?>?>(args => args == null),
                false,
                It.IsAny<CancellationToken>()), Times.Once);
            channel.Verify(c => c.BasicQosAsync(0, 1, false, It.IsAny<CancellationToken>()), Times.Once);
            channel.Verify(c => c.BasicConsumeAsync(
                _settings.QueueName!,
                false,
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<IDictionary<string, object?>?>(),
                It.IsAny<IAsyncBasicConsumer>(),
                It.IsAny<CancellationToken>()), Times.Once);

            logger.Verify(l => l.Info(It.Is<string>(msg => msg.Contains("Consumer READY"))), Times.Once);
            consumerCapture.Should().NotBeNull();
        }

        [Fact]
        public async Task StartConsumingAsync_ShouldProcessMessageAndAck()
        {
            AsyncEventingBasicConsumer? consumerCapture = null;
            var channel = CreateChannelMock(out var logger, consumer => consumerCapture = consumer);
            var subject = new RabbitMqConsumer(channel.Object, logger.Object, _settings);

            var handledMessages = new List<(string Message, string RoutingKey)>();
            subject.SetMessageHandler((message, routingKey, _) =>
            {
                handledMessages.Add((message, routingKey));
                return Task.CompletedTask;
            });

            await subject.StartConsumingAsync();
            consumerCapture.Should().NotBeNull();
            var consumer = consumerCapture!;

            var body = Encoding.UTF8.GetBytes("{\"id\":1}");
            await consumer.HandleBasicDeliverAsync(
                ConsumerTag,
                7UL,
                false,
                _settings.ExchangeName,
                "entity.updated",
                Mock.Of<IReadOnlyBasicProperties>(),
                body.AsMemory(),
                CancellationToken.None);

            channel.Verify(c => c.BasicAckAsync(7UL, false, It.IsAny<CancellationToken>()), Times.Once);
            handledMessages.Should().ContainSingle(tuple => tuple.RoutingKey == "entity.updated" && tuple.Message.Contains("id"));
        }

        [Fact]
        public async Task StartConsumingAsync_WhenHandlerThrows_ShouldNackAndLogError()
        {
            AsyncEventingBasicConsumer? consumerCapture = null;
            var channel = CreateChannelMock(out var logger, consumer => consumerCapture = consumer);
            var subject = new RabbitMqConsumer(channel.Object, logger.Object, _settings);

            subject.SetMessageHandler((_, _, _) => throw new InvalidOperationException("boom"));

            await subject.StartConsumingAsync();
            consumerCapture.Should().NotBeNull();
            var consumer = consumerCapture!;

            var body = Encoding.UTF8.GetBytes("{}");
            await consumer.HandleBasicDeliverAsync(
                ConsumerTag,
                11UL,
                false,
                _settings.ExchangeName,
                "entity.failed",
                Mock.Of<IReadOnlyBasicProperties>(),
                body.AsMemory(),
                CancellationToken.None);

            channel.Verify(c => c.BasicNackAsync(11UL, false, true, It.IsAny<CancellationToken>()), Times.Once);
            logger.Verify(l => l.Error(It.Is<string>(msg => msg.Contains("Error processing message")), It.IsAny<Exception>()), Times.Once);
        }

        [Fact]
        public async Task StartConsumingAsync_WithoutHandler_ShouldWarnAndAck()
        {
            AsyncEventingBasicConsumer? consumerCapture = null;
            var channel = CreateChannelMock(out var logger, consumer => consumerCapture = consumer);
            var subject = new RabbitMqConsumer(channel.Object, logger.Object, _settings);

            await subject.StartConsumingAsync();
            consumerCapture.Should().NotBeNull();
            var consumer = consumerCapture!;

            var body = Encoding.UTF8.GetBytes("{}");
            await consumer.HandleBasicDeliverAsync(
                ConsumerTag,
                21UL,
                false,
                _settings.ExchangeName,
                "entity.fallback",
                Mock.Of<IReadOnlyBasicProperties>(),
                body.AsMemory(),
                CancellationToken.None);

            channel.Verify(c => c.BasicAckAsync(21UL, false, It.IsAny<CancellationToken>()), Times.Once);
            logger.Verify(l => l.Warn(It.Is<string>(msg => msg.Contains("No message handler"))), Times.Once);
        }

        [Fact]
        public async Task StopConsumingAsync_ShouldCancelConsumer()
        {
            AsyncEventingBasicConsumer? consumerCapture = null;
            var channel = CreateChannelMock(out var logger, consumer => consumerCapture = consumer);
            var subject = new RabbitMqConsumer(channel.Object, logger.Object, _settings);

            await subject.StartConsumingAsync();
            await subject.StopConsumingAsync();

            channel.Verify(c => c.BasicCancelAsync(ConsumerTag, false, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task StartConsumingAsync_WhenInitializationFails_ShouldLogAndRethrow()
        {
            var channel = new Mock<IChannel>();
            var logger = new Mock<ILogger<RabbitMqConsumer>>();
            channel.Setup(c => c.ExchangeDeclareAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<IDictionary<string, object?>?>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("exchange error"));

            var subject = new RabbitMqConsumer(channel.Object, logger.Object, _settings);

            var act = async () => await subject.StartConsumingAsync();

            await act.Should().ThrowAsync<InvalidOperationException>();
            logger.Verify(l => l.Error(It.Is<string>(msg => msg.Contains("Error starting RabbitMQ consumer")), It.IsAny<Exception>()), Times.Once);
        }

        private static Mock<IChannel> CreateChannelMock(out Mock<ILogger<RabbitMqConsumer>> logger, Action<AsyncEventingBasicConsumer> consumerRegistered)
        {
            logger = new Mock<ILogger<RabbitMqConsumer>>();

            var channel = new Mock<IChannel>();

            channel.Setup(c => c.ExchangeDeclareAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<IDictionary<string, object?>?>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            channel.Setup(c => c.QueueDeclareAsync(
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<IDictionary<string, object?>?>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new QueueDeclareOk("queue", 0, 0));

            channel.Setup(c => c.QueueBindAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<IDictionary<string, object?>?>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            channel.Setup(c => c.BasicQosAsync(It.IsAny<uint>(), It.IsAny<ushort>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            channel.Setup(c => c.BasicConsumeAsync(
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<IDictionary<string, object?>?>(),
                    It.IsAny<IAsyncBasicConsumer>(),
                    It.IsAny<CancellationToken>()))
                .Callback<string, bool, string, bool, bool, IDictionary<string, object?>?, IAsyncBasicConsumer, CancellationToken>((queue, autoAck, tag, noLocal, exclusive, args, consumer, token) =>
                {
                    if (consumer is AsyncEventingBasicConsumer eventingConsumer)
                    {
                        consumerRegistered(eventingConsumer);
                    }
                })
                .ReturnsAsync(ConsumerTag);

            channel.Setup(c => c.BasicAckAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);

            channel.Setup(c => c.BasicNackAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);

            channel.Setup(c => c.BasicCancelAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            return channel;
        }
    }
}
