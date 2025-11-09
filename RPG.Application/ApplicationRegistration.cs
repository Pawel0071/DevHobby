using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RPG.Abstractions.Interfaces;
using RPG.Application.Commands;
using RPG.Application.Dispatchers;
using RPG.Application.Events;
using RPG.Application.Handlers;
using RPG.Application.Interfaces;

namespace RPG.Application;

public static class ApplicationRegistration
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<ICommandHandler<EquipItemCommand>, CharacterCommandHandler>();
        services.AddScoped<ICommandHandler<UnequipItemCommand>, CharacterCommandHandler>();
        services.AddScoped<ICommandHandler<PutItemToBankCommand>, CharacterCommandHandler>();
        services.AddScoped<ICommandHandler<GetItemFromBankCommand>, CharacterCommandHandler>();
        services.AddScoped<ICommandHandler<UseItemCommand>, CharacterCommandHandler>();
        services.AddScoped<ICommandHandler<DropItemCommand>, CharacterCommandHandler>();
        services.AddScoped<ICommandHandler<PickUpItemCommand>, CharacterCommandHandler>();
        services.AddScoped<ICommandHandler<GainExperienceCommand>, CharacterCommandHandler>();
        services.AddScoped<ICommandHandler<LevelUpCommand>, CharacterCommandHandler>();
        services.AddScoped<ICommandHandler<StartMovementCommand>, CharacterCommandHandler>();
        services.AddScoped<ICommandHandler<StopMovementCommand>, CharacterCommandHandler>();
        services.AddScoped<ICommandHandler<StartRotationCommand>, CharacterCommandHandler>();
        services.AddScoped<ICommandHandler<StopRotationCommand>, CharacterCommandHandler>();
        services.AddSingleton<IGameEventDispatcher, GameEventDispatcher>();
        services.AddSingleton<INpcCombatEventDispatcher, NpcCombatEventDispatcher>();

        return services;
    }
}
