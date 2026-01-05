namespace kasthack.noscope.Tests;

using kasthack.noscope.Tests.Scopes;
using kasthack.noscope.Tests.TestTypes;

using Xunit;

/// <summary>
/// Tests for Direct accessor functionality.
/// </summary>
public class DirectAccessorTests
{
    [Fact]
    public void PublicProperty_Get_ReturnsValue()
    {
        // Arrange
        var godObject = new GodObject { PublicProperty = 42 };
        var scope = new DirectAccessorScope(godObject);

        // Act
        var result = scope.PublicProperty;

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public void PublicProperty_Set_UpdatesTarget()
    {
        // Arrange
        var godObject = new GodObject();
        var scope = new DirectAccessorScope(godObject);

        // Act
        scope.PublicProperty = 99;

        // Assert
        Assert.Equal(99, godObject.PublicProperty);
    }

    [Fact]
    public void PublicReadOnlyProperty_Get_ReturnsValue()
    {
        // Arrange
        var godObject = new GodObject();
        var scope = new DirectAccessorScope(godObject);

        // Act
        var result = scope.PublicReadOnlyProperty;

        // Assert
        Assert.Equal(20, result);
    }

    [Fact]
    public void PublicMethod_Executes()
    {
        // Arrange
        var godObject = new GodObject(100);
        var scope = new DirectAccessorScope(godObject);

        // Act
        scope.PublicMethod();

        // Assert - method increments private field, check via return method
        Assert.Equal(101, scope.PublicMethodWithReturn());
    }

    [Fact]
    public void PublicMethodWithReturn_ReturnsValue()
    {
        // Arrange
        var godObject = new GodObject(55);
        var scope = new DirectAccessorScope(godObject);

        // Act
        var result = scope.PublicMethodWithReturn();

        // Assert
        Assert.Equal(55, result);
    }

    [Fact]
    public void PublicMethodWithParameters_ReturnsSum()
    {
        // Arrange
        var godObject = new GodObject();
        var scope = new DirectAccessorScope(godObject);

        // Act
        var result = scope.PublicMethodWithParameters(10, 20);

        // Assert
        Assert.Equal(30, result);
    }

    [Fact]
    public void ScopeMember_Name_RenamesProperty()
    {
        // Arrange
        var godObject = new GodObject { WhoCoMesUpWiThThEsEnAmEs = 123 };
        var scope = new DirectAccessorScope(godObject);

        // Act
        var result = scope.NiceName;

        // Assert
        Assert.Equal(123, result);
    }

    [Fact]
    public void ScopeMember_Name_SetRenamesProperty()
    {
        // Arrange
        var godObject = new GodObject();
        var scope = new DirectAccessorScope(godObject);

        // Act
        scope.NiceName = 456;

        // Assert
        Assert.Equal(456, godObject.WhoCoMesUpWiThThEsEnAmEs);
    }

    [Fact]
    public void Target_ReturnsUnderlyingObject()
    {
        // Arrange
        var godObject = new GodObject();
        var scope = new DirectAccessorScope(godObject);

        // Act & Assert
        Assert.Same(godObject, scope.Target);
    }
}
