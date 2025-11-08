using RPG.Core.Common;
using RPG.Core.Interfaces;
using RPG.Domain.Entities;
using RPG.Domain.Entities.Items;
using RPG.Domain.Entities.Skills;
using RPG.Infrastructure.Interfaces;

namespace RPG.Core.Services.SkillService;

public class SkillService : ISkillService
{
    private readonly ILogger<SkillService> _logger;

    public SkillService(ILogger<SkillService> logger)
    {
        _logger = logger;
    }

    public ServiceResult<bool> AddSkillsAfterLevelUp(Character character)
    {
        throw new NotImplementedException();
    }

    public ServiceResult<bool> AddSkillsAfterEquipItem(Character character, Item item)
    {
        throw new NotImplementedException();
    }

    public ServiceResult<bool> RemoveSkillsAfterUnEquipItem(Character character, Item item)
    {
        throw new NotImplementedException();
    }

    public ServiceResult<bool> UseSkill(Character character, Skill skill)
    {
        throw new NotImplementedException();
    }

    public ServiceResult<bool> LearnSkill(Character character, Skill skill)
    {
        throw new NotImplementedException();
    }

    public ServiceResult<bool> UnlearnSkill(Character character, Skill skill)
    {
        throw new NotImplementedException();
    }
}
