using System;
using System.Collections.Generic;
using FluentAssertions;
using RPG.Infrastructure.Logger;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace RPG.UnitTest.Infrastructure.Logger;

public class SerilogWrapperTests : IDisposable
{
    private readonly ILogger _previousLogger;
    private readonly CollectingSink _sink;

    public SerilogWrapperTests()
    {
        _previousLogger = Log.Logger;
        _sink = new CollectingSink(typeof(SerilogWrapperTests));
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Sink(_sink)
            .CreateLogger();
    }

    [Fact]
    public void Wrapper_ShouldForwardCallsToSerilog()
    {
        var wrapper = new SerilogWrapper<SerilogWrapperTests>();
        var exception = new InvalidOperationException("boom");

        wrapper.Info("info message");
        wrapper.Warn("warn message");
        wrapper.Debug("debug message");
        wrapper.Error("error message", exception);

        _sink.ContextEvents.Should().HaveCount(4);
        _sink.ShouldContain(LogEventLevel.Information, "info message");
        _sink.ShouldContain(LogEventLevel.Warning, "warn message");
        _sink.ShouldContain(LogEventLevel.Debug, "debug message");
        var errorEvent = _sink.GetEvent(LogEventLevel.Error);
        errorEvent.Exception.Should().Be(exception);
        errorEvent.RenderMessage(null).Should().Contain("error message");
    }

    public void Dispose()
    {
        Log.Logger = _previousLogger;
    }

    private sealed class CollectingSink : ILogEventSink
    {
        private readonly string _contextName;
        public List<LogEvent> Events { get; } = [];

        public CollectingSink(Type contextType)
        {
            _contextName = contextType.FullName ?? contextType.Name;
        }

        public void Emit(LogEvent logEvent)
        {
            Events.Add(logEvent);
        }

        public IEnumerable<LogEvent> ContextEvents => Events.Where(IsContextEvent);

        private bool IsContextEvent(LogEvent logEvent)
        {
            if (!logEvent.Properties.TryGetValue("SourceContext", out var value))
            {
                return false;
            }

            if (value is not ScalarValue scalarValue)
            {
                return false;
            }

            return scalarValue.Value is string context && context == _contextName;
        }

        public void ShouldContain(LogEventLevel level, string message)
        {
            ContextEvents.Should().Contain(e => e.Level == level && e.RenderMessage(null).Contains(message));
        }

        public LogEvent GetEvent(LogEventLevel level)
        {
            return ContextEvents.Should().ContainSingle(e => e.Level == level).Subject;
        }
    }
}
