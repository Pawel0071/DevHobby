using MongoDB.Driver;
using RabbitMQ.Client;

namespace RPG.PersistanceService.Infrastructure;

public interface IRabbitMqToMongoService
{
    void StartListening();
}