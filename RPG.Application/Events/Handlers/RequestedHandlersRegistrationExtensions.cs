using Microsoft.Extensions.DependencyInjection;
using RPG.Application.Events.Handlers;
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
        services.AddScoped<IRequestedEventHandler, MovementRequestedHandler>();
        services.AddScoped<IRequestedEventHandler, EquipmentInventoryRequestedHandler>();
        services.AddScoped<IRequestedEventHandler, CharacterCreationRequestedHandler>();
        return services;
    }
}

