using Microsoft.Extensions.Hosting;
using RPG.Abstractions.Interfaces;
using RPG.Application.Events;
using RPG.Application.Infrastructure;
using RPG.Core.Interfaces;
using RPG.Domain.Common;
using RPG.Domain.Enums;
using RPG.Domain.Models;
using RPG.Domain.Models.Items;
using RPG.Domain.Models.Items.ItemComponent;
using RPG.Infrastructure.Interfaces;

namespace RPG.Application.Hosted;

[Obsolete("Replaced by RequestedEventsHostedService + handler. Keeping class for reference; not registered in DI anymore.")]
public sealed class EquipmentInventoryRequestedHostedService : BackgroundService
{
    private readonly ILogger<EquipmentInventoryRequestedHostedService> _logger;

    public EquipmentInventoryRequestedHostedService(ILogger<EquipmentInventoryRequestedHostedService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.Info("EquipmentInventoryRequestedHostedService started (DEPRECATED, not used)");
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
        _logger.Info("EquipmentInventoryRequestedHostedService stopped");
    }
}
