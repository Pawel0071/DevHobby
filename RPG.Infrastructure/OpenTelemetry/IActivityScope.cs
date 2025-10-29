namespace RPG.Infrastructure.OpenTelemetry;

public interface IActivityScope
{
    IDisposable? Start(string name, IDictionary<string, object>? tags = null);
}