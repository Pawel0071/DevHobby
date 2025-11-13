// filepath: /Volumes/Data/Repositories/DevHobby/RPG.Infrastructure/Logger/ActivityEnricher.cs
using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace RPG.Infrastructure.Logger;

public sealed class ActivityEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var activity = Activity.Current;
        if (activity == null)
            return;

        // OpenTelemetry Activity
        var traceId = activity.TraceId.ToString();
        var spanId = activity.SpanId.ToString();

        logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("TraceId", traceId));
        logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("SpanId", spanId));
    }
}

