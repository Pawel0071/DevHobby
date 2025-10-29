namespace RPG.Core.Infrastructure.Services.Logger;

public interface ILogger<T>
{
    void Info(string message);
    void Warn(string message);
    void Error(string message, Exception? ex = null);
    void Debug(string message);
}