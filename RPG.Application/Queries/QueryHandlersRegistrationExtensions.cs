using Microsoft.Extensions.DependencyInjection;
using RPG.Application.Interfaces;
using RPG.Domain.Models;
using RPG.Domain.Models.Items;
using RPG.Domain.Models.MapObjects;
using RPG.Domain.Models.Npcs;
using RPG.Domain.Models.Quests;
using RPG.Domain.Models.Skills;

namespace RPG.Application.Queries;

public static class QueryHandlersRegistrationExtensions
{
    // Rejestruje wszystkie implementacje IQueryHandler używane w Application.
    public static IServiceCollection AddQueryHandlers(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<GetCharacterQuery, Character>, GetCharacterQueryHandler>();
        services.AddScoped<IQueryHandler<GetWorldStateQuery, WorldStateReadDto>, GetWorldStateQueryHandler>();
        services.AddScoped<IQueryHandler<GetItemQuery, Item>, GetItemQueryHandler>();
        services.AddScoped<IQueryHandler<GetItemsQuery, IReadOnlyList<Item>>, GetItemsQueryHandler>();
        services.AddScoped<IQueryHandler<GetItemsByIdsQuery, IReadOnlyList<Item>>, GetItemsByIdsQueryHandler>();

        services.AddScoped<IQueryHandler<GetSkillQuery, Skill>, GetSkillQueryHandler>();
        services.AddScoped<IQueryHandler<GetSkillsQuery, IReadOnlyList<Skill>>, GetSkillsQueryHandler>();
        services.AddScoped<IQueryHandler<GetSkillsByIdsQuery, IReadOnlyList<Skill>>, GetSkillsByIdsQueryHandler>();

        services.AddScoped<IQueryHandler<GetNpcQuery, Npc>, GetNpcQueryHandler>();
        services.AddScoped<IQueryHandler<GetNpcsQuery, IReadOnlyList<Npc>>, GetNpcsQueryHandler>();
        services.AddScoped<IQueryHandler<GetNpcsByIdsQuery, IReadOnlyList<Npc>>, GetNpcsByIdsQueryHandler>();

        services.AddScoped<IQueryHandler<GetMapObjectQuery, MapObject>, GetMapObjectQueryHandler>();
        services.AddScoped<IQueryHandler<GetMapObjectsQuery, IReadOnlyList<MapObject>>, GetMapObjectsQueryHandler>();
        services.AddScoped<IQueryHandler<GetMapObjectsByIdsQuery, IReadOnlyList<MapObject>>, GetMapObjectsByIdsQueryHandler>();

        services.AddScoped<IQueryHandler<GetQuestQuery, Quest>, GetQuestQueryHandler>();
        services.AddScoped<IQueryHandler<GetQuestsQuery, IReadOnlyList<Quest>>, GetQuestsQueryHandler>();
        services.AddScoped<IQueryHandler<GetQuestsByIdsQuery, IReadOnlyList<Quest>>, GetQuestsByIdsQueryHandler>();

        services.AddScoped<IQueryHandler<GetSessionQuery, GameSession>, GetSessionQueryHandler>();
        return services;
    }
}
