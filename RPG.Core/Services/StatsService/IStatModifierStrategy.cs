using RPG.Domain.Entities;

namespace RPG.Core.Services.StatsService;

public interface IStatModifierStrategy
{
    void Apply(Character character);
    void Initialize(Character character);
}
