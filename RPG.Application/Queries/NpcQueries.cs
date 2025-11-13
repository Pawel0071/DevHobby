// filepath: /Volumes/Data/Repositories/DevHobby/RPG.Application/Queries/NpcQueries.cs
using System.Text.Json;
using RPG.Application.Interfaces;
using RPG.Domain.Enums;
using RPG.Domain.Models.Npcs;
using RPG.Infrastructure.Interfaces;

namespace RPG.Application.Queries;

public sealed record GetNpcQuery(Guid NpcId) : IQuery<NpcReadDto>;
public sealed record GetNpcsQuery() : IQuery<IReadOnlyList<NpcReadDto>>;
public sealed record GetNpcsByIdsQuery(IReadOnlyCollection<Guid> NpcIds) : IQuery<IReadOnlyList<NpcReadDto>>;

public sealed class NpcReadDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int Level { get; init; }
    public int CurrentHealth { get; init; }
    public int MaxHealth { get; init; }
    public IReadOnlyDictionary<string, int> BaseStats { get; init; } = new Dictionary<string, int>();
    public IReadOnlyDictionary<string, int> ModifiedStats { get; init; } = new Dictionary<string, int>();
    public LocationReadDto SpawnLocation { get; init; } = new();
    public LocationReadDto CurrentLocation { get; init; } = new();
    public bool IsMoving { get; init; }
    public bool IsRotating { get; init; }
    public Guid WorldId { get; init; }
    public IReadOnlyCollection<string> Tags { get; init; } = Array.Empty<string>();
    public IReadOnlyList<ComponentReadDto> Components { get; init; } = new List<ComponentReadDto>();
}

public sealed class GetNpcQueryHandler(IModelRepository repo) : IQueryHandler<GetNpcQuery, NpcReadDto>
{
    public async Task<NpcReadDto> HandleAsync(GetNpcQuery query, CancellationToken ct = default)
    {
        var npc = await repo.GetByIdAsync<Npc>(query.NpcId, ct) ?? throw new KeyNotFoundException("Npc not found");
        return NpcQueriesMapper.Map(npc);
    }
}

public sealed class GetNpcsQueryHandler(IModelRepository repo) : IQueryHandler<GetNpcsQuery, IReadOnlyList<NpcReadDto>>
{
    public async Task<IReadOnlyList<NpcReadDto>> HandleAsync(GetNpcsQuery query, CancellationToken ct = default)
    {
        var all = await repo.GetAllAsync<Npc>(ct);
        return all.Select(NpcQueriesMapper.Map).ToList();
    }
}

public sealed class GetNpcsByIdsQueryHandler(IModelRepository repo) : IQueryHandler<GetNpcsByIdsQuery, IReadOnlyList<NpcReadDto>>
{
    public async Task<IReadOnlyList<NpcReadDto>> HandleAsync(GetNpcsByIdsQuery query, CancellationToken ct = default)
    {
        var list = new List<NpcReadDto>(query.NpcIds.Count);
        foreach (var id in query.NpcIds)
        {
            var npc = await repo.GetByIdAsync<Npc>(id, ct);
            if (npc != null) list.Add(NpcQueriesMapper.Map(npc));
        }
        return list;
    }
}

internal static class NpcQueriesMapper
{
    public static NpcReadDto Map(Npc npc)
    {
        var baseStats = npc.BaseStats.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value);
        var modifiedStats = npc.ModifiedStats.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value);
        var spawn = LocationReadDto.FromDomain(npc.SpawnLocation);
        var current = LocationReadDto.FromDomain(npc.CurrentLocation);
        var components = npc.Components
            .Select(c => new ComponentReadDto(c.GetType().Name, JsonSerializer.Serialize(c, c.GetType())))
            .ToList();
        return new NpcReadDto
        {
            Id = npc.Id,
            Name = npc.Name,
            DisplayName = npc.DisplayName,
            Description = npc.Description,
            Level = npc.Level,
            CurrentHealth = npc.CurrentHealth,
            MaxHealth = npc.MaxHealth,
            BaseStats = baseStats,
            ModifiedStats = modifiedStats,
            SpawnLocation = spawn,
            CurrentLocation = current,
            IsMoving = npc.IsMoving,
            IsRotating = npc.IsRotating,
            WorldId = npc.WorldId,
            Tags = npc.Tags.ToList(),
            Components = components
        };
    }
}
