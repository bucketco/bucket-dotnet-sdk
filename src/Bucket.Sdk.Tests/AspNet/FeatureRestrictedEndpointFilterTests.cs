namespace Bucket.Sdk.Tests.AspNet;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

public sealed class FeatureRestrictedEndpointFilterTests
{
    private const string _featureKey = "test-feature";

    private readonly Mock<EndpointFilterInvocationContext> _mockEndpointFilterInvocationContext;
    private readonly Mock<IFeature> _mockFeature;
    private readonly Mock<EndpointFilterDelegate> _mockNext;
    private readonly object _result = new();

    public FeatureRestrictedEndpointFilterTests()
    {
        var mockFeatureClient = new Mock<IFeatureClient>(MockBehavior.Strict);

        _mockFeature = new Mock<IFeature>(MockBehavior.Strict);
        _ = mockFeatureClient.Setup(m => m.GetFeatureAsync(
                _featureKey, It.IsAny<Context>(), It.IsAny<TrackingStrategy>()
            )
        ).ReturnsAsync(_mockFeature.Object);

        _mockEndpointFilterInvocationContext = new Mock<EndpointFilterInvocationContext>(MockBehavior.Strict);

        _ = _mockEndpointFilterInvocationContext
            .Setup(m => m.HttpContext.RequestServices.GetService(It.IsAny<Type>()))
            .Returns(() => null);

        _ = _mockEndpointFilterInvocationContext
            .Setup(m => m.HttpContext.RequestServices.GetService(typeof(IFeatureClient)))
            .Returns(mockFeatureClient.Object);

        _ = _mockEndpointFilterInvocationContext
            .Setup(m => m.HttpContext.RequestServices.GetService(typeof(BucketFeatureServiceGuard)))
            .Returns(new BucketFeatureServiceGuard());

        _ = _mockEndpointFilterInvocationContext
            .Setup(m => m.HttpContext.Items)
            .Returns(new Dictionary<object, object?>());

        _mockNext = new Mock<EndpointFilterDelegate>(MockBehavior.Strict);
        _ = _mockNext.Setup(n => n(_mockEndpointFilterInvocationContext.Object)).ReturnsAsync(_result);
    }

    [Fact]
    public void Constructor_ThrowsWhenArgumentsAreInvalid()
    {
        _ = Assert.Throws<ArgumentNullException>(() => new FeatureRestrictedEndpointFilter(null!));
        _ = Assert.Throws<ArgumentException>(() => new FeatureRestrictedEndpointFilter(string.Empty));
        _ = Assert.Throws<ArgumentException>(() => new FeatureRestrictedEndpointFilter(""));
    }

    [Fact]
    public async Task InvokeAsync_ThrowsWhenArgumentsAreInvalid_Async()
    {
        var filter = new FeatureRestrictedEndpointFilter(_featureKey);

        _ = await Assert.ThrowsAsync<ArgumentNullException>(async () => await filter.InvokeAsync(null!, _mockNext.Object));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await filter.InvokeAsync(_mockEndpointFilterInvocationContext.Object, null!));
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task InvokeAsync_ReturnsCorrectResponseBasedOnFeatureStateAndRequirement_Async(
        bool featureEnabled, bool requiresEnabled)
    {
        var filter = new FeatureRestrictedEndpointFilter(_featureKey, requiresEnabled);
        _ = _mockFeature.Setup(f => f.Enabled).Returns(featureEnabled);

        var result = await filter.InvokeAsync(
            _mockEndpointFilterInvocationContext.Object, _mockNext.Object);

        if (featureEnabled == requiresEnabled)
        {
            Assert.Equal(_result, result);

            _mockNext.Verify(
                v => v.Invoke(_mockEndpointFilterInvocationContext.Object),
                Times.Once
            );
        }
        else
        {
            _ = Assert.IsType<NotFound>(result);

            _mockNext.Verify(
                v => v.Invoke(_mockEndpointFilterInvocationContext.Object),
                Times.Never
            );
        }
    }
}
