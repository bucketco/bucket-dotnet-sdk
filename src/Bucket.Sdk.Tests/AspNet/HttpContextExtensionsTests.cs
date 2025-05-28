namespace Bucket.Sdk.Tests.AspNet;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

public sealed class HttpContextExtensionsTests
{
    private const string _featureKey = "test-feature";
    private const TrackingStrategy _trackingStrategy = TrackingStrategy.Active;
    private static readonly Context _context = [];
    private readonly Dictionary<object, object?> _contextItems;
    private readonly Mock<IFeature> _mockFeature;
    private readonly Mock<IFeatureClient> _mockFeatureClient;
    private readonly Mock<IFeature<string>> _mockFeatureGeneric;

    private readonly Mock<HttpContext> _mockHttpContext;
    private readonly Mock<IServiceProvider> _mockServiceProvider;

    public HttpContextExtensionsTests()
    {
        _contextItems = [];
        _mockHttpContext = new Mock<HttpContext>(MockBehavior.Strict);
        _mockServiceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
        _mockFeatureClient = new Mock<IFeatureClient>(MockBehavior.Strict);
        _mockFeature = new Mock<IFeature>(MockBehavior.Strict);
        _mockFeatureGeneric = new Mock<IFeature<string>>(MockBehavior.Strict);

        _ = _mockFeatureClient.Setup(m => m.GetFeatureAsync(
                It.IsAny<string>(), It.IsAny<Context>(), It.IsAny<TrackingStrategy>()
            )
        ).ReturnsAsync(_mockFeature.Object);

        _ = _mockFeatureClient.Setup(m => m.GetFeatureAsync<string>(
                It.IsAny<string>(), It.IsAny<Context>(), It.IsAny<TrackingStrategy>()
            )
        ).ReturnsAsync(_mockFeatureGeneric.Object);

        _ = _mockServiceProvider.Setup(m => m.GetService(typeof(ResolveEvaluationContextAsyncDelegate))
        ).Returns(() => new ResolveEvaluationContextAsyncDelegate(MockContextResolver));

        _ = _mockHttpContext.Setup(c => c.Items).Returns(_contextItems);
        _ = _mockHttpContext.Setup(c => c.RequestServices).Returns(_mockServiceProvider.Object);
        _ = _mockHttpContext.Setup(m => m.Features.Get<IHttpActivityFeature>())
            .Returns(() => null);

        _ = _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(IFeatureClient)))
            .Returns(_mockFeatureClient.Object);

        _ = _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(BucketFeatureServiceGuard)))
            .Returns(new BucketFeatureServiceGuard());
    }

    private static ValueTask<(Context, TrackingStrategy)> MockContextResolver(HttpContext _) =>
        new((_context, _trackingStrategy));


    [Fact]
    public async Task GetFeatureAsync_ThrowsWhenArgumentsAreInvalid_Async()
    {
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            ((HttpContext) null!).GetFeatureAsync(_featureKey));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _mockHttpContext.Object.GetFeatureAsync(null!));
        _ = await Assert.ThrowsAsync<ArgumentException>(() =>
            _mockHttpContext.Object.GetFeatureAsync(string.Empty));
    }

    [Fact]
    public async Task GetFeatureAsync_CallsFeatureClientWithCorrectParameters_Async()
    {
        // Arrange & Act
        var result = await _mockHttpContext.Object.GetFeatureAsync(_featureKey);

        // Assert
        Assert.Equal(_mockFeature.Object, result);

        _mockFeatureClient.Verify(
            v => v.GetFeatureAsync(_featureKey, _context, _trackingStrategy),
            Times.Once
        );
    }

    [Fact]
    public async Task GetFeatureAsync_ThrowsWhenBucketFeatureServiceIsNotRegistered_Async()
    {
        // Arrange
        _ = _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(BucketFeatureServiceGuard)))
            .Returns(() => null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _mockHttpContext.Object.GetFeatureAsync(_featureKey));

        Assert.Equal(
            $"Bucket feature service not found. Add the required service by calling '{nameof(IServiceCollection)}.{nameof(ServiceCollectionExtensions.AddBucketFeatures)}' in the application startup code.",
            exception.Message
        );
    }

    [Fact]
    public async Task GetFeatureAsync_Generic_CallsFeatureClientWithCorrectParameters_Async()
    {
        // Arrange & Act
        var result = await _mockHttpContext.Object.GetFeatureAsync<string>(_featureKey);

        // Assert
        Assert.Equal(_mockFeatureGeneric.Object, result);

        _mockFeatureClient.Verify(
            v => v.GetFeatureAsync<string>(_featureKey, _context, _trackingStrategy),
            Times.Once
        );
    }

    [Fact]
    public async Task GetFeatureAsync_Generic_ThrowsWhenBucketFeatureServiceIsNotRegistered_Async()
    {
        // Arrange
        _ = _mockServiceProvider
            .Setup(
                sp => sp.GetService(typeof(BucketFeatureServiceGuard))
            )
            .Returns(() => null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _mockHttpContext.Object.GetFeatureAsync<string>(_featureKey));

        Assert.Equal(
            $"Bucket feature service not found. Add the required service by calling '{nameof(IServiceCollection)}.{nameof(ServiceCollectionExtensions.AddBucketFeatures)}' in the application startup code.",
            exception.Message
        );
    }

    [Fact]
    public async Task GetFeatureAsync_Generic_ThrowsWhenArgumentsAreInvalid_Async()
    {
        // Arrange & Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            ((HttpContext) null!).GetFeatureAsync<string>(_featureKey));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _mockHttpContext.Object.GetFeatureAsync<string>(null!));
        _ = await Assert.ThrowsAsync<ArgumentException>(() =>
            _mockHttpContext.Object.GetFeatureAsync<string>(string.Empty));
    }

    [Fact]
    public Task GetEvaluationContextAsync_ThrowsWhenArgumentsAreInvalid_Async() =>
        // Arrange & Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(() =>
            ((HttpContext) null!).GetEvaluationContextAsync().AsTask());

    [Fact]
    public async Task GetEvaluationContextAsync_ReturnsEmptyContext_WhenNoResolverInstalled_Async()
    {
        _ = _mockServiceProvider.Setup(
            m => m.GetService(typeof(ResolveEvaluationContextAsyncDelegate))
        ).Returns(() => null);

        var (context, trackingStrategy) = await _mockHttpContext.Object.GetEvaluationContextAsync();

        Assert.NotSame(_context, context);
        Assert.Equal(TrackingStrategy.Default, trackingStrategy);
        Assert.Null(context.User);
        Assert.Null(context.Company);
        Assert.Equal(0, context.Count);

        _mockServiceProvider.Verify(v => v.GetService(
            typeof(ResolveEvaluationContextAsyncDelegate)
        ), Times.Once);
    }

    [Fact]
    public async Task GetEvaluationContextAsync_CachesContext_Async()
    {
        // Arrange
        var result = await _mockHttpContext.Object.GetEvaluationContextAsync();

        // Assert
        Assert.Equal(_context, result.context);
        Assert.Equal(_trackingStrategy, result.trackingStrategy);

        // Act
        result = await _mockHttpContext.Object.GetEvaluationContextAsync();

        // Assert
        Assert.Equal(_context, result.context);
        Assert.Equal(_trackingStrategy, result.trackingStrategy);

        _mockServiceProvider.Verify(v => v.GetService(
            typeof(ResolveEvaluationContextAsyncDelegate)
        ), Times.Once);
    }

    [Fact]
    public async Task GetEvaluationContextAsync_ResolvesContextWhenNotCached_Async()
    {
        // Arrange & Act
        var (context, trackingStrategy) = await _mockHttpContext.Object.GetEvaluationContextAsync();

        // Assert
        Assert.Equal(_context, context);
        Assert.Equal(_trackingStrategy, trackingStrategy);
        Assert.Contains((_context, _trackingStrategy), _contextItems.Values);
    }

    [Fact]
    public async Task GetEvaluationContextAsync_ThrowsWhenResolvedContextIsNull_Async()
    {
        // Arrange
        static ValueTask<(Context, TrackingStrategy)> badResolver(HttpContext _) =>
            new((null!, _trackingStrategy));

        _ = _mockServiceProvider.Setup(m => m.GetService(typeof(ResolveEvaluationContextAsyncDelegate))
        ).Returns(() =>
            new ResolveEvaluationContextAsyncDelegate(
                (Func<HttpContext, ValueTask<(Context, TrackingStrategy)>>) badResolver));

        // Act & Assert
        _ = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _mockHttpContext.Object.GetEvaluationContextAsync().AsTask());
    }
}
