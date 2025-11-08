using System.Collections.Generic;
using System.Diagnostics;
using FluentAssertions;
using Moq;
using RPG.Infrastructure.Interfaces;
using RPG.Infrastructure.OpenTelemetry;
using Xunit;

namespace RPG.UnitTest.Infrastructure.OpenTelemetry;

public class OpenTelemetryActivityScopeTests
{
    [Fact]
    public void Start_ShouldReturnActivity_WhenListenerPresent()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "RPG.GameServer",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var logger = new Mock<ILogger<OpenTelemetryActivityScope>>();
        var scope = new OpenTelemetryActivityScope(logger.Object);
        var tags = new Dictionary<string, object> { ["tag"] = "value" };

        var disposable = scope.Start("test-activity", tags);

        disposable.Should().BeOfType<Activity>();
        var activity = (Activity)disposable!;
        activity.GetTagItem("tag").Should().Be("value");
        activity.Dispose();

        logger.Verify(l => l.Debug(It.Is<string>(msg => msg.Contains("Starting activity"))), Times.Once);
        logger.Verify(l => l.Debug(It.Is<string>(msg => msg.Contains("started with"))), Times.Once);
    }

    [Fact]
    public void Start_ShouldReturnActivity_WhenNoExternalListener()
    {
        var logger = new Mock<ILogger<OpenTelemetryActivityScope>>();
        var scope = new OpenTelemetryActivityScope(logger.Object);

        var disposable = scope.Start("no-listener");

        disposable.Should().BeOfType<Activity>();
        ((Activity)disposable!).Dispose();
        logger.Verify(l => l.Debug(It.Is<string>(msg => msg.Contains("Starting activity"))), Times.Once);
        logger.Verify(l => l.Warn(It.IsAny<string>()), Times.Never);
    }
}
