using System.Threading;

namespace RPG.PersistenceService.Service;

public interface IRabbitMqToMongoService
{
    Task StartListeningAsync(CancellationToken cancellationToken = default);
    Task StopListeningAsync(CancellationToken cancellationToken = default);
}
