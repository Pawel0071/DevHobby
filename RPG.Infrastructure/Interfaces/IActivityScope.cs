namespace RPG.Infrastructure.Interfaces;

public interface IActivityScope
{
    IDisposable? Start(string name, IDictionary<string, object>? tags = null);
}
