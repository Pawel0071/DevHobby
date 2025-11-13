using System;
using FluentAssertions;
using RPG.Infrastructure.Outbox;
using Xunit;

namespace RPG.UnitTest.Infrastructure.Outbox;

public class OutboxMessageTests
{
    [Fact]
    public void Constructor_ShouldInitializeDefaults()
    {
        var message = new OutboxMessage();

        message.Id.Should().NotBe(Guid.Empty);
        message.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        message.RetryCount.Should().Be(0);
        message.LastRetryAt.Should().BeNull();
    }
}
