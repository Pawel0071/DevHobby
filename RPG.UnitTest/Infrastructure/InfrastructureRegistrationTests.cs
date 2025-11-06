using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RPG.Domain.Common;
using RPG.Infrastructure;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;

namespace RPG.UnitTest.Infrastructure;

public class InfrastructureRegistrationTests
{
    [Fact]
    public void AddInfrastructure_RegistersExpectedServices()
    {
        var inMemory = new List<KeyValuePair<string, string?>>
        {
            new KeyValuePair<string, string?>("ConnectionStrings:Redis", "redis://localhost"),
            new KeyValuePair<string, string?>("ConnectionStrings:Mongo", "mongodb://localhost:27017")
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemory)
            .Build();

        var services = new ServiceCollection();

        services.AddInfrastructure(config);

        // Ensure some core infra services registered
        services.Should().Contain(sd => sd.ServiceType == typeof(IRedisCache));
        services.Should().Contain(sd => sd.ServiceType == typeof(IDictionaryRegistry<ItemTypeDefinition>));
        services.Should().Contain(sd => sd.ServiceType == typeof(MongoDB.Driver.IMongoCollection<ItemDocument>));
    }
}
