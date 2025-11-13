namespace RPG.Application.Interfaces;

public interface IQuery<TResult> { }

public interface IQueryHandler<TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken ct = default);
}

public interface IQueryBus
{
    Task<TResult> ExecuteAsync<TQuery, TResult>(TQuery query, CancellationToken ct = default) where TQuery : IQuery<TResult>;
}
