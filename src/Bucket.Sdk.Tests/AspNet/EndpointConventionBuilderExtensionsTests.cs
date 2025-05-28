namespace Bucket.Sdk.Tests.AspNet;

using Microsoft.AspNetCore.Builder;

public sealed class EndpointConventionBuilderExtensionsTests
{
    private const string _featureKey = "test-feature";

    private readonly Mock<IEndpointConventionBuilder> _mockEndpointConventionBuilder;

    public EndpointConventionBuilderExtensionsTests()
    {
        _mockEndpointConventionBuilder = new Mock<IEndpointConventionBuilder>(MockBehavior.Strict);
        _ = _mockEndpointConventionBuilder.Setup(m => m.Add(It.IsAny<Action<EndpointBuilder>>()));
    }

    [Fact]
    public void WithFeatureRestriction_ThrowsWhenArgumentsAreInvalid()
    {
        // Arrange & Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            ((IEndpointConventionBuilder) null!).WithFeatureRestriction(_featureKey));
        _ = Assert.Throws<ArgumentNullException>(() =>
            _mockEndpointConventionBuilder.Object.WithFeatureRestriction(null!));
        _ = Assert.Throws<ArgumentException>(() =>
            _mockEndpointConventionBuilder.Object.WithFeatureRestriction(string.Empty));
    }

    [Fact]
    public void WithFeatureRestriction_AddsEndpointFilter()
    {
        // Act
        var result = _mockEndpointConventionBuilder.Object.WithFeatureRestriction(_featureKey);

        // Assert
        Assert.Equal(_mockEndpointConventionBuilder.Object, result);

        _mockEndpointConventionBuilder.Verify(
            v => v.Add(It.IsAny<Action<EndpointBuilder>>()),
            Times.Once
        );
    }

    [Fact]
    public void WithFeatureRestriction_AddsEndpointFilterWithEnabledValue()
    {
        // Act
        var result = _mockEndpointConventionBuilder.Object.WithFeatureRestriction(_featureKey, false);

        // Assert
        Assert.Equal(_mockEndpointConventionBuilder.Object, result);

        _mockEndpointConventionBuilder.Verify(
            v => v.Add(It.IsAny<Action<EndpointBuilder>>()),
            Times.Once
        );
    }
}
