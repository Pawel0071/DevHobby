using Microsoft.Extensions.DependencyInjection;
using RPG.Domain.Common;
using RPG.Infrastructure.Helpers;
using RPG.Infrastructure.Interfaces;
namespace RPG.Infrastructure.Repositories.Orchestrators
{
    public class DocumentRepository : IDocumentRepository
    {
        private readonly IDocumentTypeResolver _typeResolver;
        private readonly IMongoDocumentRepository _mongoRepository;
        private readonly IRedisDocumentRepository _redisRepository;
        private readonly IRabbitMqPublisher _rabbitMqPublisher;
        private readonly ILogger<DocumentRepository> _logger;

        public DocumentRepository(
            IDocumentTypeResolver typeResolver,
            IMongoDocumentRepository mongoRepository,
            IRedisDocumentRepository redisRepository,
            IRabbitMqPublisher rabbitMqPublisher,
            ILogger<DocumentRepository> logger)
        {
            _typeResolver = typeResolver;
            _mongoRepository = mongoRepository;
            _redisRepository = redisRepository;
            _rabbitMqPublisher = rabbitMqPublisher;
            _logger = logger;
        }

        private IDocumentRepositoryHandler<TEntity> GetHandler<TEntity>() where TEntity : class, IDomainEntity
        {
            var (documentType, mapper) = _typeResolver.GetMapping<TEntity>();
            _logger.Debug($"Creating handler for {typeof(TEntity).Name} -> {documentType.Name}");
            var handlerType = typeof(DocumentRepositoryHandler<,>).MakeGenericType(typeof(TEntity), documentType);
            
            var handler = Activator.CreateInstance(handlerType, _mongoRepository, _redisRepository, _rabbitMqPublisher, mapper, _logger);

            if (handler is null)
            {
                _logger.Error($"Could not create instance of handler for {typeof(TEntity).Name}");
                throw new InvalidOperationException($"Could not create instance of handler for {typeof(TEntity).Name}");
            }
            
            return (IDocumentRepositoryHandler<TEntity>)handler;
        }

        public Task UpsertAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class, IDomainEntity
        {
            return GetHandler<TEntity>().UpsertAsync(entity, cancellationToken);
        }

        public Task<TEntity?> GetByIdAsync<TEntity>(object id, CancellationToken cancellationToken = default) where TEntity : class, IDomainEntity
        {
            return GetHandler<TEntity>().GetByIdAsync(id, cancellationToken);
        }

        public Task<List<TEntity>> GetAllAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class, IDomainEntity
        {
            return GetHandler<TEntity>().GetAllAsync(cancellationToken);
        }

        public Task<List<TEntity>> GetBatchAsync<TEntity>(int skip, int limit, CancellationToken cancellationToken = default) where TEntity : class, IDomainEntity
        {
            return GetHandler<TEntity>().GetBatchAsync(skip, limit, cancellationToken);
        }

        public Task<long> CountAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class, IDomainEntity
        {
            return GetHandler<TEntity>().CountAsync(cancellationToken);
        }

        public Task<bool> DeleteAsync<TEntity>(object id, CancellationToken cancellationToken = default) where TEntity : class, IDomainEntity
        {
            return GetHandler<TEntity>().DeleteAsync(id, cancellationToken);
        }
    }
}
