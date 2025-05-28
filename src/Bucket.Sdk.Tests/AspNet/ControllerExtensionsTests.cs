namespace Bucket.Sdk.Tests.AspNet;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

public sealed class ControllerExtensionsTests
{
    private const string _featureKey = "test-feature";

    private readonly Mock<Controller> _mockController;
    private readonly Mock<IFeature> _mockFeature;
    private readonly Mock<IFeatureClient> _mockFeatureClient;
    private readonly Mock<IFeature<string>> _mockFeatureGeneric;

    public ControllerExtensionsTests()
    {
        _mockFeatureClient = new Mock<IFeatureClient>(MockBehavior.Strict);
        _mockController = new Mock<Controller>(MockBehavior.Strict);
        _mockFeature = new Mock<IFeature>(MockBehavior.Strict);
        _mockFeatureGeneric = new Mock<IFeature<string>>(MockBehavior.Strict);

        var mockHttpContext = new Mock<HttpContext>(MockBehavior.Strict);
        var mockServiceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);

        _ = _mockFeatureClient.Setup(m => m.GetFeatureAsync(
                It.IsAny<string>(), It.IsAny<Context>(), It.IsAny<TrackingStrategy>()
            )
        ).ReturnsAsync(_mockFeature.Object);

        _ = _mockFeatureClient.Setup(m => m.GetFeatureAsync<string>(
                It.IsAny<string>(), It.IsAny<Context>(), It.IsAny<TrackingStrategy>()
            )
        ).ReturnsAsync(_mockFeatureGeneric.Object);

        _mockController.Object.ControllerContext = new ControllerContext { HttpContext = mockHttpContext.Object };

        _ = mockServiceProvider.Setup(
            m => m.GetService(It.IsAny<Type>())
            )
        .Returns(() => null);

        _ = mockServiceProvider.Setup(
            m => m.GetService(typeof(IFeatureClient))
        ).Returns(_mockFeatureClient.Object);
        _ = mockServiceProvider.Setup(
            m => m.GetService(typeof(BucketFeatureServiceGuard))
        ).Returns(new BucketFeatureServiceGuard());

        _ = mockHttpContext.Setup(m => m.RequestServices)
            .Returns(mockServiceProvider.Object);
        _ = mockHttpContext.Setup(m => m.Items)
            .Returns(new Dictionary<object, object?>());
        _ = mockHttpContext.Setup(
            m => m.Features.Get<IHttpActivityFeature>()
        ).Returns(() => null);
    }

    [Fact]
    public async Task GetFeatureAsync_ThrowsWhenArgumentsAreInvalid_Async()
    {
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            ((Controller) null!).GetFeatureAsync(_featureKey));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _mockController.Object.GetFeatureAsync(null!));
        _ = await Assert.ThrowsAsync<ArgumentException>(() =>
            _mockController.Object.GetFeatureAsync(string.Empty));
    }

    [Fact]
    public async Task GetFeatureAsync_DelegatesToHttpContext_Async()
    {
        var result = await _mockController.Object.GetFeatureAsync(_featureKey);

        Assert.Equal(_mockFeature.Object, result);

        _mockFeatureClient.Verify(
            v => v.GetFeatureAsync(
                _featureKey, It.IsAny<Context>(), It.IsAny<TrackingStrategy>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task GetFeatureAsync_Generic_ThrowsWhenArgumentsAreInvalid_Async()
    {
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            ((Controller) null!).GetFeatureAsync<string>(_featureKey));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _mockController.Object.GetFeatureAsync<string>(null!));
        _ = await Assert.ThrowsAsync<ArgumentException>(() =>
            _mockController.Object.GetFeatureAsync<string>(string.Empty));
    }

    [Fact]
    public async Task GetFeatureAsync_Generic_DelegatesToHttpContext_Async()
    {
        var result = await _mockController.Object.GetFeatureAsync<string>(_featureKey);

        Assert.Equal(_mockFeatureGeneric.Object, result);

        _mockFeatureClient.Verify(
            v => v.GetFeatureAsync<string>(
                _featureKey, It.IsAny<Context>(), It.IsAny<TrackingStrategy>()
            ),
            Times.Once
        );
    }
}
