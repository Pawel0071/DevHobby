using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using RabbitMQ.Client;
using RPG.Infrastructure.HealthChecks;
using RPG.Infrastructure.Interfaces;
using StackExchange.Redis;

namespace RPG.UnitTest.Infrastructure.HealthChecks;

/// <summary>
///     Tests for Health Checks - MongoDB, Redis, RabbitMQ connectivity checks
/// </summary>
public class HealthCheckTests
{
    #region MongoHealthCheck Tests

    [Fact]
    public async Task MongoHealthCheck_WhenHealthy_ReturnsHealthy()
    {
        // Arrange
        var mockDatabase = new Mock<IMongoDatabase>();
        var mockLogger = new Mock<ILogger<MongoHealthCheck>>();

        mockDatabase
            .Setup(x => x.RunCommandAsync<BsonDocument>(
                It.IsAny<Command<BsonDocument>>(),
                It.IsAny<ReadPreference>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BsonDocument());

        var healthCheck = new MongoHealthCheck(mockDatabase.Object, mockLogger.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Be("MongoDB is responsive");
    }

    [Fact]
    public async Task MongoHealthCheck_WhenUnhealthy_ReturnsUnhealthy()
    {
        // Arrange
        var mockDatabase = new Mock<IMongoDatabase>();
        var mockLogger = new Mock<ILogger<MongoHealthCheck>>();

        mockDatabase
            .Setup(x => x.RunCommandAsync<BsonDocument>(
                It.IsAny<Command<BsonDocument>>(),
                It.IsAny<ReadPreference>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MongoException("Connection failed"));

        var healthCheck = new MongoHealthCheck(mockDatabase.Object, mockLogger.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("MongoDB is not responsive");
        result.Exception.Should().NotBeNull();
    }

    [Fact]
    public async Task MongoHealthCheck_PassesCancellationToken()
    {
        // Arrange
        var mockDatabase = new Mock<IMongoDatabase>();
        var mockLogger = new Mock<ILogger<MongoHealthCheck>>();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        mockDatabase
            .Setup(x => x.RunCommandAsync<BsonDocument>(
                It.IsAny<Command<BsonDocument>>(),
                It.IsAny<ReadPreference>(),
                token))
            .ReturnsAsync(new BsonDocument());

        var healthCheck = new MongoHealthCheck(mockDatabase.Object, mockLogger.Object);

        // Act
        await healthCheck.CheckHealthAsync(new HealthCheckContext(), token);

        // Assert
        mockDatabase.Verify(
            x => x.RunCommandAsync<BsonDocument>(
                It.IsAny<Command<BsonDocument>>(),
                It.IsAny<ReadPreference>(),
                token),
            Times.Once);
    }

    #endregion

    #region RedisHealthCheck Tests

    [Fact]
    public async Task RedisHealthCheck_WhenHealthy_ReturnsHealthy()
    {
        // Arrange
        var mockRedis = new Mock<IConnectionMultiplexer>();
        var mockDatabase = new Mock<IDatabase>();
        var mockLogger = new Mock<ILogger<RedisHealthCheck>>();

        mockRedis.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(mockDatabase.Object);

        mockDatabase.Setup(x => x.PingAsync(It.IsAny<CommandFlags>()))
            .ReturnsAsync(TimeSpan.FromMilliseconds(10));

        var healthCheck = new RedisHealthCheck(mockRedis.Object, mockLogger.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Be("Redis is responsive");
    }

    [Fact]
    public async Task RedisHealthCheck_WhenUnhealthy_ReturnsUnhealthy()
    {
        // Arrange
        var mockRedis = new Mock<IConnectionMultiplexer>();
        var mockDatabase = new Mock<IDatabase>();
        var mockLogger = new Mock<ILogger<RedisHealthCheck>>();

        mockRedis.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(mockDatabase.Object);

        mockDatabase.Setup(x => x.PingAsync(It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection failed"));

        var healthCheck = new RedisHealthCheck(mockRedis.Object, mockLogger.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("Redis is not responsive");
        result.Exception.Should().NotBeNull();
    }

    #endregion

    #region RabbitMqHealthCheck Tests

    [Fact]
    public async Task RabbitMqHealthCheck_WhenConnectionOpen_ReturnsHealthy()
    {
        // Arrange
        var mockConnection = new Mock<IConnection>();
        var mockLogger = new Mock<ILogger<RabbitMqHealthCheck>>();

        mockConnection.Setup(x => x.IsOpen).Returns(true);

        var healthCheck = new RabbitMqHealthCheck(mockLogger.Object, mockConnection.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Be("RabbitMQ connection is open");
    }

    [Fact]
    public async Task RabbitMqHealthCheck_WhenConnectionClosed_ReturnsDegraded()
    {
        // Arrange
        var mockConnection = new Mock<IConnection>();
        var mockLogger = new Mock<ILogger<RabbitMqHealthCheck>>();

        mockConnection.Setup(x => x.IsOpen).Returns(false);

        var healthCheck = new RabbitMqHealthCheck(mockLogger.Object, mockConnection.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Be("RabbitMQ connection is closed");
    }

    [Fact]
    public async Task RabbitMqHealthCheck_WhenConnectionNull_ReturnsHealthy()
    {
        // Arrange - null connection (using NullPublisher pattern)
        var mockLogger = new Mock<ILogger<RabbitMqHealthCheck>>();

        var healthCheck = new RabbitMqHealthCheck(mockLogger.Object, connection: null);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Be("RabbitMQ not configured - using NullPublisher");
    }

    [Fact]
    public async Task RabbitMqHealthCheck_WhenConnectionThrows_ReturnsUnhealthy()
    {
        // Arrange
        var mockConnection = new Mock<IConnection>();
        var mockLogger = new Mock<ILogger<RabbitMqHealthCheck>>();

        mockConnection.Setup(x => x.IsOpen).Throws(new InvalidOperationException("Connection error"));

        var healthCheck = new RabbitMqHealthCheck(mockLogger.Object, mockConnection.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("RabbitMQ connection failed");
        result.Exception.Should().NotBeNull();
    }

    #endregion
}
