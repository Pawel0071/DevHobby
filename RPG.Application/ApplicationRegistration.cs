using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RPG.Abstractions.Interfaces;
using RPG.Application.Broadcasters;
using RPG.Application.Commands;
using RPG.Application.Commands.Handlers;
using RPG.Application.Diagnostics;
using RPG.Application.Dispatchers;
using RPG.Application.Events.Handlers;
using RPG.Application.Interfaces;
using RPG.Application.Infrastructure;
using RPG.Application.Queries;
using RPG.Infrastructure.Interfaces;
using RPG.Application.Hosted;
using RPG.Application.Managers;

namespace RPG.Application;

public static class ApplicationRegistration
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration config)
    {
        // Init diagnostics service label
        var serviceName = config.GetValue<string>("OpenTelemetry:ServiceName") ?? "RPG";
        ApplicationDiagnostics.Init(serviceName);

        services.AddCommandHandlers();
        services.AddScoped<ICommandBus, CommandBus>();
        services.AddSingleton<IEventBroadcaster, LoggingEventBroadcaster>();
        // Simplified dispatcher chain
        services.AddSingleton<GameEventDispatcher>();
        services.AddSingleton<IGameEventDispatcher>(sp =>
            new BroadcastingEventDispatcher(
                sp.GetRequiredService<GameEventDispatcher>(),
                sp.GetRequiredService<IEventBroadcaster>(),
                sp.GetRequiredService<ILogger<BroadcastingEventDispatcher>>()));
        services.AddSingleton<IEventMessageQueue, EventMessageQueue>();
        services.AddSingleton<IEventIdProvider, DeterministicEventIdProvider>();
        services.AddSingleton<IEventSequenceStore, InMemoryEventSequenceStore>();

        // Requested events unified hosted service + handlers
        services.AddSingleton<IRequestEventQueue, RequestedEventQueue>();
        services.AddSingleton<IRequestedEventOrchestrator, RequestedEventOrchestrator>();
        services.AddSingleton<IRequestedEventInlineDispatcher, RequestedEventInlineDispatcher>();
        services.AddHostedService<RequestedEventsHostedService>();
        services.AddRequestedEventHandlers();

        services.AddQueryHandlers();
        services.AddScoped<IQueryBus, QueryBus>();
        services.AddScoped<ISessionManager, SessionManager>();
        services.AddSingleton<ICharacterStateBroadcaster, CharacterStateBroadcaster>();
        services.AddSingleton<IWorldStateBroadcaster, WorldStateBroadcaster>();
        services.AddSingleton<IGameStateBroadcaster, GameStateBroadcaster>();

        return services;
    }
}
