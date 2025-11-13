// filepath: /Volumes/Data/Repositories/DevHobby/RPG.Infrastructure/Interfaces/IOutboxQueueWriter.cs
namespace RPG.Infrastructure.Interfaces;

public interface IOutboxQueueWriter
{
    Task EnqueueAsync(string topic, object payload, CancellationToken ct = default);
}

