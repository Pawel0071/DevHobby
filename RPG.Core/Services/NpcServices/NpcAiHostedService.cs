using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using RPG.Core.Interfaces.NpcServices;
using RPG.Infrastructure.Interfaces;

namespace RPG.Core.Services.NpcServices;

public sealed class NpcAiHostedService : BackgroundService
{
    private static readonly TimeSpan DefaultTickInterval = TimeSpan.FromMilliseconds(500);

    private readonly INpcAiService _npcAiService;
    private readonly ILogger<NpcAiHostedService> _logger;
    private readonly TimeSpan _tickInterval;

    public NpcAiHostedService(
        INpcAiService npcAiService,
        ILogger<NpcAiHostedService> logger)
    {
        _npcAiService = npcAiService;
        _logger = logger;
        _tickInterval = DefaultTickInterval;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.Info($"NPC AI hosted service started with tick interval {_tickInterval}.");

        try
        {
            await _npcAiService.TickAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            _logger.Error("Initial NPC AI tick failed.", ex);
        }

        using var timer = new PeriodicTimer(_tickInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                try
                {
                    await _npcAiService.TickAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Error( "NPC AI tick failed.",ex);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // graceful shutdown
        }

        _logger.Info("NPC AI hosted service is stopping.");
    }
}
