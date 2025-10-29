using Serilog;

namespace RPG.Infrastructure.Logger;

public class SerilogWrapper<T> : ILogger<T>
{
    private readonly Serilog.ILogger _logger;

    public SerilogWrapper()
    {
        _logger = Log.ForContext(typeof(T));
    }

    public void Info(string message) => _logger.Information(message);
    public void Warn(string message) => _logger.Warning(message);
    public void Error(string message, Exception? ex = null) => _logger.Error(ex, message);
    public void Debug(string message) => _logger.Debug(message);
}