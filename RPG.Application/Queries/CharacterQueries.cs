using RPG.Application.Interfaces;
using RPG.Domain.Models;
using RPG.Infrastructure.Interfaces;

namespace RPG.Application.Queries;

public sealed record GetCharacterQuery(Guid CharacterId) : IQuery<CharacterReadDto>;

public sealed class CharacterReadDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Level { get; init; }
    public long Experience { get; init; }
    public int CurrentHealth { get; init; }
    public int MaxHealth { get; init; }
    public float X { get; init; }
    public float Y { get; init; }
    public float Z { get; init; }
    public float Rotation { get; init; }
}

public sealed class GetCharacterQueryHandler : IQueryHandler<GetCharacterQuery, CharacterReadDto>
{
    private readonly IModelRepository _repo;
    public GetCharacterQueryHandler(IModelRepository repo) => _repo = repo;

    public async Task<CharacterReadDto> HandleAsync(GetCharacterQuery query, CancellationToken ct = default)
    {
        var character = await _repo.GetByIdAsync<Character>(query.CharacterId, ct) ?? throw new KeyNotFoundException("Character not found");
        return new CharacterReadDto
        {
            Id = character.Id,
            Name = character.Name,
            Level = character.Level,
            Experience = character.Experience,
            CurrentHealth = character.CurrentHealth,
            MaxHealth = character.MaxHealth,
            X = character.CurrentLocation?.Position.X ?? 0f,
            Y = character.CurrentLocation?.Position.Y ?? 0f,
            Z = character.CurrentLocation?.Position.Z ?? 0f,
            Rotation = character.CurrentLocation?.Rotation ?? 0f
        };
    }
}
