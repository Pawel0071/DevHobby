using RPG.Domain.Common;
using RPG.Infrastructure.Documents;
using RPG.Infrastructure.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RPG.Infrastructure.Repositories.Orchestrators
{
    public class DocumentRepositoryHandler<TEntity, TDocument> : IDocumentRepositoryHandler<TEntity>
        where TEntity : class, IDomainEntity
        where TDocument : class, IMongoDocument
    {
        private readonly IMongoDocumentRepository _mongoRepository;
        private readonly IRedisDocumentRepository _redisRepository;
        private readonly IRabbitMqPublisher _rabbitMqPublisher;
        private readonly IDocumentMapper<TEntity, TDocument> _mapper;
        private readonly ILogger<DocumentRepositoryHandler<TEntity, TDocument>> _logger;

        public DocumentRepositoryHandler(
            IMongoDocumentRepository mongoRepository,
            IRedisDocumentRepository redisRepository,
            IRabbitMqPublisher rabbitMqPublisher,
            IDocumentMapper<TEntity, TDocument> mapper,
            ILogger<DocumentRepositoryHandler<TEntity, TDocument>> logger)
        {
            _mongoRepository = mongoRepository;
            _redisRepository = redisRepository;
            _rabbitMqPublisher = rabbitMqPublisher;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task UpsertAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            try
            {
                var document = _mapper.ToDocument(entity);
                await _redisRepository.UpsertAsync(document, cancellationToken);
                _logger.Debug($"Upserted document {document.Id} to Redis for entity {typeof(TEntity).Name}.");

                var routingKey = $"{typeof(TEntity).Name.ToLower()}.upserted";
                await _rabbitMqPublisher.PublishAsync(routingKey, document);
                _logger.Info($"Published 'upserted' event for entity {typeof(TEntity).Name} with Id {entity.Id}.");
            }
            catch (Exception ex)
            {
                _logger.Error($"An error occurred during UpsertAsync for entity {typeof(TEntity).Name} with Id {entity.Id}.", ex);
                throw;
            }
        }

        public async Task<TEntity?> GetByIdAsync(object id, CancellationToken cancellationToken = default)
        {
            try
            {
                var cachedDocument = await _redisRepository.GetByIdAsync<TDocument>(id, cancellationToken);
                if (cachedDocument != null)
                {
                    _logger.Debug($"Cache hit for {typeof(TEntity).Name} with Id {id}.");
                    return _mapper.ToDomain(cachedDocument);
                }

                _logger.Debug($"Cache miss for {typeof(TEntity).Name} with Id {id}. Fetching from MongoDB.");
                var document = await _mongoRepository.GetByIdAsync<TDocument>(id, cancellationToken);
                if (document == null)
                {
                    _logger.Debug($"Entity {typeof(TEntity).Name} with Id {id} not found in MongoDB.");
                    return null;
                }

                var entity = _mapper.ToDomain(document);
                await _redisRepository.UpsertAsync(document, cancellationToken);
                _logger.Debug($"Populated cache for {typeof(TEntity).Name} with Id {id}.");
                return entity;
            }
            catch (Exception ex)
            {
                _logger.Error($"An error occurred during GetByIdAsync for entity {typeof(TEntity).Name} with Id {id}.", ex);
                throw;
            }
        }

        public async Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Debug($"Fetching all documents for {typeof(TEntity).Name} from MongoDB.");
                var documents = await _mongoRepository.GetAllAsync<TDocument>(cancellationToken);
                return documents.ConvertAll(doc => _mapper.ToDomain(doc));
            }
            catch (Exception ex)
            {
                _logger.Error($"An error occurred during GetAllAsync for entity {typeof(TEntity).Name}.", ex);
                throw;
            }
        }

        public async Task<List<TEntity>> GetBatchAsync(int skip, int limit, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Debug($"Fetching batch (skip: {skip}, limit: {limit}) for {typeof(TEntity).Name} from MongoDB.");
                var documents = await _mongoRepository.GetBatchAsync<TDocument>(skip, limit, cancellationToken);
                return documents.ConvertAll(doc => _mapper.ToDomain(doc));
            }
            catch (Exception ex)
            {
                _logger.Error($"An error occurred during GetBatchAsync for entity {typeof(TEntity).Name}.", ex);
                throw;
            }
        }

        public async Task<long> CountAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Debug($"Counting documents for {typeof(TEntity).Name} in MongoDB.");
                return await _mongoRepository.CountAsync<TDocument>(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Error($"An error occurred during CountAsync for entity {typeof(TEntity).Name}.", ex);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(object id, CancellationToken cancellationToken = default)
        {
            try
            {
                var document = await _redisRepository.GetByIdAsync<TDocument>(id, cancellationToken) 
                    ?? await _mongoRepository.GetByIdAsync<TDocument>(id, cancellationToken);

                if (document == null)
                {
                    _logger.Debug($"Entity {typeof(TEntity).Name} with Id {id} not found for deletion.");
                    return false;
                }

                await _redisRepository.DeleteAsync<TDocument>(id, cancellationToken);
                _logger.Debug($"Deleted document {id} from Redis for entity {typeof(TEntity).Name}.");
                
                var routingKey = $"{typeof(TEntity).Name.ToLower()}.deleted";
                await _rabbitMqPublisher.PublishAsync(routingKey, document);
                _logger.Info($"Published 'deleted' event for entity {typeof(TEntity).Name} with Id {id}.");

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"An error occurred during DeleteAsync for entity {typeof(TEntity).Name} with Id {id}.", ex);
                throw;
            }
        }
    }
}
