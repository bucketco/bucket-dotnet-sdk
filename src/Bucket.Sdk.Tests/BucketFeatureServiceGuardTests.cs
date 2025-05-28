namespace Bucket.Sdk.Tests;

public sealed class BucketFeatureServiceGuardTests
{
    [Fact]
    public void EnsureRegistered_ThrowsWhenArgumentsAreInvalid() =>
         // Act & Assert
         Assert.Throws<ArgumentNullException>(
           () => BucketFeatureServiceGuard.EnsureRegistered(null!)
       );

    [Fact]
    public void EnsureRegistered_ThrowsInvalidOperationException_WhenBucketFeatureServiceGuardIsNotRegistered()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
        _ = mockServiceProvider.Setup(
            sp => sp.GetService(typeof(BucketFeatureServiceGuard))
        ).Returns(null!);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            BucketFeatureServiceGuard.EnsureRegistered(mockServiceProvider.Object));

        Assert.Equal(
            $"Bucket feature service not found. Add the required service by calling '{nameof(IServiceCollection)}.{nameof(ServiceCollectionExtensions.AddBucketFeatures)}' in the application startup code.",
            exception.Message
            );
    }

    [Fact]
    public void EnsureRegistered_DoesNotThrow_WhenBucketFeatureServiceGuardIsRegistered()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
        _ = mockServiceProvider.Setup(
            sp => sp.GetService(typeof(BucketFeatureServiceGuard))
        ).Returns(new BucketFeatureServiceGuard());

        // Act & Assert
        var exception = Record.Exception(() => BucketFeatureServiceGuard.EnsureRegistered(mockServiceProvider.Object));
        Assert.Null(exception);
    }
}
