namespace Bucket.Sdk.Tests.AspNet;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;

public sealed class FeatureRestrictedActionFilterTests
{
    private const string _featureKey = "test-feature";
    private static bool _mockAsyncActionFilterInvoked;

    private readonly ActionExecutingContext _mockActionExecutingContext;
    private readonly Mock<IFeature> _mockFeature;
    private readonly Mock<ActionExecutionDelegate> _mockNext;

    public FeatureRestrictedActionFilterTests()
    {
        _mockAsyncActionFilterInvoked = false;
        _mockFeature = new Mock<IFeature>(MockBehavior.Strict);

        var mockFeatureClient = new Mock<IFeatureClient>(MockBehavior.Strict);
        _ = mockFeatureClient
            .Setup(m => m.GetFeatureAsync(
                    _featureKey,
                    It.IsAny<Context>(),
                    It.IsAny<TrackingStrategy>()
                )
            )
            .ReturnsAsync(_mockFeature.Object);

        _mockNext = new Mock<ActionExecutionDelegate>(MockBehavior.Strict);
        _ = _mockNext.Setup(n => n.Invoke()).ReturnsAsync(() => null!);

        var mockServiceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
        _ = mockServiceProvider
            .Setup(m => m.GetService(It.IsAny<Type>()))
            .Returns(() => null);
        _ = mockServiceProvider
            .Setup(m => m.GetService(typeof(IFeatureClient)))
            .Returns(mockFeatureClient.Object);
        _ = mockServiceProvider
            .Setup(m => m.GetService(typeof(BucketFeatureServiceGuard)))
            .Returns(new BucketFeatureServiceGuard());

        var mockHttpContext = new Mock<HttpContext>(MockBehavior.Strict);
        _ = mockHttpContext
            .Setup(m => m.RequestServices)
            .Returns(mockServiceProvider.Object);
        _ = mockHttpContext
            .Setup(m => m.Items)
            .Returns(new Dictionary<object, object?>());

        var mockController = new Mock<Controller>(MockBehavior.Strict);
        _mockActionExecutingContext = new ActionExecutingContext(
            new ActionContext(
                mockHttpContext.Object,
                new RouteData(),
                new ActionDescriptor(),
                new ModelStateDictionary()
            ),
            [],
            new Dictionary<string, object?>(),
            mockController.Object
        );
    }

    [Fact]
    public void Constructor_ThrowsWhenArgumentsAreInvalid()
    {
        _ = Assert.Throws<ArgumentNullException>(() =>
            new FeatureRestrictedActionFilter<MockAsyncActionFilter>(null!));
        _ = Assert.Throws<ArgumentException>(() =>
            new FeatureRestrictedActionFilter<MockAsyncActionFilter>(string.Empty));
    }

    [Fact]
    public async Task OnActionExecutionAsync_ThrowsWhenArgumentsAreInvalid_Async()
    {
        var filter = new FeatureRestrictedActionFilter<MockAsyncActionFilter>(_featureKey);

        _ = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            filter.OnActionExecutionAsync(null!, _mockNext.Object));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            filter.OnActionExecutionAsync(_mockActionExecutingContext, null!));
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task OnActionExecutionAsync_ExecutesFilterBasedOnFeatureStateAndRequirement_Async(
        bool featureEnabled, bool requiresEnabled)
    {
        _ = _mockFeature.Setup(m => m.Enabled).Returns(featureEnabled);
        var mockFeatureRestrictedActionFilter = new FeatureRestrictedActionFilter<MockAsyncActionFilter>(
            _featureKey, requiresEnabled
        );

        await mockFeatureRestrictedActionFilter.OnActionExecutionAsync(
            _mockActionExecutingContext,
            _mockNext.Object
        );

        _mockNext.Verify(
            v => v.Invoke(),
            Times.Once
        );

        Assert.Equal(featureEnabled == requiresEnabled, _mockAsyncActionFilterInvoked);
    }

    [UsedImplicitly]
    private sealed class MockAsyncActionFilter: IAsyncActionFilter
    {
        public MockAsyncActionFilter(IFeatureClient featureClient) =>
            Assert.NotNull(featureClient);

        public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            _mockAsyncActionFilterInvoked = true;
            return next();
        }
    }
}
