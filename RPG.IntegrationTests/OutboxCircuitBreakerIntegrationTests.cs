using System;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RPG.Infrastructure;
using RPG.Infrastructure.Outbox;
using StackExchange.Redis;
using Xunit;

namespace RPG.IntegrationTests;

public class OutboxCircuitBreakerIntegrationTests : IClassFixture<TestContainersFixture>
{
    private readonly TestContainersFixture _fixture;

    public OutboxCircuitBreakerIntegrationTests(TestContainersFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(Timeout = 30000)]
    public async Task OutboxDispatcher_ShouldPublishMessage_FromRedisPendingList()
    {
        // Arrange: create host for CircuitBreaker service with real Redis & Rabbit
        using var host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(cfg =>
            {
                var rabbitUri = new Uri(_fixture.RabbitConnectionString);
                cfg.AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string,string>("ConnectionStrings:Redis", NormalizeRedis(_fixture.RedisConnectionString) ?? string.Empty),
                    new KeyValuePair<string,string>("ConnectionStrings:Mongo", _fixture.MongoConnectionString),
                    new KeyValuePair<string,string>("RabbitMQ:Host", rabbitUri.Host),
                    new KeyValuePair<string,string>("RabbitMQ:Port", rabbitUri.Port.ToString()),
                    new KeyValuePair<string,string>("RabbitMQ:Username", rabbitUri.UserInfo.Split(':')[0]),
                    new KeyValuePair<string,string>("RabbitMQ:Password", rabbitUri.UserInfo.Split(':')[1]),
                    new KeyValuePair<string,string>("RabbitMQ:VirtualHost", "/"),
                    new KeyValuePair<string,string>("RabbitMQ:ExchangeName", "rpg_exchange"),
                    new KeyValuePair<string,string>("RabbitMQ:ExchangeType", "topic"),
                    new KeyValuePair<string,string>("Outbox:Enabled", "true"),
                    new KeyValuePair<string,string>("Outbox:Role", "Processor")
                }!);
            })
            .ConfigureServices((ctx, services) =>
            {
                services.AddInfrastructure(ctx.Configuration, "RPG.CircuitBreaker.IntegrationTest");
            })
            .Build();

        await host.StartAsync();

        var redisDb = _fixture.RedisDatabase; // real redis from fixture
        var channel = _fixture.RabbitChannel; // real rabbitmq channel

        // Ensure exchange exists and prepare a temp queue to capture the published message
        await channel.ExchangeDeclareAsync("rpg_exchange", RabbitMQ.Client.ExchangeType.Topic, durable: true, autoDelete: false);
        var queueName = await channel.QueueDeclareAsync("", exclusive: true, autoDelete: true);
        await channel.QueueBindAsync(queue: queueName, exchange: "rpg_exchange", routingKey: "test.topic");

        var outboxMessage = new OutboxMessage
        {
            Topic = "test.topic",
            Payload = JsonSerializer.Serialize(new { value = 123 })
        };

        var serialized = JsonSerializer.Serialize(outboxMessage);
        await redisDb.ListLeftPushAsync("outbox:pending", serialized);

        // Act: poll queue for the message
        var deadline = DateTime.UtcNow.AddSeconds(10);
        bool received = false;
        while (DateTime.UtcNow < deadline && !received)
        {
            var result = await channel.BasicGetAsync(queueName, autoAck: true);
            if (result != null)
            {
                received = true;
                result.Body.ToArray().Length.Should().BeGreaterThan(0);
            }
            else
            {
                await Task.Delay(300);
            }
        }

        // Assert
        received.Should().BeTrue("OutboxDispatcher should have published the pending message");

        await host.StopAsync();
    }

    private static string? NormalizeRedis(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return connectionString;
        if (!connectionString.Contains("://", StringComparison.Ordinal)) return connectionString; // already host:port
        var uri = new Uri(connectionString);
        return uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{uri.Port}";
    }
}
