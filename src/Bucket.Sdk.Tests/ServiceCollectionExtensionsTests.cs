namespace Bucket.Sdk.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    private readonly Mock<ILogger<FeatureClient>> _mockLogger;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<IServiceCollection> _mockServiceCollection;

    public ServiceCollectionExtensionsTests()
    {
        _mockServiceCollection = new Mock<IServiceCollection>(MockBehavior.Strict);
        _mockLoggerFactory = new Mock<ILoggerFactory>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<FeatureClient>>(MockBehavior.Strict);

        _ = _mockLoggerFactory.Setup(lf => lf.CreateLogger(It.IsAny<string>()))
            .Returns(_mockLogger.Object);

        _ = _mockServiceCollection.Setup(s => s.Add(It.IsAny<ServiceDescriptor>()));
    }

    [Fact]
    public void AddBucketFeatures_WithoutConfiguration_ThrowsWhenArgumentsAreInvalid()
    {
        _ = Assert.Throws<ArgumentNullException>(() =>
            ((IServiceCollection) null!).AddBucketFeatures());
    }

    [Fact]
    public void AddBucketFeatures_WithConfiguration_ThrowsWhenArgumentsAreInvalid()
    {
        var config = new Configuration { SecretKey = "test-api-key" };

        _ = Assert.Throws<ArgumentNullException>(() =>
            ((IServiceCollection) null!).AddBucketFeatures(config));

        _ = Assert.Throws<ArgumentNullException>(() =>
            _mockServiceCollection.Object.AddBucketFeatures(null!));
    }

    [Fact]
    public void AddBucketFeatures_WithoutConfiguration_RegistersFeatureClient_AndFeatureServiceGuard()
    {
        // Arrange
        var serviceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
        var optionsMonitor = new Mock<IOptions<Configuration>>(MockBehavior.Strict);

        // Setup for service provider to return options
        var expectedConfig = new Configuration { SecretKey = "test-api-key" };
        _ = optionsMonitor.Setup(om => om.Value).Returns(expectedConfig);

        // Setup service provider to return options and logger factory
        _ = serviceProvider.Setup(sp => sp.GetService(typeof(IOptions<Configuration>)))
            .Returns(optionsMonitor.Object);
        _ = serviceProvider.Setup(sp => sp.GetService(typeof(ILoggerFactory)))
            .Returns(_mockLoggerFactory.Object);
        _ = serviceProvider.Setup(sp => sp.GetService(typeof(IEnumerable<ResolveLocalFeaturesAsyncDelegate>)))
            .Returns((IEnumerable<ResolveLocalFeaturesAsyncDelegate>) null!);

        _ = _mockServiceCollection.Setup(sc => sc.GetEnumerator())
            .Returns(new List<ServiceDescriptor>().GetEnumerator());

        // Verify service descriptor is added for IFeatureClient
        var addedDescriptors = new List<ServiceDescriptor>();
        _ = _mockServiceCollection.Setup(s => s.Add(It.IsAny<ServiceDescriptor>()))
            .Callback<ServiceDescriptor>(addedDescriptors.Add);

        // Act
        var result = _mockServiceCollection.Object.AddBucketFeatures();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, addedDescriptors.Count);

        Assert.Equal(typeof(IFeatureClient), addedDescriptors[0].ServiceType);
        Assert.Equal(ServiceLifetime.Singleton, addedDescriptors[0].Lifetime);

        Assert.Equal(typeof(BucketFeatureServiceGuard), addedDescriptors[1].ServiceType);
        Assert.Equal(ServiceLifetime.Singleton, addedDescriptors[1].Lifetime);

        // Verify service builder is returned
        Assert.NotNull(result);
        _ = Assert.IsAssignableFrom<IFeatureServiceBuilder>(result);
    }

    [Fact]
    public void AddBucketFeatures_WithConfiguration_RegistersFeatureClient_AndBucketFeatureServiceGuard()
    {
        // Arrange
        var config = new Configuration { SecretKey = "test-api-key" };

        // Verify service descriptor is added for IFeatureClient
        var addedDescriptors = new List<ServiceDescriptor>();
        _ = _mockServiceCollection.Setup(s => s.Add(It.IsAny<ServiceDescriptor>()))
            .Callback<ServiceDescriptor>(addedDescriptors.Add);

        // Act
        var result = _mockServiceCollection.Object.AddBucketFeatures(config);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, addedDescriptors.Count);

        Assert.Equal(typeof(IFeatureClient), addedDescriptors[0].ServiceType);
        Assert.Equal(ServiceLifetime.Singleton, addedDescriptors[0].Lifetime);

        Assert.Equal(typeof(BucketFeatureServiceGuard), addedDescriptors[1].ServiceType);
        Assert.Equal(ServiceLifetime.Singleton, addedDescriptors[1].Lifetime);

        // Verify service builder is returned
        Assert.NotNull(result);
        _ = Assert.IsAssignableFrom<IFeatureServiceBuilder>(result);
    }
}
