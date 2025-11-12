using Microsoft.Extensions.DependencyInjection;
using RPG.Domain.Common;
using RPG.Infrastructure.Helpers;
using RPG.Infrastructure.Interfaces;
namespace RPG.Infrastructure.Repositories.Orchestrators
{
    public class ModelRepository : IModelRepository
    {
        private readonly IDocumentTypeResolver _typeResolver;
        private readonly IMongoRepository _mongoRepository;
        private readonly IRedisRepository _redisRepository;
        private readonly IRabbitMqPublisher _rabbitMqPublisher;
    private readonly ILogger<ModelRepository> _logger;
    private readonly IServiceProvider _serviceProvider;

        public ModelRepository(
            IDocumentTypeResolver typeResolver,
            IMongoRepository mongoRepository,
            IRedisRepository redisRepository,
            IRabbitMqPublisher rabbitMqPublisher,
            ILogger<ModelRepository> logger,
            IServiceProvider serviceProvider)
        {
            _typeResolver = typeResolver;
            _mongoRepository = mongoRepository;
            _redisRepository = redisRepository;
            _rabbitMqPublisher = rabbitMqPublisher;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        private IModelRepositoryHandler<TEntity> GetHandler<TEntity>() where TEntity : class, IDomainModel
        {
            var (documentType, _) = _typeResolver.GetMapping<TEntity>();
            _logger.Debug($"Creating handler for {typeof(TEntity).Name} -> {documentType.Name}");
            var handlerType = typeof(ModelRepositoryHandler<,>).MakeGenericType(typeof(TEntity), documentType);

            var handler = ActivatorUtilities.CreateInstance(_serviceProvider, handlerType);

            if (handler is null)
            {
                _logger.Error($"Could not create instance of handler for {typeof(TEntity).Name}");
                throw new InvalidOperationException($"Could not create instance of handler for {typeof(TEntity).Name}");
            }

            return (IModelRepositoryHandler<TEntity>)handler;
        }

        public Task UpsertAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class, IDomainModel
        {
            return GetHandler<TEntity>().UpsertAsync(entity, cancellationToken);
        }

        public Task<TEntity?> GetByIdAsync<TEntity>(object id, CancellationToken cancellationToken = default) where TEntity : class, IDomainModel
        {
            return GetHandler<TEntity>().GetByIdAsync(id, cancellationToken);
        }

        public Task<List<TEntity>> GetAllAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class, IDomainModel
        {
            return GetHandler<TEntity>().GetAllAsync(cancellationToken);
        }

        public Task<List<TEntity>> GetBatchAsync<TEntity>(int skip, int limit, CancellationToken cancellationToken = default) where TEntity : class, IDomainModel
        {
            return GetHandler<TEntity>().GetBatchAsync(skip, limit, cancellationToken);
        }

        public Task<long> CountAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class, IDomainModel
        {
            return GetHandler<TEntity>().CountAsync(cancellationToken);
        }

        public Task<bool> DeleteAsync<TEntity>(object id, CancellationToken cancellationToken = default) where TEntity : class, IDomainModel
        {
            return GetHandler<TEntity>().DeleteAsync(id, cancellationToken);
        }
    }
}
