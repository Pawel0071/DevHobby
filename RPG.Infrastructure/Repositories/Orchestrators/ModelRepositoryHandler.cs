using RPG.Domain.Common;
using RPG.Infrastructure.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RPG.Infrastructure.Models;

namespace RPG.Infrastructure.Repositories.Orchestrators
{
    public class ModelRepositoryHandler<TDomainModel, TPersistenceModel> : IModelRepositoryHandler<TDomainModel>
        where TDomainModel : class, IDomainModel
        where TPersistenceModel : class, IPersistenceModel
    {
        private readonly IMongoRepository _mongoRepository;
        private readonly IRedisRepository _redisRepository;
        private readonly IRabbitMqPublisher _rabbitMqPublisher;
        private readonly IModelMapper<TDomainModel, TPersistenceModel> _mapper;
        private readonly ILogger<ModelRepositoryHandler<TDomainModel, TPersistenceModel>> _logger;

        public ModelRepositoryHandler(
            IMongoRepository mongoRepository,
            IRedisRepository redisRepository,
            IRabbitMqPublisher rabbitMqPublisher,
            IModelMapper<TDomainModel, TPersistenceModel> mapper,
            ILogger<ModelRepositoryHandler<TDomainModel, TPersistenceModel>> logger)
        {
            _mongoRepository = mongoRepository;
            _redisRepository = redisRepository;
            _rabbitMqPublisher = rabbitMqPublisher;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task UpsertAsync(TDomainModel domainModel, CancellationToken cancellationToken = default)
        {
            try
            {
                var persistenceModel = _mapper.ToPersistence(domainModel);
                await _redisRepository.UpsertAsync(persistenceModel, cancellationToken);
                _logger.Debug($"Upserted document {persistenceModel.Id} to Redis for domainModel {typeof(TDomainModel).Name}.");

                var routingKey = $"{typeof(TDomainModel).Name.ToLower()}.upserted";
                await _rabbitMqPublisher.PublishAsync(routingKey, persistenceModel);
                _logger.Info($"Published 'upserted' event for domainModel {typeof(TDomainModel).Name} with Id {domainModel.Id}.");
            }
            catch (Exception ex)
            {
                _logger.Error($"An error occurred during UpsertAsync for domainModel {typeof(TDomainModel).Name} with Id {domainModel.Id}.", ex);
                throw;
            }
        }

        public async Task<TDomainModel?> GetByIdAsync(object id, CancellationToken cancellationToken = default)
        {
            try
            {
                var cachedDocument = await _redisRepository.GetByIdAsync<TPersistenceModel>(id, cancellationToken);
                if (cachedDocument != null)
                {
                    _logger.Debug($"Cache hit for {typeof(TDomainModel).Name} with Id {id}.");
                    return _mapper.ToDomain(cachedDocument);
                }

                _logger.Debug($"Cache miss for {typeof(TDomainModel).Name} with Id {id}. Fetching from MongoDB.");
                var document = await _mongoRepository.GetByIdAsync<TPersistenceModel>(id, cancellationToken);
                if (document == null)
                {
                    _logger.Debug($"Entity {typeof(TDomainModel).Name} with Id {id} not found in MongoDB.");
                    return null;
                }

                var entity = _mapper.ToDomain(document);
                await _redisRepository.UpsertAsync(document, cancellationToken);
                _logger.Debug($"Populated cache for {typeof(TDomainModel).Name} with Id {id}.");
                return entity;
            }
            catch (Exception ex)
            {
                _logger.Error($"An error occurred during GetByIdAsync for entity {typeof(TDomainModel).Name} with Id {id}.", ex);
                throw;
            }
        }

        public async Task<List<TDomainModel>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Debug($"Fetching all documents for {typeof(TDomainModel).Name} from MongoDB.");
                var documents = await _mongoRepository.GetAllAsync<TPersistenceModel>(cancellationToken);
                return documents.ConvertAll(doc => _mapper.ToDomain(doc));
            }
            catch (Exception ex)
            {
                _logger.Error($"An error occurred during GetAllAsync for entity {typeof(TDomainModel).Name}.", ex);
                throw;
            }
        }

        public async Task<List<TDomainModel>> GetBatchAsync(int skip, int limit, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Debug($"Fetching batch (skip: {skip}, limit: {limit}) for {typeof(TDomainModel).Name} from MongoDB.");
                var documents = await _mongoRepository.GetBatchAsync<TPersistenceModel>(skip, limit, cancellationToken);
                return documents.ConvertAll(doc => _mapper.ToDomain(doc));
            }
            catch (Exception ex)
            {
                _logger.Error($"An error occurred during GetBatchAsync for entity {typeof(TDomainModel).Name}.", ex);
                throw;
            }
        }

        public async Task<long> CountAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Debug($"Counting documents for {typeof(TDomainModel).Name} in MongoDB.");
                return await _mongoRepository.CountAsync<TPersistenceModel>(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Error($"An error occurred during CountAsync for entity {typeof(TDomainModel).Name}.", ex);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(object id, CancellationToken cancellationToken = default)
        {
            try
            {
                var document = await _redisRepository.GetByIdAsync<TPersistenceModel>(id, cancellationToken)
                    ?? await _mongoRepository.GetByIdAsync<TPersistenceModel>(id, cancellationToken);

                if (document == null)
                {
                    _logger.Debug($"Entity {typeof(TDomainModel).Name} with Id {id} not found for deletion.");
                    return false;
                }

                await _redisRepository.DeleteAsync<TPersistenceModel>(id, cancellationToken);
                _logger.Debug($"Deleted document {id} from Redis for entity {typeof(TDomainModel).Name}.");

                var routingKey = $"{typeof(TDomainModel).Name.ToLower()}.deleted";
                await _rabbitMqPublisher.PublishAsync(routingKey, document);
                _logger.Info($"Published 'deleted' event for entity {typeof(TDomainModel).Name} with Id {id}.");

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"An error occurred during DeleteAsync for entity {typeof(TDomainModel).Name} with Id {id}.", ex);
                throw;
            }
        }
    }
}
