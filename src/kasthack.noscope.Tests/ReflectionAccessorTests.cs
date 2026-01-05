namespace kasthack.noscope.Tests;

using kasthack.noscope.Tests.Scopes;
using kasthack.noscope.Tests.TestTypes;

using Xunit;

/// <summary>
/// Tests for ReflectionAccessor functionality (runtime reflection for non-partial types).
/// </summary>
public class ReflectionAccessorTests
{
    [Fact]
    public void PublicProperty_Get_UsesDirectAccessor()
    {
        // Arrange
        var target = new SealedBinaryType { PublicValue = 500 };
        var scope = new ReflectionAccessorScope(target);

        // Act
        var result = scope.PublicValue;

        // Assert
        Assert.Equal(500, result);
    }

    [Fact]
    public void PublicProperty_Set_UsesDirectAccessor()
    {
        // Arrange
        var target = new SealedBinaryType();
        var scope = new ReflectionAccessorScope(target);

        // Act
        scope.PublicValue = 600;

        // Assert
        Assert.Equal(600, target.PublicValue);
    }

    [Fact]
    public void PrivateField_Get_UsesReflection()
    {
        // Arrange
        var target = new SealedBinaryType(999, "test");
        var scope = new ReflectionAccessorScope(target);

        // Act
        var result = scope._privateValue;

        // Assert
        Assert.Equal(999, result);
    }

    [Fact]
    public void PrivateField_Set_UsesReflection()
    {
        // Arrange
        var target = new SealedBinaryType();
        var scope = new ReflectionAccessorScope(target);

        // Act
        scope._privateValue = 777;

        // Assert - verify via GetPrivateValue method
        Assert.Equal(777, target.GetPrivateValue());
    }

    [Fact]
    public void PrivateStringField_Get_UsesReflection()
    {
        // Arrange
        var target = new SealedBinaryType(0, "secret");
        var scope = new ReflectionAccessorScope(target);

        // Act
        var result = scope._privateName;

        // Assert
        Assert.Equal("secret", result);
    }

    [Fact]
    public void PrivateStringField_Set_UsesReflection()
    {
        // Arrange
        var target = new SealedBinaryType();
        var scope = new ReflectionAccessorScope(target);

        // Act
        scope._privateName = "modified";

        // Assert - verify via GetPrivateName method
        Assert.Equal("modified", target.GetPrivateName());
    }
}
