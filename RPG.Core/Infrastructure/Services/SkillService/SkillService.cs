using RPG.Core.Domain.Entities;
using RPG.Core.Domain.Entities.Common;
using RPG.Core.Infrastructure.Services.Logger;
using RPG.Core.Infrastructure.Services.StatsService;

namespace RPG.Core.Interfaces;

public class SkillService : ISkillService
{
    private readonly ILogger<SkillService> _logger;
    
    public SkillService(ILogger<SkillService> logger)
    {
        _logger = logger;
    }

    public SkillResult AddSkillsAfterLevelUp(Character character)
    {
        throw new NotImplementedException();
    }

    public SkillResult AddSkillsAfterEquipItem(Character character, Item item)
    {
        throw new NotImplementedException();
    }

    public SkillResult RemoveSkillsAfterUnEquipItem(Character character, Item item)
    {
        throw new NotImplementedException();
    }

    public SkillResult UseSkill(Character character, Skill skill)
    {
        throw new NotImplementedException();
    }

    public SkillResult LearnSkill(Character character, Skill skill)
    {
        throw new NotImplementedException();
    }

    public SkillResult UnlearnSkill(Character character, Skill skill)
    {
        throw new NotImplementedException();
    }
}