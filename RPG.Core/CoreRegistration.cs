using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RPG.Core.Interfaces;
using RPG.Core.Services.EquipmentService;
using RPG.Core.Services.InventoryService;
using RPG.Core.Services.LevelService;
using RPG.Core.Services.SkillService;
using RPG.Core.Services.StatsService;

namespace RPG.Core;

public static class CoreRegistration
{
    public static IServiceCollection AddCore(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<IEquipmentService, EquipmentService>();
        services.AddSingleton<IInventoryService, InventoryService>();
        services.AddSingleton<ISkillService, SkillService>();
        services.AddSingleton<IStatsService, StatsService>();
        services.AddSingleton<ILevelingService, LevelingService>();
        
        return services;
    }
}