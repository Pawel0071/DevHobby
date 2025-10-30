using RPG.Core.Interfaces;
using RPG.Domain.Common;
using RPG.Domain.Entities;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.Logger;

namespace RPG.Core.Services.SkillService;

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