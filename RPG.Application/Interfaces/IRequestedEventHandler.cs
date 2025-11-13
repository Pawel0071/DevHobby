// filepath: /Volumes/Data/Repositories/DevHobby/RPG.Application/Interfaces/IRequestedEventHandler.cs
using System.Threading;
using System.Threading.Tasks;
using RPG.Abstractions.Interfaces;
using RPG.Infrastructure.Interfaces;

namespace RPG.Application.Interfaces;

public interface IRequestedEventHandler
{
    bool CanHandle(IGameEvent evt);
    Task HandleAsync(IGameEvent evt, CancellationToken ct);
}

