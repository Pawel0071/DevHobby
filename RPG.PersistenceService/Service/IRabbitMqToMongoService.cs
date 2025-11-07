namespace RPG.PersistenceService.Service;

public interface IRabbitMqToMongoService
{
    Task StartListeningAsync();
}