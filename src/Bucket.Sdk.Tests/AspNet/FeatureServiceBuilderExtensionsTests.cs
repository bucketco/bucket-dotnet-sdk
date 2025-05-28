namespace Bucket.Sdk.Tests.AspNet;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public sealed class FeatureServiceBuilderExtensionsTests
{
    private readonly Mock<IFeatureServiceBuilder> _mockFeatureServiceBuilder;
    private readonly Mock<IServiceCollection> _mockServiceCollection;

    public FeatureServiceBuilderExtensionsTests()
    {
        _mockServiceCollection = new Mock<IServiceCollection>(MockBehavior.Strict);
        _mockFeatureServiceBuilder = new Mock<IFeatureServiceBuilder>(MockBehavior.Strict);

        _ = _mockServiceCollection.Setup(s => s.Add(It.IsAny<ServiceDescriptor>()));
        _ = _mockFeatureServiceBuilder.Setup(s => s.Services).Returns(_mockServiceCollection.Object);
    }

    private static ValueTask<IActionResult> NoOpActionHandler(IFeature _, FilterContext __) => ValueTask.FromResult<IActionResult>(new NoContentResult());
    private static ValueTask<object?> NoOpEndpointHandler(IFeature _, EndpointFilterInvocationContext __) => ValueTask.FromResult<object?>(null);

    private static ValueTask<(Context context, TrackingStrategy trackingStrategy)> Resolver(HttpContext _) =>
        ValueTask.FromResult((new Context(), TrackingStrategy.Default));

    [Fact]
    public void UseContextResolver_ThrowsWhenArgumentsAreInvalid()
    {
        // Arrange & Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            ((IFeatureServiceBuilder) null!).UseContextResolver(null!));
        _ = Assert.Throws<ArgumentNullException>(() =>
            _mockFeatureServiceBuilder.Object.UseContextResolver(null!));
    }

    [Fact]
    public void UseContextResolver_RegistersResolverDelegate()
    {
        // Arrange
        var result = _mockFeatureServiceBuilder.Object.UseContextResolver(Resolver);

        // Assert
        Assert.Equal(_mockFeatureServiceBuilder.Object, result);

        _mockServiceCollection.Verify(
            s => s.Add(It.Is<ServiceDescriptor>(sd =>
                    sd.Lifetime == ServiceLifetime.Singleton &&
                    sd.ServiceType == typeof(ResolveEvaluationContextAsyncDelegate)
                )
            ),
            Times.Once
        );
    }

    [Fact]
    public void UseRestrictedFeatureActionHandler_ThrowsArgumentsAreInvalid()
    {
        // Arrange & Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            ((IFeatureServiceBuilder) null!).UseRestrictedFeatureHandler(NoOpActionHandler));
        _ = Assert.Throws<ArgumentNullException>(() =>
            _mockFeatureServiceBuilder.Object.UseRestrictedFeatureHandler(null!));
        _ = Assert.Throws<ArgumentNullException>(() =>
            ((IFeatureServiceBuilder) null!).UseRestrictedFeatureHandler(NoOpEndpointHandler));
        _ = Assert.Throws<ArgumentNullException>(() =>
            _mockFeatureServiceBuilder.Object.UseRestrictedFeatureHandler((RestrictedFeatureEndpointHandlerAsyncDelegate) null!));
    }

    [Fact]
    public void UseRestrictedFeatureActionHandler_RegistersActionHandlerDelegate()
    {
        // Arrange
        var result =
            _mockFeatureServiceBuilder.Object.UseRestrictedFeatureHandler(NoOpActionHandler);

        // Assert
        Assert.Equal(_mockFeatureServiceBuilder.Object, result);

        _mockServiceCollection.Verify(
            s => s.Add(It.Is<ServiceDescriptor>(sd =>
                    sd.Lifetime == ServiceLifetime.Singleton &&
                    sd.ServiceType == typeof(RestrictedFeatureActionHandlerAsyncDelegate)
                )
            ),
            Times.Once
        );
    }

    [Fact]
    public void UseRestrictedFeatureActionHandler_RegistersEndpointHandlerDelegate()
    {
        // Arrange
        var result =
            _mockFeatureServiceBuilder.Object.UseRestrictedFeatureHandler(NoOpEndpointHandler);

        // Assert
        Assert.Equal(_mockFeatureServiceBuilder.Object, result);

        _mockServiceCollection.Verify(
            s => s.Add(It.Is<ServiceDescriptor>(sd =>
                    sd.Lifetime == ServiceLifetime.Singleton &&
                    sd.ServiceType == typeof(RestrictedFeatureEndpointHandlerAsyncDelegate)
                )
            ),
            Times.Once
        );
    }
}
