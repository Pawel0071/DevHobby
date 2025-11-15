using Microsoft.Extensions.DependencyInjection;
using RPG.Application.Dispatchers;
using RPG.Application.Interfaces;

namespace RPG.Application.Commands.Handlers;

public static class CommandHandlersRegistrationExtensions
{
    // Rejestruje wszystkie implementacje ICommandHandler dla głównego CommandHandlera.
    public static IServiceCollection AddCommandHandlers(this IServiceCollection services)
    {
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
        services.AddScoped<ICommandHandler<UnlearnSkillCommand>, CommandHandler>();
        services.AddScoped<ICommandHandler<DieCommand>, CommandHandler>();

        return services;
    }
}
