using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RPG.Domain.Common;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Helpers;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Repositories.Orchestrators;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RPG.UnitTest.Infrastructure.Repositories.Orchestrators
{
    public class DocumentRepositoryTests
    {
        private readonly Mock<IDocumentTypeResolver> _typeResolver = new();
        private readonly Mock<IMongoDocumentRepository> _mongoRepository = new();
        private readonly Mock<IRedisDocumentRepository> _redisRepository = new();
        private readonly Mock<IRabbitMqPublisher> _rabbitMqPublisher = new();
        private readonly TestLogger _logger = new();
        private readonly TestMapper _mapper = new();
        private readonly DocumentRepository _repository;
        private readonly CancellationToken _cancellationToken = CancellationToken.None;
        private readonly IServiceProvider _serviceProvider;

        public DocumentRepositoryTests()
        {
            _typeResolver
                .Setup(r => r.GetMapping<TestEntity>())
                .Returns((typeof(TestDocument), (object)_mapper));

            var services = new ServiceCollection();
            services.AddSingleton<IMongoDocumentRepository>(_mongoRepository.Object);
            services.AddSingleton<IRedisDocumentRepository>(_redisRepository.Object);
            services.AddSingleton<IRabbitMqPublisher>(_rabbitMqPublisher.Object);
            services.AddSingleton<IDocumentMapper<TestEntity, TestDocument>>(_mapper);
            services.AddSingleton<ILogger<DocumentRepository>>(_logger);
            services.AddSingleton<ILogger<DocumentRepositoryHandler<TestEntity, TestDocument>>>(_logger);

            _serviceProvider = services.BuildServiceProvider();

            _repository = new DocumentRepository(
                _typeResolver.Object,
                _mongoRepository.Object,
                _redisRepository.Object,
                _rabbitMqPublisher.Object,
                _logger,
                _serviceProvider);
        }

        [Fact]
        public async Task UpsertAsync_ShouldDelegateToHandler()
        {
            var entity = new TestEntity(Guid.NewGuid());

            await _repository.UpsertAsync(entity, _cancellationToken);

            _redisRepository.Verify(r => r.UpsertAsync(It.Is<TestDocument>(d => d.Id == entity.Id), _cancellationToken), Times.Once);
            _rabbitMqPublisher.Verify(p => p.PublishAsync("testentity.upserted", It.Is<TestDocument>(d => d.Id == entity.Id)), Times.Once);
            _typeResolver.Verify(r => r.GetMapping<TestEntity>(), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WhenCached_ShouldReturnEntity()
        {
            var id = Guid.NewGuid();
            var document = new TestDocument { Id = id };
            _redisRepository.Setup(r => r.GetByIdAsync<TestDocument>(id, _cancellationToken)).ReturnsAsync(document);

            var result = await _repository.GetByIdAsync<TestEntity>(id, _cancellationToken);

            result.Should().NotBeNull();
            result!.Id.Should().Be(id);
        }

        [Fact]
        public async Task GetByIdAsync_WhenCacheMiss_ShouldFetchFromMongo()
        {
            var id = Guid.NewGuid();
            var document = new TestDocument { Id = id };

            _redisRepository.Setup(r => r.GetByIdAsync<TestDocument>(id, _cancellationToken)).ReturnsAsync((TestDocument?)null);
            _mongoRepository.Setup(m => m.GetByIdAsync<TestDocument>(id, _cancellationToken)).ReturnsAsync(document);

            var result = await _repository.GetByIdAsync<TestEntity>(id, _cancellationToken);

            result.Should().NotBeNull();
            _redisRepository.Verify(r => r.UpsertAsync(document, _cancellationToken), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnMappedEntities()
        {
            var documents = new List<TestDocument>
            {
                new() { Id = Guid.NewGuid() },
                new() { Id = Guid.NewGuid() }
            };

            _mongoRepository.Setup(m => m.GetAllAsync<TestDocument>(_cancellationToken)).ReturnsAsync(documents);

            var result = await _repository.GetAllAsync<TestEntity>(_cancellationToken);

            result.Should().HaveCount(documents.Count);
            result.Should().OnlyContain(e => documents.Exists(d => d.Id == e.Id));
        }

        [Fact]
        public async Task GetBatchAsync_ShouldReturnMappedEntities()
        {
            var documents = new List<TestDocument> { new() { Id = Guid.NewGuid() } };
            _mongoRepository.Setup(m => m.GetBatchAsync<TestDocument>(0, 10, _cancellationToken)).ReturnsAsync(documents);

            var result = await _repository.GetBatchAsync<TestEntity>(0, 10, _cancellationToken);

            result.Should().HaveCount(1);
            result[0].Id.Should().Be(documents[0].Id);
        }

        [Fact]
        public async Task CountAsync_ShouldReturnValueFromMongo()
        {
            _mongoRepository.Setup(m => m.CountAsync<TestDocument>(_cancellationToken)).ReturnsAsync(7);

            var result = await _repository.CountAsync<TestEntity>(_cancellationToken);

            result.Should().Be(7);
        }

        [Fact]
        public async Task DeleteAsync_WhenDocumentExists_ShouldDeleteAndPublish()
        {
            var id = Guid.NewGuid();
            var document = new TestDocument { Id = id };

            _redisRepository.Setup(r => r.GetByIdAsync<TestDocument>(id, _cancellationToken)).ReturnsAsync(document);
            _redisRepository.Setup(r => r.DeleteAsync<TestDocument>(id, _cancellationToken)).ReturnsAsync(true);

            var result = await _repository.DeleteAsync<TestEntity>(id, _cancellationToken);

            result.Should().BeTrue();
            _rabbitMqPublisher.Verify(p => p.PublishAsync("testentity.deleted", document), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenDocumentMissing_ShouldReturnFalse()
        {
            var id = Guid.NewGuid();
            _redisRepository.Setup(r => r.GetByIdAsync<TestDocument>(id, _cancellationToken)).ReturnsAsync((TestDocument?)null);
            _mongoRepository.Setup(m => m.GetByIdAsync<TestDocument>(id, _cancellationToken)).ReturnsAsync((TestDocument?)null);

            var result = await _repository.DeleteAsync<TestEntity>(id, _cancellationToken);

            result.Should().BeFalse();
            _rabbitMqPublisher.Verify(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<TestDocument>()), Times.Never);
        }

        private sealed class TestLogger : ILogger<DocumentRepository>, ILogger<DocumentRepositoryHandler<TestEntity, TestDocument>>
        {
            public readonly List<string> InfoMessages = new();
            public readonly List<string> WarnMessages = new();
            public readonly List<(string Message, Exception? Exception)> ErrorMessages = new();
            public readonly List<string> DebugMessages = new();

            void ILogger<DocumentRepository>.Info(string message) => InfoMessages.Add(message);
            void ILogger<DocumentRepositoryHandler<TestEntity, TestDocument>>.Info(string message) => InfoMessages.Add(message);

            void ILogger<DocumentRepository>.Warn(string message) => WarnMessages.Add(message);
            void ILogger<DocumentRepositoryHandler<TestEntity, TestDocument>>.Warn(string message) => WarnMessages.Add(message);

            void ILogger<DocumentRepository>.Error(string message, Exception? ex) => ErrorMessages.Add((message, ex));
            void ILogger<DocumentRepositoryHandler<TestEntity, TestDocument>>.Error(string message, Exception? ex) => ErrorMessages.Add((message, ex));

            void ILogger<DocumentRepository>.Debug(string message) => DebugMessages.Add(message);
            void ILogger<DocumentRepositoryHandler<TestEntity, TestDocument>>.Debug(string message) => DebugMessages.Add(message);
        }

        private sealed class TestMapper : IDocumentMapper<TestEntity, TestDocument>
        {
            public TestDocument ToDocument(TestEntity entity) => new() { Id = entity.Id };
            public TestEntity ToDomain(TestDocument document) => new(document.Id);
        }

        private sealed class TestEntity : IDomainEntity
        {
            public TestEntity(Guid id)
            {
                Id = id;
            }

            public Guid Id { get; }
        }

        private sealed class TestDocument : IMongoDocument
        {
            public static string CollectionName => "test_documents";
            public Guid Id { get; set; }
        }
    }
}
