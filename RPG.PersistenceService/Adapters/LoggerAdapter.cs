using Microsoft.Extensions.Logging;

namespace RPG.PersistenceService.Adapters;

/// <summary>
/// Adapter for Infrastructure ILogger to Microsoft.Extensions.Logging.ILogger
/// </summary>
public class LoggerAdapter<T> : RPG.Infrastructure.Interfaces.ILogger<T>
{
    private readonly ILogger<T> _microsoftLogger;

    public LoggerAdapter(ILogger<T> microsoftLogger)
    {
        _microsoftLogger = microsoftLogger;
    }

    public void Info(string message)
    {
        _microsoftLogger.LogInformation(message);
    }

    public void Warn(string message)
    {
        _microsoftLogger.LogWarning(message);
    }

    public void Error(string message, Exception? ex = null)
    {
        _microsoftLogger.LogError(ex, message);
    }

    public void Debug(string message)
    {
        _microsoftLogger.LogDebug(message);
    }
}
