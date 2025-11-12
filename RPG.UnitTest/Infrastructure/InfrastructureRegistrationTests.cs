using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RPG.Domain.Common;
using RPG.Infrastructure;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Repositories.RabbitMQ;

namespace RPG.UnitTest.Infrastructure;

public class InfrastructureRegistrationTests
{
    [Fact]
    public void AddInfrastructure_RegistersExpectedServices()
    {
        var inMemory = new List<KeyValuePair<string, string?>>
        {
            new("ConnectionStrings:Redis", "redis://localhost"),
            new("ConnectionStrings:Mongo", "mongodb://localhost:27017")
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemory)
            .Build();

        var services = new ServiceCollection();

        services.AddInfrastructure(config);

        // Ensure some core infra services registered
    services.Should().Contain(sd => sd.ServiceType == typeof(IRedisRepository));
    services.Should().Contain(sd => sd.ServiceType == typeof(IDictionaryRegistry<TagDefinition>));
    }

    [Fact]
    public void AddInfrastructure_WhenRabbitMqConfigured_ShouldRegisterRabbitMqPublisher()
    {
        var inMemory = new List<KeyValuePair<string, string?>>
        {
            new("ConnectionStrings:Redis", "redis://localhost"),
            new("ConnectionStrings:Mongo", "mongodb://localhost:27017"),
            new("RabbitMQ:Host", "localhost"),
            new("RabbitMQ:Port", "5672"),
            new("RabbitMQ:Username", "guest"),
            new("RabbitMQ:Password", "guest"),
            new("RabbitMQ:VirtualHost", "/")
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemory)
            .Build();

        var services = new ServiceCollection();

        services.AddInfrastructure(config);

        services.Should().Contain(sd => sd.ServiceType == typeof(IRabbitMqPublisher) && sd.ImplementationType == typeof(RabbitMqPublisher));
        services.Should().Contain(sd => sd.ServiceType == typeof(IRabbitMqConsumer) && sd.ImplementationType == typeof(RabbitMqConsumer));
    }

    [Fact]
    public void AddInfrastructure_WhenRabbitMqMissing_ShouldUseNullPublisher()
    {
        var inMemory = new List<KeyValuePair<string, string?>>
        {
            new("ConnectionStrings:Redis", "redis://localhost"),
            new("ConnectionStrings:Mongo", "mongodb://localhost:27017")
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemory)
            .Build();

        var services = new ServiceCollection();
        services.AddInfrastructure(config);

        using var provider = services.BuildServiceProvider();
        var publisher = provider.GetRequiredService<IRabbitMqPublisher>();

        publisher.Should().BeOfType<NullRabbitMqPublisher>();
    }
}
