using FluentAssertions;
using Moq;
using RPG.Domain.Common;
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
    public class ModelRepositoryHandlerTests
    {
        private readonly Mock<IMongoRepository> _mongoRepository = new();
        private readonly Mock<IRedisRepository> _redisRepository = new();
        private readonly Mock<IRabbitMqPublisher> _rabbitMqPublisher = new();
        private readonly Mock<IModelMapper<TestModel, TestDocument>> _mapper = new();
        private readonly Mock<ILogger<ModelRepositoryHandler<TestModel, TestDocument>>> _logger = new();
        private readonly ModelRepositoryHandler<TestModel, TestDocument> _handler;
        private readonly CancellationToken _cancellationToken = CancellationToken.None;

        public ModelRepositoryHandlerTests()
        {
            _handler = new ModelRepositoryHandler<TestModel, TestDocument>(
                _mongoRepository.Object,
                _redisRepository.Object,
                _rabbitMqPublisher.Object,
                _mapper.Object,
                _logger.Object);
        }

        [Fact]
        public async Task UpsertAsync_ShouldCacheAndPublish()
        {
            var entity = new TestModel(Guid.NewGuid());
            var document = new TestDocument { Id = entity.Id };

            _mapper.Setup(m => m.ToPersistence(entity)).Returns(document);

            await _handler.UpsertAsync(entity, _cancellationToken);

            _redisRepository.Verify(r => r.UpsertAsync(document, _cancellationToken), Times.Once);
            _rabbitMqPublisher.Verify(p => p.PublishAsync("testmodel.upserted", document), Times.Once);
            _logger.Verify(l => l.Info(It.Is<string>(msg => msg.Contains("upserted"))), Times.Once);
        }

        [Fact]
        public async Task UpsertAsync_WhenCacheFails_ShouldLogAndRethrow()
        {
            var entity = new TestModel(Guid.NewGuid());
            var document = new TestDocument { Id = entity.Id };
            var exception = new InvalidOperationException("cache error");

            _mapper.Setup(m => m.ToPersistence(entity)).Returns(document);
            _redisRepository
                .Setup(r => r.UpsertAsync(document, _cancellationToken))
                .ThrowsAsync(exception);

            var act = async () => await _handler.UpsertAsync(entity, _cancellationToken);

            await act.Should().ThrowAsync<InvalidOperationException>();
            _logger.Verify(l => l.Error(It.Is<string>(msg => msg.Contains("UpsertAsync")), exception), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WhenCached_ShouldReturnEntityWithoutMongoCall()
        {
            var id = Guid.NewGuid();
            var document = new TestDocument { Id = id };
            var entity = new TestModel(id);

            _redisRepository.Setup(r => r.GetByIdAsync<TestDocument>(id, _cancellationToken)).ReturnsAsync(document);
            _mapper.Setup(m => m.ToDomain(document)).Returns(entity);

            var result = await _handler.GetByIdAsync(id, _cancellationToken);

            result.Should().BeEquivalentTo(entity);
            _mongoRepository.Verify(m => m.GetByIdAsync<TestDocument>(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
            _redisRepository.Verify(r => r.UpsertAsync(It.IsAny<TestDocument>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetByIdAsync_WhenCacheMiss_ShouldFetchFromMongoAndPopulateCache()
        {
            var id = Guid.NewGuid();
            var document = new TestDocument { Id = id };
            var entity = new TestModel(id);

            _redisRepository.Setup(r => r.GetByIdAsync<TestDocument>(id, _cancellationToken)).ReturnsAsync((TestDocument?)null);
            _mongoRepository.Setup(m => m.GetByIdAsync<TestDocument>(id, _cancellationToken)).ReturnsAsync(document);
            _mapper.Setup(m => m.ToDomain(document)).Returns(entity);

            var result = await _handler.GetByIdAsync(id, _cancellationToken);

            result.Should().BeEquivalentTo(entity);
            _redisRepository.Verify(r => r.UpsertAsync(document, _cancellationToken), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WhenDocumentMissing_ShouldReturnNull()
        {
            var id = Guid.NewGuid();
            _redisRepository.Setup(r => r.GetByIdAsync<TestDocument>(id, _cancellationToken)).ReturnsAsync((TestDocument?)null);
            _mongoRepository.Setup(m => m.GetByIdAsync<TestDocument>(id, _cancellationToken)).ReturnsAsync((TestDocument?)null);

            var result = await _handler.GetByIdAsync(id, _cancellationToken);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAllAsync_ShouldMapAllDocuments()
        {
            var documents = new List<TestDocument>
            {
                new() { Id = Guid.NewGuid() },
                new() { Id = Guid.NewGuid() }
            };

            _mongoRepository.Setup(m => m.GetAllAsync<TestDocument>(_cancellationToken)).ReturnsAsync(documents);
            _mapper.Setup(m => m.ToDomain(It.IsAny<TestDocument>()))
                .Returns<TestDocument>(doc => new TestModel(doc.Id));

            var result = await _handler.GetAllAsync(_cancellationToken);

            result.Should().HaveCount(documents.Count);
            result.Should().OnlyContain(entity => documents.Exists(doc => doc.Id == entity.Id));
        }

        [Fact]
        public async Task GetBatchAsync_ShouldMapRequestedDocuments()
        {
            var documents = new List<TestDocument>
            {
                new() { Id = Guid.NewGuid() }
            };

            _mongoRepository.Setup(m => m.GetBatchAsync<TestDocument>(5, 10, _cancellationToken)).ReturnsAsync(documents);
            _mapper.Setup(m => m.ToDomain(It.IsAny<TestDocument>()))
                .Returns<TestDocument>(doc => new TestModel(doc.Id));

            var result = await _handler.GetBatchAsync(5, 10, _cancellationToken);

            result.Should().HaveCount(1);
            result[0].Id.Should().Be(documents[0].Id);
        }

        [Fact]
        public async Task CountAsync_ShouldReturnDocumentCount()
        {
            _mongoRepository.Setup(m => m.CountAsync<TestDocument>(_cancellationToken)).ReturnsAsync(42);

            var result = await _handler.CountAsync(_cancellationToken);

            result.Should().Be(42);
        }

        [Fact]
        public async Task DeleteAsync_WhenDocumentFoundInCache_ShouldDeleteAndPublish()
        {
            var id = Guid.NewGuid();
            var document = new TestDocument { Id = id };

            _redisRepository.Setup(r => r.GetByIdAsync<TestDocument>(id, _cancellationToken)).ReturnsAsync(document);
            _redisRepository.Setup(r => r.DeleteAsync<TestDocument>(id, _cancellationToken)).ReturnsAsync(true);

            var result = await _handler.DeleteAsync(id, _cancellationToken);

            result.Should().BeTrue();
            _rabbitMqPublisher.Verify(p => p.PublishAsync("testmodel.deleted", document), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenCacheMissButMongoHasDocument_ShouldDeleteAndPublish()
        {
            var id = Guid.NewGuid();
            var document = new TestDocument { Id = id };

            _redisRepository.Setup(r => r.GetByIdAsync<TestDocument>(id, _cancellationToken)).ReturnsAsync((TestDocument?)null);
            _mongoRepository.Setup(m => m.GetByIdAsync<TestDocument>(id, _cancellationToken)).ReturnsAsync(document);
            _redisRepository.Setup(r => r.DeleteAsync<TestDocument>(id, _cancellationToken)).ReturnsAsync(true);

            var result = await _handler.DeleteAsync(id, _cancellationToken);

            result.Should().BeTrue();
            _rabbitMqPublisher.Verify(p => p.PublishAsync("testmodel.deleted", document), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenDocumentNotFound_ShouldReturnFalse()
        {
            var id = Guid.NewGuid();
            _redisRepository.Setup(r => r.GetByIdAsync<TestDocument>(id, _cancellationToken)).ReturnsAsync((TestDocument?)null);
            _mongoRepository.Setup(m => m.GetByIdAsync<TestDocument>(id, _cancellationToken)).ReturnsAsync((TestDocument?)null);

            var result = await _handler.DeleteAsync(id, _cancellationToken);

            result.Should().BeFalse();
            _rabbitMqPublisher.Verify(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<TestDocument>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WhenDeleteFails_ShouldLogAndRethrow()
        {
            var id = Guid.NewGuid();
            var document = new TestDocument { Id = id };
            var exception = new InvalidOperationException("delete error");

            _redisRepository.Setup(r => r.GetByIdAsync<TestDocument>(id, _cancellationToken)).ReturnsAsync(document);
            _redisRepository.Setup(r => r.DeleteAsync<TestDocument>(id, _cancellationToken)).ThrowsAsync(exception);

            var act = async () => await _handler.DeleteAsync(id, _cancellationToken);

            await act.Should().ThrowAsync<InvalidOperationException>();
            _logger.Verify(l => l.Error(It.Is<string>(msg => msg.Contains("DeleteAsync")), exception), Times.Once);
        }

        public record TestModel(Guid Id) : IDomainModel;

        public class TestDocument : IPersistenceModel
        {
            public static string CollectionName => "test_documents";
            public Guid Id { get; set; }
        }
    }
}
