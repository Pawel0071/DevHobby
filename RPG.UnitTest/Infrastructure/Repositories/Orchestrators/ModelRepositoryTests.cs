using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RPG.Domain.Common;
using RPG.Infrastructure.Helpers;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Repositories.Orchestrators;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RPG.Infrastructure.Models;
using Xunit;

namespace RPG.UnitTest.Infrastructure.Repositories.Orchestrators
{
    public class ModelRepositoryTests
    {
        private readonly Mock<IDocumentTypeResolver> _typeResolver = new();
        private readonly Mock<IMongoRepository> _mongoRepository = new();
        private readonly Mock<IRedisRepository> _redisRepository = new();
        private readonly Mock<IRabbitMqPublisher> _rabbitMqPublisher = new();
        private readonly TestLogger _logger = new();
        private readonly TestMapper _mapper = new();
        private readonly ModelRepository _repository;
        private readonly CancellationToken _cancellationToken = CancellationToken.None;
        private readonly IServiceProvider _serviceProvider;

        public ModelRepositoryTests()
        {
            _typeResolver
                .Setup(r => r.GetMapping<TestModel>())
                .Returns((typeof(TestDocument), (object)_mapper));

            var services = new ServiceCollection();
            services.AddSingleton<IMongoRepository>(_mongoRepository.Object);
            services.AddSingleton<IRedisRepository>(_redisRepository.Object);
            services.AddSingleton<IRabbitMqPublisher>(_rabbitMqPublisher.Object);
            services.AddSingleton<IModelMapper<TestModel, TestDocument>>(_mapper);
            services.AddSingleton<ILogger<ModelRepository>>(_logger);
            services.AddSingleton<ILogger<ModelRepositoryHandler<TestModel, TestDocument>>>(_logger);

            _serviceProvider = services.BuildServiceProvider();

            _repository = new ModelRepository(
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
            var entity = new TestModel(Guid.NewGuid());

            await _repository.UpsertAsync(entity, _cancellationToken);

            _redisRepository.Verify(r => r.UpsertAsync(It.Is<TestDocument>(d => d.Id == entity.Id), _cancellationToken), Times.Once);
            _rabbitMqPublisher.Verify(p => p.PublishAsync("testmodel.upserted", It.Is<TestDocument>(d => d.Id == entity.Id)), Times.Once);
            _typeResolver.Verify(r => r.GetMapping<TestModel>(), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WhenCached_ShouldReturnEntity()
        {
            var id = Guid.NewGuid();
            var document = new TestDocument { Id = id };
            _redisRepository.Setup(r => r.GetByIdAsync<TestDocument>(id, _cancellationToken)).ReturnsAsync(document);

            var result = await _repository.GetByIdAsync<TestModel>(id, _cancellationToken);

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

            var result = await _repository.GetByIdAsync<TestModel>(id, _cancellationToken);

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

            var result = await _repository.GetAllAsync<TestModel>(_cancellationToken);

            result.Should().HaveCount(documents.Count);
            result.Should().OnlyContain(e => documents.Exists(d => d.Id == e.Id));
        }

        [Fact]
        public async Task GetBatchAsync_ShouldReturnMappedEntities()
        {
            var documents = new List<TestDocument> { new() { Id = Guid.NewGuid() } };
            _mongoRepository.Setup(m => m.GetBatchAsync<TestDocument>(0, 10, _cancellationToken)).ReturnsAsync(documents);

            var result = await _repository.GetBatchAsync<TestModel>(0, 10, _cancellationToken);

            result.Should().HaveCount(1);
            result[0].Id.Should().Be(documents[0].Id);
        }

        [Fact]
        public async Task CountAsync_ShouldReturnValueFromMongo()
        {
            _mongoRepository.Setup(m => m.CountAsync<TestDocument>(_cancellationToken)).ReturnsAsync(7);

            var result = await _repository.CountAsync<TestModel>(_cancellationToken);

            result.Should().Be(7);
        }

        [Fact]
        public async Task DeleteAsync_WhenDocumentExists_ShouldDeleteAndPublish()
        {
            var id = Guid.NewGuid();
            var document = new TestDocument { Id = id };

            _redisRepository.Setup(r => r.GetByIdAsync<TestDocument>(id, _cancellationToken)).ReturnsAsync(document);
            _redisRepository.Setup(r => r.DeleteAsync<TestDocument>(id, _cancellationToken)).ReturnsAsync(true);

            var result = await _repository.DeleteAsync<TestModel>(id, _cancellationToken);

            result.Should().BeTrue();
            _rabbitMqPublisher.Verify(p => p.PublishAsync("testmodel.deleted", document), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenDocumentMissing_ShouldReturnFalse()
        {
            var id = Guid.NewGuid();
            _redisRepository.Setup(r => r.GetByIdAsync<TestDocument>(id, _cancellationToken)).ReturnsAsync((TestDocument?)null);
            _mongoRepository.Setup(m => m.GetByIdAsync<TestDocument>(id, _cancellationToken)).ReturnsAsync((TestDocument?)null);

            var result = await _repository.DeleteAsync<TestModel>(id, _cancellationToken);

            result.Should().BeFalse();
            _rabbitMqPublisher.Verify(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<TestDocument>()), Times.Never);
        }

        private sealed class TestLogger : ILogger<ModelRepository>, ILogger<ModelRepositoryHandler<TestModel, TestDocument>>
        {
            public readonly List<string> InfoMessages = new();
            public readonly List<string> WarnMessages = new();
            public readonly List<(string Message, Exception? Exception)> ErrorMessages = new();
            public readonly List<string> DebugMessages = new();

            void ILogger<ModelRepository>.Info(string message) => InfoMessages.Add(message);
            void ILogger<ModelRepositoryHandler<TestModel, TestDocument>>.Info(string message) => InfoMessages.Add(message);

            void ILogger<ModelRepository>.Warn(string message) => WarnMessages.Add(message);
            void ILogger<ModelRepositoryHandler<TestModel, TestDocument>>.Warn(string message) => WarnMessages.Add(message);

            void ILogger<ModelRepository>.Error(string message, Exception? ex) => ErrorMessages.Add((message, ex));
            void ILogger<ModelRepositoryHandler<TestModel, TestDocument>>.Error(string message, Exception? ex) => ErrorMessages.Add((message, ex));

            void ILogger<ModelRepository>.Debug(string message) => DebugMessages.Add(message);
            void ILogger<ModelRepositoryHandler<TestModel, TestDocument>>.Debug(string message) => DebugMessages.Add(message);
        }

        private sealed class TestMapper : IModelMapper<TestModel, TestDocument>
        {
            public TestDocument ToPersistence(TestModel model) => new() { Id = model.Id };
            public TestModel ToDomain(TestDocument document) => new(document.Id);
        }

        private sealed class TestModel : IDomainModel
        {
            public TestModel(Guid id)
            {
                Id = id;
            }

            public Guid Id { get; }
        }

        private sealed class TestDocument : IPersistenceModel
        {
            public static string CollectionName => "test_documents";
            public Guid Id { get; set; }
        }
    }
}
