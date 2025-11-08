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
    private readonly CollectingSink _sink = new();

    public SerilogWrapperTests()
    {
        _previousLogger = Log.Logger;
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

        _sink.Events.Should().HaveCount(4);
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
        public List<LogEvent> Events { get; } = [];

        public void Emit(LogEvent logEvent)
        {
            Events.Add(logEvent);
        }

        public void ShouldContain(LogEventLevel level, string message)
        {
            Events.Should().Contain(e => e.Level == level && e.RenderMessage(null).Contains(message));
        }

        public LogEvent GetEvent(LogEventLevel level)
        {
            return Events.Should().ContainSingle(e => e.Level == level).Subject;
        }
    }
}
