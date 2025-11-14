using RPG.Application.Interfaces;
using RPG.Domain.Models;
using RPG.Infrastructure.Interfaces;

namespace RPG.Application.Queries;

public sealed record GetSessionQuery(Guid SessionId) : IQuery<GameSession>;

public sealed class GetSessionQueryHandler(IModelRepository repo) : IQueryHandler<GetSessionQuery, GameSession>
{
    public async Task<GameSession> HandleAsync(GetSessionQuery query, CancellationToken ct = default)
    {
        return await repo.GetByIdAsync<GameSession>(query.SessionId, ct)
               ?? throw new KeyNotFoundException("Session not found");
    }
}
