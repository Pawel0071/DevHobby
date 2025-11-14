using RPG.GameServer.QueryProtos;
using DomainSkill = RPG.Domain.Models.Skills.Skill;

namespace RPG.GameServer.Mappers;

/// <summary>
/// Mapper for Skill domain model to proto message
/// </summary>
public class SkillProtoMapper : IProtoMapper<DomainSkill, Skill>
{
    private readonly RPG.Infrastructure.Interfaces.ILogger<SkillProtoMapper> _logger;

    public SkillProtoMapper(RPG.Infrastructure.Interfaces.ILogger<SkillProtoMapper> logger)
    {
        _logger = logger;
    }

    public Skill ToProto(DomainSkill domain)
    {
        _logger.Debug($"Converting Skill to proto. Id={domain.Id}, Name={domain.Name}");

        var proto = new Skill
        {
            Id = domain.Id.ToString(),
            Name = domain.Name,
            Description = domain.Description ?? string.Empty
        };

        proto.Tags.AddRange(domain.Tags);

        _logger.Debug($"Skill proto created. Id={proto.Id}");
        return proto;
    }

    public DomainSkill ToDomain(Skill proto)
    {
        _logger.Debug($"Converting Skill proto to domain. Id={proto.Id}, Name={proto.Name}");

        var id = Guid.TryParse(proto.Id, out var parsed) ? parsed : Guid.NewGuid();
        var skill = DomainSkill.Create(proto.Name, proto.Description);

        // Override Id if provided
        typeof(DomainSkill).GetProperty(nameof(DomainSkill.Id))?.SetValue(skill, id);

        foreach (var tag in proto.Tags)
        {
            skill.Tags.Add(tag);
        }

        _logger.Debug($"Skill domain created. Id={skill.Id}");
        return skill;
    }
}

