using System.Diagnostics;
using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.OpenTelemetry;

public class OpenTelemetryActivityScope : IActivityScope
{
    private static readonly ActivitySource Source = new("RPG.GameServer");

    public IDisposable? Start(string name, IDictionary<string, object>? tags = null)
    {
        var activity = Source.StartActivity(name, ActivityKind.Internal);

        if (activity is null)
            return null;

        if (tags is not null)
        {
            foreach (var tag in tags)
            {
                activity.SetTag(tag.Key, tag.Value);
            }
        }

        return activity;
    }
}