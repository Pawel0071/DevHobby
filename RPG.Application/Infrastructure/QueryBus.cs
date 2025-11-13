using Microsoft.Extensions.DependencyInjection;
using RPG.Application.Interfaces;

namespace RPG.Application.Infrastructure;

public class QueryBus(IServiceProvider serviceProvider) : IQueryBus
{
    public async Task<TResult> ExecuteAsync<TQuery, TResult>(TQuery query, CancellationToken ct = default) where TQuery : IQuery<TResult>
    {
        using var scope = serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IQueryHandler<TQuery, TResult>>();
        return await handler.HandleAsync(query, ct);
    }
}
