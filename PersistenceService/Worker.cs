using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace PersistenceService
{
    public class Worker : BackgroundService
    {
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Worker logic goes here
            return Task.CompletedTask;
        }
    }
}