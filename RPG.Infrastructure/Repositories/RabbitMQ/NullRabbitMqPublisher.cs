using RPG.Infrastructure.Interfaces;
using System.Threading.Tasks;

namespace RPG.Infrastructure.Repositories.RabbitMQ
{
    public class NullRabbitMqPublisher : IRabbitMqPublisher
    {
        private readonly ILogger<NullRabbitMqPublisher>? _logger;

        public NullRabbitMqPublisher(ILogger<NullRabbitMqPublisher>? logger)
        {
            _logger = logger;
            _logger?.Info("NullRabbitMqPublisher initialized because RabbitMQ is not configured.");
        }

        public Task PublishAsync<T>(string topic, T message)
        {
            _logger?.Debug($"Skipping message publish to topic '{topic}' as RabbitMQ is not configured.");
            return Task.CompletedTask;
        }
    }
}
