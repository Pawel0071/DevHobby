using RPG.Application.Interfaces;
using RPG.Domain.Models;
using RPG.Infrastructure.Interfaces;

namespace RPG.Application.Queries;

public sealed record GetWorldStateQuery(Guid WorldId) : IQuery<WorldStateReadDto>;

public sealed class WorldStateReadDto
{
    public Guid Id { get; init; }
    public Guid WorldId { get; init; }
    public string WorldName { get; init; } = string.Empty;
    public int CharactersCount { get; init; }
    public int NpcsCount { get; init; }
    public int MapObjectsCount { get; init; }
    public DateTime LastUpdated { get; init; }
}

public sealed class GetWorldStateQueryHandler : IQueryHandler<GetWorldStateQuery, WorldStateReadDto>
{
    private readonly IModelRepository _repo;
    public GetWorldStateQueryHandler(IModelRepository repo) => _repo = repo;

    public async Task<WorldStateReadDto> HandleAsync(GetWorldStateQuery query, CancellationToken ct = default)
    {
        var ws = await _repo.GetByIdAsync<WorldState>(query.WorldId, ct) ?? throw new KeyNotFoundException("WorldState not found");
        return new WorldStateReadDto
        {
            Id = ws.Id,
            WorldId = ws.WorldId,
            WorldName = ws.WorldName,
            CharactersCount = ws.Characters.Count,
            NpcsCount = ws.Npcs.Count,
            MapObjectsCount = ws.MapObjects.Count,
            LastUpdated = ws.LastUpdated
        };
    }
}
