using RPG.Core.Domain.Entities;

namespace RPG.Core.Infrastructure.Services.StatsService;

public interface IStatModifierStrategy
{
    void Apply(Character character);
    void Initialize(Character character);
}