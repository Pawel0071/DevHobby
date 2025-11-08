namespace RPG.Core.Interfaces;

public interface IDictionaryWarmupService
{
    Task WarmupAsync(CancellationToken cancellationToken = default);
}
