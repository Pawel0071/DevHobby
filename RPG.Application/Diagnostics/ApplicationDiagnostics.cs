using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace RPG.Application.Diagnostics;

internal static class ApplicationDiagnostics
{
    public static readonly ActivitySource ActivitySource = new("RPG.Application");

    private static readonly Meter Meter = new("RPG.Application", "1.0.0");
    public static readonly Counter<long> CommandsCounter = Meter.CreateCounter<long>("app_commands_total");
    public static readonly Counter<long> EventsCounter = Meter.CreateCounter<long>("app_events_total");
    public static readonly Counter<long> CommandTypeCounter = Meter.CreateCounter<long>("app_command_type_total");
    public static readonly Counter<long> EventTypeCounter = Meter.CreateCounter<long>("app_event_type_total");

    private static string _serviceName = "RPG";
    public static void Init(string serviceName)
    {
        if (!string.IsNullOrWhiteSpace(serviceName))
            _serviceName = serviceName;
    }

    public static void CountCommand(string commandType)
    {
        CommandsCounter.Add(1,
            new KeyValuePair<string, object?>("command", commandType),
            new KeyValuePair<string, object?>("service", _serviceName));

        CommandTypeCounter.Add(1,
            new KeyValuePair<string, object?>("command", commandType),
            new KeyValuePair<string, object?>("service", _serviceName));
    }

    public static void CountEvent(string eventType)
    {
        EventsCounter.Add(1,
            new KeyValuePair<string, object?>("event", eventType),
            new KeyValuePair<string, object?>("service", _serviceName));

        EventTypeCounter.Add(1,
            new KeyValuePair<string, object?>("event", eventType),
            new KeyValuePair<string, object?>("service", _serviceName));
    }
}
