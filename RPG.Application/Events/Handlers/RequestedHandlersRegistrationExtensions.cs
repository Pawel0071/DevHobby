using Microsoft.Extensions.DependencyInjection;
using RPG.Application.Events.Handlers;
using RPG.Application.Events.Handlers.Requested;
using RPG.Application.Interfaces;

namespace RPG.Application.Events.Handlers;

public static class RequestedHandlersRegistrationExtensions
{
    /// <summary>
    /// Rejestruje wszystkie IRequestedEventHandler dla requested eventów.
    /// Trzymamy to w jednym miejscu, żeby ApplicationRegistration było czystsze.
    /// </summary>
    public static IServiceCollection AddRequestedEventHandlers(this IServiceCollection services)
    {
        // Character handlers
        services.AddScoped<IRequestedEventHandler, MovementRequestedHandler>();
        services.AddScoped<IRequestedEventHandler, EquipmentInventoryRequestedHandler>();
        services.AddScoped<IRequestedEventHandler, CharacterCreationRequestedHandler>();

        // NPC AI handlers
        services.AddScoped<IRequestedEventHandler, NpcMovementRequestedHandler>();
        services.AddScoped<IRequestedEventHandler, NpcIdleRequestedHandler>();
        services.AddScoped<IRequestedEventHandler, NpcReturnToSpawnRequestedHandler>();

        // Skill handlers
        services.AddScoped<IRequestedEventHandler, SkillUsageRequestedHandler>();
        services.AddScoped<IRequestedEventHandler, SkillLearnRequestedHandler>();
        services.AddScoped<IRequestedEventHandler, SkillLevelUpRequestedHandler>();
        services.AddScoped<IRequestedEventHandler, SkillUnlearnRequestedHandler>();

        // Progression handlers
        services.AddScoped<IRequestedEventHandler, ExperienceGainRequestedHandler>();
        services.AddScoped<IRequestedEventHandler, CharacterLevelUpRequestedHandler>();
        services.AddScoped<IRequestedEventHandler, CharacterDeathRequestedHandler>();

        // Combat handlers
        services.AddScoped<IRequestedEventHandler, CombatRequestedHandler>();

        // Quest handlers
        services.AddScoped<IRequestedEventHandler, QuestAcceptRequestedHandler>();
        services.AddScoped<IRequestedEventHandler, QuestCompleteRequestedHandler>();
        services.AddScoped<IRequestedEventHandler, QuestProgressUpdateRequestedHandler>();

        return services;
    }
}
