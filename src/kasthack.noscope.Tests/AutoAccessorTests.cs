namespace kasthack.noscope.Tests;

using kasthack.noscope.Tests.Scopes;
using kasthack.noscope.Tests.TestTypes;

using Xunit;

/// <summary>
/// Tests for Auto accessor selection.
/// </summary>
public class AutoAccessorTests
{
    [Fact]
    public void Auto_PublicProperty_SelectsDirectAccessor()
    {
        // Arrange
        var godObject = new GodObject { PublicProperty = 123 };
        var scope = new AutoAccessorScope(godObject);

        // Act
        var result = scope.PublicProperty;

        // Assert
        Assert.Equal(123, result);
    }

    [Fact]
    public void Auto_PublicProperty_Set_SelectsDirectAccessor()
    {
        // Arrange
        var godObject = new GodObject();
        var scope = new AutoAccessorScope(godObject);

        // Act
        scope.PublicProperty = 456;

        // Assert
        Assert.Equal(456, godObject.PublicProperty);
    }

    [Fact]
    public void Auto_PrivateField_SelectsGeneratedAccessor()
    {
        // Arrange
        var godObject = new GodObject(789);
        var scope = new AutoAccessorScope(godObject);

        // Act
        var result = scope._privateField;

        // Assert
        Assert.Equal(789, result);
    }

    [Fact]
    public void Auto_PrivateField_Set_SelectsGeneratedAccessor()
    {
        // Arrange
        var godObject = new GodObject();
        var scope = new AutoAccessorScope(godObject);

        // Act
        scope._privateField = 111;

        // Assert
        Assert.Equal(111, scope._privateField);
    }
}
