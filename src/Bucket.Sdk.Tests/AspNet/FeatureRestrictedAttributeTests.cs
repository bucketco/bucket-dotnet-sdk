namespace Bucket.Sdk.Tests.AspNet;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;

public sealed class FeatureRestrictedAttributeTests
{
    private const string _featureKey = "test-feature";
    private readonly ActionExecutingContext _mockActionExecutingContext;
    private readonly Mock<IFeature> _mockFeature;
    private readonly PageHandlerExecutingContext _mockPageHandlerExecutingContext;
    private readonly PageHandlerSelectedContext _mockPageHandlerSelectedContext;

    private readonly Mock<IServiceProvider> _mockServiceProvider;

    public FeatureRestrictedAttributeTests()
    {
        var mockHttpContext = new Mock<HttpContext>(MockBehavior.Strict);
        var mockFeatureClient = new Mock<IFeatureClient>(MockBehavior.Strict);

        _mockServiceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
        _mockFeature = new Mock<IFeature>(MockBehavior.Strict);

        var context = new Context();

        _ = _mockServiceProvider
            .Setup(m => m.GetService(typeof(RestrictedFeatureActionHandlerAsyncDelegate)))
            .Returns((object?) null);

        _ = _mockServiceProvider
            .Setup(m => m.GetService(typeof(IFeatureClient)))
            .Returns(mockFeatureClient.Object);
        _ = _mockServiceProvider
            .Setup(m => m.GetService(typeof(BucketFeatureServiceGuard)))
            .Returns(new BucketFeatureServiceGuard());

        _ = _mockServiceProvider
            .Setup(m => m.GetService(typeof(ResolveEvaluationContextAsyncDelegate)))
            .Returns(() =>
                new ResolveEvaluationContextAsyncDelegate(_ => ValueTask.FromResult((context, TrackingStrategy.Active))
                )
            );

        _ = mockHttpContext.Setup(m => m.RequestServices).Returns(_mockServiceProvider.Object);
        _ = mockHttpContext.Setup(m => m.Items).Returns(new Dictionary<object, object?>());
        _ = mockHttpContext.Setup(m => m.Features.Get<IHttpActivityFeature>())
            .Returns(() => null);

        _ = mockFeatureClient
            .Setup(m => m.GetFeatureAsync(
                _featureKey,
                It.IsAny<Context>(),
                It.IsAny<TrackingStrategy>())
            )
            .ReturnsAsync(_mockFeature.Object);

        var routeData = new RouteData();
        var actionDescriptor = new ActionDescriptor();
        var mockController = new Mock<Controller>(MockBehavior.Strict);
        var modelState = new ModelStateDictionary();
        var actionContext = new ActionContext(
            mockHttpContext.Object,
            routeData,
            actionDescriptor,
            modelState
        );

        _mockActionExecutingContext = new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?>(),
            mockController.Object
        );

        var pageContext = new PageContext(
            actionContext
        );

        var mockPageModel = new Mock<PageModel>(MockBehavior.Strict);
        _mockPageHandlerExecutingContext = new PageHandlerExecutingContext(
            pageContext,
            [],
            null,
            new Dictionary<string, object?>(),
            mockPageModel.Object
        );

        _mockPageHandlerSelectedContext = new PageHandlerSelectedContext(
            pageContext,
            [],
            mockPageModel.Object
        );
    }

    [Fact]
    public void Constructor_SetsProperties()
    {
        var attribute = new FeatureRestrictedAttribute(_featureKey);

        Assert.Equal(_featureKey, attribute.FeatureKey);
        Assert.True(attribute.RequireEnabled);
        Assert.False(attribute.RequireDisabled);
    }

    [Fact]
    public void Constructor_WithEnabledFalse_SetsProperty()
    {
        // Arrange
        var attribute = new FeatureRestrictedAttribute(_featureKey, false);

        // Assert
        Assert.False(attribute.RequireEnabled);
        Assert.True(attribute.RequireDisabled);
    }

    [Fact]
    public void Constructor_ThrowsWhenArgumentsAreInvalid()
    {
        // Arrange & Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new FeatureRestrictedAttribute(null!));
        _ = Assert.Throws<ArgumentException>(() => new FeatureRestrictedAttribute(string.Empty));
    }

    [Fact]
    public void RequireDisabled_BehavesAsExpected()
    {
        var attribute = new FeatureRestrictedAttribute(_featureKey) { RequireDisabled = true };

        Assert.True(attribute.RequireDisabled);
        Assert.False(attribute.RequireEnabled);

        attribute.RequireDisabled = false;

        Assert.False(attribute.RequireDisabled);
        Assert.True(attribute.RequireEnabled);
    }

    [Fact]
    public void RequireEnabled_BehavesAsExpected()
    {
        // Arrange
        var attribute = new FeatureRestrictedAttribute(_featureKey) { RequireEnabled = true };

        // Assert
        Assert.False(attribute.RequireDisabled);
        Assert.True(attribute.RequireEnabled);

        // Act
        attribute.RequireEnabled = false;

        // Assert
        Assert.True(attribute.RequireDisabled);
        Assert.False(attribute.RequireEnabled);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task OnActionExecutionAsync_PerformsTheExpectedAction_Async(
        bool featureEnabled, bool requireEnabled)
    {
        // Arrange
        var attribute = new FeatureRestrictedAttribute(_featureKey, requireEnabled);
        _ = _mockFeature.Setup(f => f.Enabled).Returns(featureEnabled);

        var mockNext = new Mock<ActionExecutionDelegate>(MockBehavior.Strict);
        _ = mockNext.Setup(n => n.Invoke()).ReturnsAsync(() => null!);

        // Act
        await attribute.OnActionExecutionAsync(_mockActionExecutingContext, mockNext.Object);

        // Assert
        if (featureEnabled == requireEnabled)
        {
            mockNext.Verify(v => v.Invoke(), Times.Once);
        }
        else
        {
            mockNext.Verify(v => v.Invoke(), Times.Never);

            Assert.NotNull(_mockActionExecutingContext.Result);
            _ = Assert.IsType<NotFoundResult>(_mockActionExecutingContext.Result);
            Assert.Equal(StatusCodes.Status404NotFound,
                ((StatusCodeResult) _mockActionExecutingContext.Result).StatusCode);
        }
    }

    [Fact]
    public async Task OnActionExecutionAsync_WithCustomHandler_CallsCustomHandler_Async()
    {
        // Arrange
        var attribute = new FeatureRestrictedAttribute(_featureKey);
        _ = _mockFeature.Setup(f => f.Enabled).Returns(false);

        var mockNext = new Mock<ActionExecutionDelegate>(MockBehavior.Strict);
        _ = mockNext.Setup(m => m.Invoke()).ReturnsAsync(() => null!);

        var mockContentResult = new ContentResult { Content = "Custom handler" };
        var mockHandler = new RestrictedFeatureActionHandlerAsyncDelegate((_, _) => ValueTask.FromResult<IActionResult>(mockContentResult));

        _ = _mockServiceProvider
            .Setup(
                m => m.GetService(typeof(RestrictedFeatureActionHandlerAsyncDelegate))
            )
            .Returns(() => mockHandler);

        // Act
        await attribute.OnActionExecutionAsync(_mockActionExecutingContext, mockNext.Object);

        // Assert
        mockNext.Verify(v => v.Invoke(), Times.Never);
        Assert.Equal(_mockActionExecutingContext.Result, mockContentResult);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task OnPageHandlerExecutionAsync_PerformsTheExpectedAction_Async(
        bool featureEnabled, bool requireEnabled)
    {
        // Arrange
        var attribute = new FeatureRestrictedAttribute(_featureKey, requireEnabled);
        _ = _mockFeature.Setup(f => f.Enabled).Returns(featureEnabled);

        var mockNext = new Mock<PageHandlerExecutionDelegate>(MockBehavior.Strict);
        _ = mockNext.Setup(n => n.Invoke()).ReturnsAsync(() => null!);

        // Act
        await attribute.OnPageHandlerExecutionAsync(_mockPageHandlerExecutingContext, mockNext.Object);

        // Assert
        if (featureEnabled == requireEnabled)
        {
            mockNext.Verify(v => v.Invoke(), Times.Once);
        }
        else
        {
            mockNext.Verify(v => v.Invoke(), Times.Never);

            Assert.NotNull(_mockPageHandlerExecutingContext.Result);
            _ = Assert.IsType<NotFoundResult>(_mockPageHandlerExecutingContext.Result);
            Assert.Equal(StatusCodes.Status404NotFound,
                ((StatusCodeResult) _mockPageHandlerExecutingContext.Result).StatusCode);
        }
    }

    [Fact]
    public async Task OnPageHandlerExecutionAsync_WithCustomHandler_CallsCustomHandler_Async()
    {
        // Arrange
        var attribute = new FeatureRestrictedAttribute(_featureKey);
        _ = _mockFeature.Setup(f => f.Enabled).Returns(false);

        var mockNext = new Mock<PageHandlerExecutionDelegate>(MockBehavior.Strict);
        _ = mockNext.Setup(n => n.Invoke()).ReturnsAsync(() => null!);

        var mockContentResult = new ContentResult { Content = "Custom handler" };
        var mockHandler = new RestrictedFeatureActionHandlerAsyncDelegate((_, _) => ValueTask.FromResult<IActionResult>(mockContentResult));

        _ = _mockServiceProvider
            .Setup(
                m => m.GetService(typeof(RestrictedFeatureActionHandlerAsyncDelegate))
            )
            .Returns(() => mockHandler);

        // Act
        await attribute.OnPageHandlerExecutionAsync(_mockPageHandlerExecutingContext, mockNext.Object);

        // Assert
        mockNext.Verify(v => v.Invoke(), Times.Never);
        Assert.Equal(mockContentResult, _mockPageHandlerExecutingContext.Result);
    }

    [Fact]
    public async Task OnPageHandlerSelectionAsync_AlwaysCompletesTask_Async()
    {
        // Arrange
        var attribute = new FeatureRestrictedAttribute(_featureKey);

        // Act
        await attribute.OnPageHandlerSelectionAsync(_mockPageHandlerSelectedContext);
    }
}
