using BakToBucket.Features.Scheduling;
using FluentAssertions;
using Xunit;

namespace BakToBucket.Tests.Unit.Features.Scheduling;

public class WorkerSchedulingShould
{
    [Fact]
    public void ReturnSameDay_WhenScheduledTimeIsAfterNow()
    {
        // Arrange
        var now = new DateTime(2026, 6, 13, 10, 0, 0); 
        var hour = 11;
        var minute = 30;

        // Act
        var result = Worker.GetNextRunTime(now, hour, minute);

        // Assert
        result.Should().Be(new DateTime(2026, 6, 13, 11, 30, 0));
    }

    [Fact]
    public void ReturnNextDay_WhenScheduledTimeIsBeforeNow()
    {
        // Arrange
        var now = new DateTime(2026, 6, 13, 10, 0, 0); 
        var hour = 9;
        var minute = 0;

        // Act
        var result = Worker.GetNextRunTime(now, hour, minute);

        // Assert
        result.Should().Be(new DateTime(2026, 6, 14, 9, 0, 0));
    }

    [Fact]
    public void ReturnNextDay_WhenScheduledTimeIsExactlyNow()
    {
        // Arrange
        var now = new DateTime(2026, 6, 13, 10, 0, 0);
        var hour = 10;
        var minute = 0;

        // Act
        var result = Worker.GetNextRunTime(now, hour, minute);

        // Assert
        result.Should().Be(new DateTime(2026, 6, 14, 10, 0, 0));
    }

    [Fact]
    public void HandleMidnightCorrectly_WhenScheduledForEarlyMorning()
    {
        // Arrange
        var now = new DateTime(2026, 6, 13, 23, 30, 0); // 11:30 PM
        var hour = 1;
        var minute = 0;

        // Act
        var result = Worker.GetNextRunTime(now, hour, minute);

        // Assert
        result.Should().Be(new DateTime(2026, 6, 14, 1, 0, 0));
    }
}
