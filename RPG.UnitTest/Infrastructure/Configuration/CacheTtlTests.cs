using System;
using FluentAssertions;
using RPG.Infrastructure.Configuration;
using Xunit;

namespace RPG.UnitTest.Infrastructure.Configuration;

public class CacheTtlTests
{
    [Fact]
    public void PredefinedTtls_ShouldReturnExpectedDurations()
    {
        CacheTtl.Short.Should().Be(TimeSpan.FromMinutes(5));
        CacheTtl.Medium.Should().Be(TimeSpan.FromHours(1));
        CacheTtl.Long.Should().Be(TimeSpan.FromHours(24));
        CacheTtl.Permanent.Should().BeNull();
    }

    [Fact]
    public void FactoryMethods_ShouldReturnCorrectValues()
    {
        CacheTtl.Minutes(15).Should().Be(TimeSpan.FromMinutes(15));
        CacheTtl.Hours(2).Should().Be(TimeSpan.FromHours(2));
        CacheTtl.Days(3).Should().Be(TimeSpan.FromDays(3));
    }
}
