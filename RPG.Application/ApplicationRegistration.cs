using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RPG.Application.Commands;
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

        return services;
    }
}
