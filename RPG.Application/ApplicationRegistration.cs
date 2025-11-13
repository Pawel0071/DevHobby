using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RPG.Abstractions.Interfaces;
using RPG.Application.Commands;
using RPG.Application.Diagnostics;
using RPG.Application.Dispatchers;
using RPG.Application.Handlers;
using RPG.Application.Interfaces;
using RPG.Application.Infrastructure;
using RPG.Application.Queries;
using RPG.Infrastructure.Interfaces;
using RPG.Application.Hosted;
using RPG.Application.Handlers.Requested;

namespace RPG.Application;

public static class ApplicationRegistration
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration config)
    {
        // Init diagnostics service label
        var serviceName = config.GetValue<string>("OpenTelemetry:ServiceName") ?? "RPG";
        ApplicationDiagnostics.Init(serviceName);

        services.AddScoped<ICommandHandler<EquipItemCommand>, CommandHandler>();
        services.AddScoped<ICommandHandler<UnequipItemCommand>, CommandHandler>();
        services.AddScoped<ICommandHandler<PutItemToBankCommand>, CommandHandler>();
        services.AddScoped<ICommandHandler<GetItemFromBankCommand>, CommandHandler>();
        services.AddScoped<ICommandHandler<UseItemCommand>, CommandHandler>();
        services.AddScoped<ICommandHandler<DropItemCommand>, CommandHandler>();
        services.AddScoped<ICommandHandler<PickUpItemCommand>, CommandHandler>();
        services.AddScoped<ICommandHandler<GainExperienceCommand>, CommandHandler>();
        services.AddScoped<ICommandHandler<LevelUpCommand>, CommandHandler>();
        services.AddScoped<ICommandHandler<StartMovementCommand>, CommandHandler>();
        services.AddScoped<ICommandHandler<StopMovementCommand>, CommandHandler>();
        services.AddScoped<ICommandHandler<StartRotationCommand>, CommandHandler>();
        services.AddScoped<ICommandHandler<StopRotationCommand>, CommandHandler>();
        services.AddScoped<ICommandHandler<CreateCharacterCommand>, CommandHandler>();
        services.AddScoped<ICommandHandler<UseSkillCommand>, CommandHandler>();
        services.AddScoped<ICommandHandler<LearnSkillCommand>, CommandHandler>();
        services.AddScoped<ICommandHandler<LevelUpSkillCommand>, CommandHandler>();
        services.AddScoped<ICommandHandler<UnLearnSkillCommand>, CommandHandler>();
        services.AddScoped<ICommandHandler<LoginCharacterCommand>, CommandHandler>();
        services.AddScoped<ICommandHandler<LogoutCharacterCommand>, CommandHandler>();
        services.AddScoped<ICommandHandler<DieCommand>, CommandHandler>();
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
        services.AddSingleton<IRequestedEventInlineDispatcher, RequestedEventInlineDispatcher>();
        services.AddHostedService<RequestedEventsHostedService>();
        services.AddScoped<IRequestedEventHandler, MovementRequestedHandler>();
        services.AddScoped<IRequestedEventHandler, EquipmentInventoryRequestedHandler>();
        services.AddScoped<IRequestedEventHandler, CharacterCreationRequestedHandler>();

        // Queries
        services.AddScoped<IQueryHandler<GetCharacterQuery, CharacterReadDto>, GetCharacterQueryHandler>();
        services.AddScoped<IQueryHandler<GetWorldStateQuery, WorldStateReadDto>, GetWorldStateQueryHandler>();
        services.AddScoped<IQueryHandler<GetItemQuery, ItemReadDto>, GetItemQueryHandler>();
        services.AddScoped<IQueryHandler<GetItemsQuery, IReadOnlyList<ItemReadDto>>, GetItemsQueryHandler>();
        services.AddScoped<IQueryHandler<GetItemsByIdsQuery, IReadOnlyList<ItemReadDto>>, GetItemsByIdsQueryHandler>();
        services.AddScoped<IQueryHandler<GetSkillQuery, SkillReadDto>, GetSkillQueryHandler>();
        services.AddScoped<IQueryHandler<GetSkillsQuery, IReadOnlyList<SkillReadDto>>, GetSkillsQueryHandler>();
        services.AddScoped<IQueryHandler<GetSkillsByIdsQuery, IReadOnlyList<SkillReadDto>>, GetSkillsByIdsQueryHandler>();
        services.AddScoped<IQueryHandler<GetNpcQuery, NpcReadDto>, GetNpcQueryHandler>();
        services.AddScoped<IQueryHandler<GetNpcsQuery, IReadOnlyList<NpcReadDto>>, GetNpcsQueryHandler>();
        services.AddScoped<IQueryHandler<GetNpcsByIdsQuery, IReadOnlyList<NpcReadDto>>, GetNpcsByIdsQueryHandler>();
        services.AddScoped<IQueryHandler<GetMapObjectQuery, MapObjectReadDto>, GetMapObjectQueryHandler>();
        services.AddScoped<IQueryHandler<GetMapObjectsQuery, IReadOnlyList<MapObjectReadDto>>, GetMapObjectsQueryHandler>();
        services.AddScoped<IQueryHandler<GetMapObjectsByIdsQuery, IReadOnlyList<MapObjectReadDto>>, GetMapObjectsByIdsQueryHandler>();
        services.AddScoped<IQueryHandler<GetQuestQuery, QuestReadDto>, GetQuestQueryHandler>();
        services.AddScoped<IQueryHandler<GetQuestsQuery, IReadOnlyList<QuestReadDto>>, GetQuestsQueryHandler>();
        services.AddScoped<IQueryHandler<GetQuestsByIdsQuery, IReadOnlyList<QuestReadDto>>, GetQuestsByIdsQueryHandler>();
        services.AddSingleton<IQueryBus, QueryBus>();

        return services;
    }
}
