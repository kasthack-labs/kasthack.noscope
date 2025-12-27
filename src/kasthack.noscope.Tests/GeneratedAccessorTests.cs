namespace kasthack.noscope.Tests;

using kasthack.noscope.Tests.Scopes;
using kasthack.noscope.Tests.TestTypes;

using Xunit;

/// <summary>
/// Tests for GeneratedAccessor functionality (compile-time accessors for partial types).
/// </summary>
public class GeneratedAccessorTests
{
    [Fact]
    public void PrivateField_Get_ReturnsValue()
    {
        // Arrange
        var godObject = new GodObject(77);
        var scope = new GeneratedAccessorScope(godObject);

        // Act
        var result = scope._privateField;

        // Assert
        Assert.Equal(77, result);
    }

    [Fact]
    public void PrivateField_Set_UpdatesTarget()
    {
        // Arrange
        var godObject = new GodObject();
        var scope = new GeneratedAccessorScope(godObject);

        // Act
        scope._privateField = 88;

        // Assert - verify via scope getter since field is private
        Assert.Equal(88, scope._privateField);
    }

    [Fact]
    public void PrivateStringField_Get_ReturnsValue()
    {
        // Arrange
        var godObject = new GodObject();
        var scope = new GeneratedAccessorScope(godObject);

        // Act
        var result = scope._privateString;

        // Assert
        Assert.Equal("private", result);
    }

    [Fact]
    public void PrivateStringField_Set_UpdatesTarget()
    {
        // Arrange
        var godObject = new GodObject();
        var scope = new GeneratedAccessorScope(godObject);

        // Act
        scope._privateString = "modified";

        // Assert
        Assert.Equal("modified", scope._privateString);
    }

    [Fact]
    public void ReadonlyPrivateField_Get_ReturnsValue()
    {
        // Arrange
        var godObject = new GodObject();
        var scope = new GeneratedAccessorScope(godObject);

        // Act
        var result = scope._readonlyPrivateField;

        // Assert
        Assert.Equal(100, result);
    }
}
