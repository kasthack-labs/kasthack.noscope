namespace kasthack.noscope.Tests;

using kasthack.noscope.Tests.Scopes;
using kasthack.noscope.Tests.TestTypes;

using Xunit;

/// <summary>
/// Tests for event support in scopes.
/// </summary>
public class EventTests
{
    [Fact]
    public void Event_Subscribe_ReceivesEvents()
    {
        // Arrange
        var godObject = new GodObject();
        var scope = new EventScope(godObject);
        var receivedValue = 0;
        var eventRaised = false;

        scope.ValueChanged += (sender, value) =>
        {
            eventRaised = true;
            receivedValue = value;
        };

        // Act
        godObject.RaiseValueChanged(42);

        // Assert
        Assert.True(eventRaised);
        Assert.Equal(42, receivedValue);
    }

    [Fact]
    public void Event_Unsubscribe_StopsReceivingEvents()
    {
        // Arrange
        var godObject = new GodObject();
        var scope = new EventScope(godObject);
        var eventCount = 0;

        void Handler(object? sender, int value) => eventCount++;

        scope.ValueChanged += Handler;
        godObject.RaiseValueChanged(1);
        scope.ValueChanged -= Handler;
        godObject.RaiseValueChanged(2);

        // Assert
        Assert.Equal(1, eventCount);
    }

    [Fact]
    public void Event_ExposesOriginalSender()
    {
        // Arrange
        var godObject = new GodObject();
        var scope = new EventScope(godObject);
        object? capturedSender = null;

        scope.ValueChanged += (sender, _) => capturedSender = sender;

        // Act
        godObject.RaiseValueChanged(1);

        // Assert - by design, original sender is exposed
        Assert.Same(godObject, capturedSender);
    }
}
