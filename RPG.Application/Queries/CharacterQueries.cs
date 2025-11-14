using RPG.Application.Interfaces;
using RPG.Domain.Models;
using RPG.Infrastructure.Interfaces;

namespace RPG.Application.Queries;

public sealed record GetCharacterQuery(Guid CharacterId) : IQuery<Character>;

public sealed class GetCharacterQueryHandler : IQueryHandler<GetCharacterQuery, Character>
{
    private readonly IModelRepository _repo;
    public GetCharacterQueryHandler(IModelRepository repo) => _repo = repo;

    public async Task<Character> HandleAsync(GetCharacterQuery query, CancellationToken ct = default)
        => await _repo.GetByIdAsync<Character>(query.CharacterId, ct) ?? throw new KeyNotFoundException("Character not found");
}
