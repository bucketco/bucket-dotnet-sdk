namespace Bucket.Sdk.Tests.AspNet;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

public sealed class ApplicationBuilderExtensionsTests
{
    private const string _featureKey = "test-feature";

    private readonly Mock<IApplicationBuilder> _mockAppBuilder;
    private readonly Mock<IApplicationBuilder> _mockBranchBuilder;
    private readonly Mock<IFeature> _mockFeature;
    private readonly Mock<IServiceProvider> _mockServiceProvider;

    public ApplicationBuilderExtensionsTests()
    {
        _mockAppBuilder = new Mock<IApplicationBuilder>(MockBehavior.Strict);
        _mockBranchBuilder = new Mock<IApplicationBuilder>(MockBehavior.Strict);
        _mockFeature = new Mock<IFeature>(MockBehavior.Strict);
        _mockServiceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);

        var branchDelegate = new RequestDelegate(_ => Task.CompletedTask);

        _ = _mockBranchBuilder.Setup(m => m.Build())
            .Returns(branchDelegate);
        _ = _mockBranchBuilder.Setup(m => m.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()))
            .Returns(_mockBranchBuilder.Object);
        _ = _mockAppBuilder.Setup(m => m.New())
            .Returns(_mockBranchBuilder.Object);
        _ = _mockAppBuilder.Setup(m => m.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()))
            .Returns(_mockAppBuilder.Object);
        _ = _mockAppBuilder.Setup(m => m.ApplicationServices)
            .Returns(_mockServiceProvider.Object);
    }

    private static void NoOpConfigureBranch(IApplicationBuilder builder)
    {
    }

    [Fact]
    public void UseWhenFeature_ThrowsWhenArgumentsAreInvalid()
    {
        _ = Assert.Throws<ArgumentNullException>(() =>
            ((IApplicationBuilder) null!).UseWhenFeature(_featureKey, NoOpConfigureBranch));
        _ = Assert.Throws<ArgumentNullException>(() =>
            _mockAppBuilder.Object.UseWhenFeature(null!, NoOpConfigureBranch));
        _ = Assert.Throws<ArgumentException>(() =>
            _mockAppBuilder.Object.UseWhenFeature(string.Empty, NoOpConfigureBranch));
        _ = Assert.Throws<ArgumentNullException>(() =>
            _mockAppBuilder.Object.UseWhenFeature(_featureKey, null!));
    }

    [Fact]
    public void UseWhenFeature_CallsConfigureOnBranch()
    {
        // Mock service provider to simulate registered BucketFeatureServiceGuard
        _ = _mockServiceProvider.Setup(sp => sp.GetService(typeof(BucketFeatureServiceGuard)))
            .Returns(new BucketFeatureServiceGuard());

        _ = _mockFeature.Setup(f => f.Enabled).Returns(true);

        var configureMock = new Mock<Action<IApplicationBuilder>>(MockBehavior.Strict);
        _ = configureMock.Setup(
            m => m.Invoke(It.IsAny<IApplicationBuilder>())
        );

        var result = _mockAppBuilder.Object.UseWhenFeature(_featureKey, configureMock.Object);

        Assert.Equal(_mockAppBuilder.Object, result);

        configureMock.Verify(v => v.Invoke(_mockBranchBuilder.Object), Times.Once);

        _mockAppBuilder.Verify(a => a.New(), Times.Once);
        _mockAppBuilder.Verify(a => a.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()), Times.Once);
        _mockServiceProvider.Verify(sp => sp.GetService(typeof(BucketFeatureServiceGuard)), Times.Once);
    }

    [Fact]
    public void UseWhenFeature_ThrowsInvalidOperationException_WhenFeatureServiceNotRegistered()
    {
        // Arrange
        _ = _mockServiceProvider.Setup(sp => sp.GetService(typeof(BucketFeatureServiceGuard)))
            .Returns(() => null);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            _mockAppBuilder.Object.UseWhenFeature(_featureKey, NoOpConfigureBranch));

        Assert.Equal(
            $"Bucket feature service not found. Add the required service by calling '{nameof(IServiceCollection)}.{nameof(ServiceCollectionExtensions.AddBucketFeatures)}' in the application startup code.",
            exception.Message);

        _mockServiceProvider.Verify(sp => sp.GetService(typeof(BucketFeatureServiceGuard)), Times.Once);
    }

    [Fact]
    public void UseWhenNotFeature_ThrowsWhenArgumentsAreInvalid()
    {
        _ = Assert.Throws<ArgumentNullException>(() =>
            ((IApplicationBuilder) null!).UseWhenNotFeature(_featureKey, NoOpConfigureBranch));
        _ = Assert.Throws<ArgumentNullException>(() =>
            _mockAppBuilder.Object.UseWhenNotFeature(null!, NoOpConfigureBranch));
        _ = Assert.Throws<ArgumentException>(() =>
            _mockAppBuilder.Object.UseWhenNotFeature(string.Empty, NoOpConfigureBranch));
        _ = Assert.Throws<ArgumentNullException>(() =>
            _mockAppBuilder.Object.UseWhenNotFeature(_featureKey, null!));
    }

    [Fact]
    public void UseWhenNotFeature_CallsConfigureOnBranch()
    {
        // Arrange
        _ = _mockServiceProvider.Setup(sp => sp.GetService(typeof(BucketFeatureServiceGuard)))
            .Returns(new BucketFeatureServiceGuard());

        _ = _mockFeature.Setup(f => f.Enabled).Returns(false);

        var configureMock = new Mock<Action<IApplicationBuilder>>(MockBehavior.Strict);
        _ = configureMock.Setup(
            m => m.Invoke(It.IsAny<IApplicationBuilder>())
        );

        var result = _mockAppBuilder.Object.UseWhenNotFeature(_featureKey, configureMock.Object);

        Assert.Equal(_mockAppBuilder.Object, result);

        configureMock.Verify(v => v.Invoke(_mockBranchBuilder.Object), Times.Once);

        _mockAppBuilder.Verify(v => v.New(), Times.Once);
        _mockAppBuilder.Verify(v => v.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()), Times.Once);
        _mockServiceProvider.Verify(sp => sp.GetService(typeof(BucketFeatureServiceGuard)), Times.Once);
    }

    [Fact]
    public void UseWhenNotFeature_ThrowsInvalidOperationException_WhenFeatureServiceNotRegistered()
    {
        // Mock service provider to simulate unregistered BucketFeatureServiceGuard
        _ = _mockServiceProvider.Setup(sp => sp.GetService(typeof(BucketFeatureServiceGuard)))
            .Returns(() => null);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            _mockAppBuilder.Object.UseWhenNotFeature(_featureKey, NoOpConfigureBranch));

        Assert.Equal(
            $"Bucket feature service not found. Add the required service by calling '{nameof(IServiceCollection)}.{nameof(ServiceCollectionExtensions.AddBucketFeatures)}' in the application startup code.",
            exception.Message);

        _mockServiceProvider.Verify(sp => sp.GetService(typeof(BucketFeatureServiceGuard)), Times.Once);
    }

    [Fact]
    public void UseMiddlewareWhenFeature_ThrowsWhenArgumentsAreInvalid()
    {
        _ = Assert.Throws<ArgumentNullException>(() =>
            ((IApplicationBuilder) null!).UseMiddlewareWhenFeature<MockMiddleware>(_featureKey));
        _ = Assert.Throws<ArgumentNullException>(() =>
            _mockAppBuilder.Object.UseMiddlewareWhenFeature<MockMiddleware>(null!));
        _ = Assert.Throws<ArgumentException>(() =>
            _mockAppBuilder.Object.UseMiddlewareWhenFeature<MockMiddleware>(string.Empty));
    }

    [Fact]
    public void UseMiddlewareWhenFeature_SetsUpMiddleware()
    {
        // Arrange
        _ = _mockServiceProvider.Setup(sp => sp.GetService(typeof(BucketFeatureServiceGuard)))
            .Returns(new BucketFeatureServiceGuard());
        _ = _mockFeature.Setup(m => m.Enabled).Returns(true);

        // Act
        var result = _mockAppBuilder.Object.UseMiddlewareWhenFeature<MockMiddleware>(_featureKey);

        // Assert
        Assert.Equal(_mockAppBuilder.Object, result);

        _mockAppBuilder.Verify(a => a.New(), Times.Once);
        _mockAppBuilder.Verify(a => a.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()), Times.Once);
        _mockServiceProvider.Verify(sp => sp.GetService(typeof(BucketFeatureServiceGuard)), Times.Once);
    }

    [Fact]
    public void UseMiddlewareWhenFeature_ThrowsInvalidOperationException_WhenFeatureServiceNotRegistered()
    {
        // Arrange
        _ = _mockServiceProvider.Setup(sp => sp.GetService(typeof(BucketFeatureServiceGuard)))
            .Returns(() => null);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            _mockAppBuilder.Object.UseMiddlewareWhenFeature<MockMiddleware>(_featureKey));

        Assert.Equal(
            $"Bucket feature service not found. Add the required service by calling '{nameof(IServiceCollection)}.{nameof(ServiceCollectionExtensions.AddBucketFeatures)}' in the application startup code.",
            exception.Message);

        _mockServiceProvider.Verify(sp => sp.GetService(typeof(BucketFeatureServiceGuard)), Times.Once);
    }

    [Fact]
    public void UseMiddlewareWhenNotFeature_ThrowsWhenArgumentsAreInvalid()
    {
        // Arrange, Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            ((IApplicationBuilder) null!).UseMiddlewareWhenNotFeature<MockMiddleware>(_featureKey));
        _ = Assert.Throws<ArgumentNullException>(() =>
            _mockAppBuilder.Object.UseMiddlewareWhenNotFeature<MockMiddleware>(null!));
        _ = Assert.Throws<ArgumentException>(() =>
            _mockAppBuilder.Object.UseMiddlewareWhenNotFeature<MockMiddleware>(string.Empty));
    }

    [Fact]
    public void UseMiddlewareWhenNotFeature_SetsUpMiddleware()
    {
        // Arrange
        _ = _mockServiceProvider.Setup(sp => sp.GetService(typeof(BucketFeatureServiceGuard)))
            .Returns(new BucketFeatureServiceGuard());
        _ = _mockFeature.Setup(m => m.Enabled).Returns(false);

        // Act
        var result = _mockAppBuilder.Object.UseMiddlewareWhenNotFeature<MockMiddleware>(_featureKey);

        // Assert
        Assert.Equal(_mockAppBuilder.Object, result);

        _mockAppBuilder.Verify(v => v.New(), Times.Once);
        _mockAppBuilder.Verify(v => v.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()), Times.Once);
        _mockServiceProvider.Verify(sp => sp.GetService(typeof(BucketFeatureServiceGuard)), Times.Once);
    }

    [Fact]
    public void UseMiddlewareWhenNotFeature_ThrowsInvalidOperationException_WhenFeatureServiceNotRegistered()
    {
        // Arrange
        _ = _mockServiceProvider.Setup(sp => sp.GetService(typeof(BucketFeatureServiceGuard)))
            .Returns(() => null);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            _mockAppBuilder.Object.UseMiddlewareWhenNotFeature<MockMiddleware>(_featureKey));

        Assert.Equal(
            $"Bucket feature service not found. Add the required service by calling '{nameof(IServiceCollection)}.{nameof(ServiceCollectionExtensions.AddBucketFeatures)}' in the application startup code.",
            exception.Message);

        _mockServiceProvider.Verify(sp => sp.GetService(typeof(BucketFeatureServiceGuard)), Times.Once);
    }

    private sealed class MockMiddleware(RequestDelegate next)
    {
        [UsedImplicitly]
        public Task InvokeAsync(HttpContext context) => next(context);
    }
}
