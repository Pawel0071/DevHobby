using Microsoft.Extensions.DependencyInjection;

namespace RPG.GameServer.Mappers;

/// <summary>
/// Extension methods for registering proto mappers in DI container
/// </summary>
public static class ProtoMappersRegistrationExtensions
{
    /// <summary>
    /// Registers all proto mapper classes
    /// </summary>
    public static IServiceCollection AddProtoMappers(this IServiceCollection services)
    {
        services.AddSingleton<LocationProtoMapper>();
        services.AddSingleton<ItemProtoMapper>();
        services.AddSingleton<SkillProtoMapper>();
        services.AddSingleton<NpcProtoMapper>();
        services.AddSingleton<MapObjectProtoMapper>();
        services.AddSingleton<QuestProtoMapper>();
        services.AddSingleton<CharacterProtoMapper>();

        return services;
    }
}

