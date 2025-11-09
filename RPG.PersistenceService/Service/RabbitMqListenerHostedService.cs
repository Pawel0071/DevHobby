using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace RPG.PersistenceService.Service;

public class RabbitMqListenerHostedService : IHostedService
{
    private readonly IRabbitMqToMongoService _listener;

    public RabbitMqListenerHostedService(IRabbitMqToMongoService listener)
    {
        _listener = listener;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return _listener.StartListeningAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return _listener.StopListeningAsync(cancellationToken);
    }
}
