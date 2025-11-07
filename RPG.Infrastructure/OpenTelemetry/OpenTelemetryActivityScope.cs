using System.Diagnostics;
using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.OpenTelemetry;

public class OpenTelemetryActivityScope : IActivityScope
{
    private static readonly ActivitySource Source = new("RPG.GameServer");
    private readonly ILogger<OpenTelemetryActivityScope> _logger;

    public OpenTelemetryActivityScope(ILogger<OpenTelemetryActivityScope> logger)
    {
        _logger = logger;
    }

    public IDisposable? Start(string name, IDictionary<string, object>? tags = null)
    {
        _logger.Debug($"Starting activity: {name}");
        
        var activity = Source.StartActivity(name, ActivityKind.Internal);

        if (activity is null)
        {
            _logger.Warn($"Failed to start activity: {name}");
            return null;
        }

        if (tags is not null)
        {
            foreach (var tag in tags)
            {
                activity.SetTag(tag.Key, tag.Value);
            }
            _logger.Debug($"Activity {name} started with {tags.Count} tags");
        }

        return activity;
    }
}