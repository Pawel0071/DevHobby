using RPG.GameServer.QueryProtos;
using RPG.Infrastructure.Interfaces;
using RPG.Domain.Enums;
using RPG.Domain.Models.Npcs;
using DomainNpc = RPG.Domain.Models.Npcs.Npc;
using DomainSkill = RPG.Domain.Models.Skills.Skill;
using ProtoNpc = RPG.GameServer.QueryProtos.Npc;
using ProtoNpcSkill = RPG.GameServer.QueryProtos.NpcSkillEntry;

namespace RPG.GameServer.Mappers;

/// <summary>
/// Mapper for Npc domain model to proto message
/// </summary>
public class NpcProtoMapper : IProtoMapper<DomainNpc, ProtoNpc>
{
    private readonly Infrastructure.Interfaces.ILogger<NpcProtoMapper> _logger;
    private readonly LocationProtoMapper _locationMapper;
    private readonly RPG.Infrastructure.Interfaces.IModelMapper<RPG.Domain.Models.Npcs.Npc, RPG.Infrastructure.Models.NpcDocument> _npcModelMapper;

    public NpcProtoMapper(
        Infrastructure.Interfaces.ILogger<NpcProtoMapper> logger,
        LocationProtoMapper locationMapper,
        RPG.Infrastructure.Interfaces.IModelMapper<RPG.Domain.Models.Npcs.Npc, RPG.Infrastructure.Models.NpcDocument> npcModelMapper)
    {
        _logger = logger;
        _locationMapper = locationMapper;
        _npcModelMapper = npcModelMapper;
    }

    public ProtoNpc ToProto(DomainNpc domain)
    {
        _logger.Debug($"Converting Npc to proto. Id={domain.Id}, Name={domain.Name}");

        var proto = new ProtoNpc
        {
            Id = domain.Id.ToString(),
            Name = domain.Name,
            DisplayName = domain.DisplayName,
            Description = domain.Description,
            Level = domain.Level,
            IsMoving = domain.IsMoving,
            IsRotating = domain.IsRotating,
            CurrentHealth = domain.CurrentHealth,
            MaxHealth = domain.MaxHealth,
            MapName = domain.CurrentLocation?.MapName ?? string.Empty
        };

        if (domain.CurrentLocation is not null)
        {
            proto.Location = _locationMapper.ToProto(domain.CurrentLocation);
        }

        proto.Tags.AddRange(domain.Tags);

        foreach (var kvp in MapStats(domain.BaseStats))
        {
            proto.BaseStats[kvp.Key] = kvp.Value;
        }

        foreach (var kvp in MapStats(domain.ModifiedStats))
        {
            proto.ModifiedStats[kvp.Key] = kvp.Value;
        }

        proto.Skills.AddRange(MapSkills(domain.Skills, domain.ActiveSkills));
        proto.Components.AddRange(MapComponents(domain.Components));

        _logger.Debug($"Npc proto created. Id={proto.Id}");
        return proto;
    }

    public DomainNpc ToDomain(ProtoNpc proto)
    {
        _logger.Debug($"Converting Npc proto to domain. Id={proto.Id}, Name={proto.Name}");

        var id = Guid.TryParse(proto.Id, out var parsed) ? parsed : Guid.NewGuid();
        var worldId = Guid.NewGuid();
        var location = proto.Location is not null
            ? _locationMapper.ToDomain(proto.Location)
            : RPG.Domain.Models.Location.Create(0, 0, 0, worldId);

        var npc = DomainNpc.Create(proto.Name, proto.DisplayName, location, worldId);

        // Do not override Id (no reflection) – keep factory-generated identity
        npc.Description = proto.Description;
        npc.Level = proto.Level;
        npc.CurrentHealth = proto.CurrentHealth;
        npc.MaxHealth = proto.MaxHealth;
        npc.SetMovementState(proto.IsMoving);
        npc.SetRotationState(proto.Location?.Rotation?.IsRotating ?? false);

        foreach (var tag in proto.Tags)
        {
            npc.Tags.Add(tag);
        }

        if (proto.BaseStats?.Count > 0)
        {
            var baseStats = npc.GetBaseStatsContainer();
            foreach (var kvp in proto.BaseStats)
            {
                if (Enum.TryParse<StatsProperty>(kvp.Key, true, out var stat))
                {
                    baseStats.Stats[stat] = kvp.Value;
                }
            }
        }

        if (proto.ModifiedStats?.Count > 0)
        {
            var modified = npc.GetModifiedStatsContainer();
            foreach (var kvp in proto.ModifiedStats)
            {
                if (Enum.TryParse<StatsProperty>(kvp.Key, true, out var stat))
                {
                    modified.Stats[stat] = kvp.Value;
                }
            }
        }

        if (proto.Skills?.Count > 0)
        {
            foreach (var skillProto in proto.Skills)
            {
                var skill = DomainSkill.Create(skillProto.Name, skillProto.Description);
                if (skillProto.Tags is { Count: > 0 })
                {
                    foreach (var t in skillProto.Tags)
                    {
                        if (!string.IsNullOrWhiteSpace(t)) skill.Tags.Add(t);
                    }
                }

                // Skills listed are considered learnt
                npc.Skills[skill] = SkillAvailability.Learnt;
                if (skillProto.IsActive)
                {
                    npc.ActiveSkills[skill] = DateTime.UtcNow;
                }
            }
        }

        // Components are serialized JSON - leverage infrastructure mapper for consistency
        npc.Components.Clear();
        foreach (var component in proto.Components)
        {
            if (string.IsNullOrWhiteSpace(component.Type) || string.IsNullOrWhiteSpace(component.DataJson))
            {
                continue;
            }

            var documentComponent = new RPG.Infrastructure.Models.ComponentData
            {
                Type = component.Type,
                Data = component.DataJson
            };

            // Use mapper's deserialization directly (no reflection)
            var deserialized = (_npcModelMapper as RPG.Infrastructure.Mappers.NpcModelMapper)?.DeserializeComponent(documentComponent)
                               ?? (documentComponent != null ? DeserializeFallback(documentComponent) : null);

            if (deserialized != null)
            {
                npc.Components.Add(deserialized);
            }
        }

        _logger.Debug($"Npc domain created. Id={npc.Id}");
        return npc;
    }

    private static IDictionary<string, int> MapStats(IDictionary<StatsProperty, int> stats)
    {
        var dict = new Dictionary<string, int>();
        foreach (var kvp in stats ?? Enumerable.Empty<KeyValuePair<StatsProperty, int>>())
        {
            dict[kvp.Key.ToString()] = kvp.Value;
        }
        return dict;
    }

    private static IEnumerable<ProtoNpcSkill> MapSkills(IDictionary<DomainSkill, SkillAvailability> skills, IDictionary<DomainSkill, DateTime> activeSkills)
    {
        foreach (var kvp in skills ?? Enumerable.Empty<KeyValuePair<DomainSkill, SkillAvailability>>())
        {
            yield return new ProtoNpcSkill
            {
                Id = kvp.Key.Id.ToString(),
                Name = kvp.Key.Name,
                Description = kvp.Key.Description,
                Tags = { kvp.Key.Tags },
                IsActive = activeSkills != null && activeSkills.ContainsKey(kvp.Key)
            };
        }
    }

    private static IEnumerable<Component> MapComponents(IEnumerable<INpcComponent> components)
    {
        foreach (var component in components ?? Enumerable.Empty<INpcComponent>())
        {
            yield return new Component
            {
                Type = component.GetType().Name,
                DataJson = System.Text.Json.JsonSerializer.Serialize(component, component.GetType())
            };
        }
    }

    private INpcComponent? DeserializeFallback(RPG.Infrastructure.Models.ComponentData documentComponent)
    {
        // Best-effort fallback to avoid tight coupling; if concrete mapper isn't available, try basic JSON deserialization
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<RPG.Domain.Models.Npcs.NpcComponents.LootableComponent>(documentComponent.Data);
        }
        catch
        {
            return null;
        }
    }
}
