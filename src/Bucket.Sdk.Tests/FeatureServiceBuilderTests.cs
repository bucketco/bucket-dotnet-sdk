namespace Bucket.Sdk.Tests;

public sealed class FeatureServiceBuilderTests
{
    private readonly IFeatureServiceBuilder _featureServiceBuilder;

    private readonly Mock<IServiceCollection> _mockServiceCollection;

    public FeatureServiceBuilderTests()
    {
        _mockServiceCollection = new Mock<IServiceCollection>(MockBehavior.Strict);

        // Setup to capture service descriptors being added
        _ = _mockServiceCollection.Setup(s => s.Add(It.IsAny<ServiceDescriptor>()));

        // Create a test implementation with the mocked service collection
        _featureServiceBuilder = new TestFeatureServiceBuilder(_mockServiceCollection.Object);
    }

    [Fact]
    public void AddLocalFeatures_Delegate_ThrowsWhenArgumentsAreInvalid()
    {
        _ = Assert.Throws<ArgumentNullException>(() =>
             _featureServiceBuilder.AddLocalFeatures((ResolveLocalFeaturesAsyncDelegate) null!));
    }

    [Fact]
    public void AddLocalFeatures_Delegate_RegistersDelegateAsService()
    {
        // Arrange
        ResolveLocalFeaturesAsyncDelegate resolver = (_, _) =>
            ValueTask.FromResult(new[] { new EvaluatedFeature("test-feature", true) }.AsEnumerable());

        ServiceDescriptor? addedDescriptor = null;
        _ = _mockServiceCollection.Setup(s => s.Add(It.IsAny<ServiceDescriptor>()))
            .Callback<ServiceDescriptor>(sd => { addedDescriptor = sd; });

        // Act
        var result = _featureServiceBuilder.AddLocalFeatures(resolver);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(addedDescriptor);
        Assert.Equal(typeof(ResolveLocalFeaturesAsyncDelegate), addedDescriptor.ServiceType);
        Assert.Equal(resolver, addedDescriptor.ImplementationInstance);
        Assert.Equal(ServiceLifetime.Singleton, addedDescriptor.Lifetime);
        Assert.Same(_featureServiceBuilder, result);
    }

    [Fact]
    public void AddLocalFeatures_Features_ThrowsWhenArgumentsAreInvalid()
    {
        // Arrange
        var feature = new EvaluatedFeature("test-feature", true);

        _ = Assert.Throws<ArgumentNullException>(() =>
            _featureServiceBuilder.AddLocalFeatures((ResolveLocalFeaturesAsyncDelegate) null!));
        _ = Assert.Throws<ArgumentNullException>(() =>
            _featureServiceBuilder.AddLocalFeatures((EvaluatedFeature[]) null!));
        _ = Assert.Throws<ArgumentNullException>(() =>
            _featureServiceBuilder.AddLocalFeatures((EvaluatedFeature) null!));

        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() =>
            _featureServiceBuilder.AddLocalFeatures(feature, feature));
    }

    [Fact]
    public async Task AddLocalFeatures_Features_RegistersResolverDelegate_Async()
    {
        // Arrange
        var feature1 = new EvaluatedFeature("test-feature-1", true);
        var feature2 = new EvaluatedFeature("test-feature-2", false);

        ServiceDescriptor? addedDescriptor = null;
        _ = _mockServiceCollection.Setup(s => s.Add(It.IsAny<ServiceDescriptor>()))
            .Callback<ServiceDescriptor>(sd => { addedDescriptor = sd; });

        // Act
        var result = _featureServiceBuilder.AddLocalFeatures(feature1, feature2);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(addedDescriptor);
        Assert.Equal(typeof(ResolveLocalFeaturesAsyncDelegate), addedDescriptor.ServiceType);
        _ = Assert.IsType<ResolveLocalFeaturesAsyncDelegate>(addedDescriptor.ImplementationInstance);
        Assert.Equal(ServiceLifetime.Singleton, addedDescriptor.Lifetime);
        Assert.Same(_featureServiceBuilder, result);

        // Verify the resolver returns the expected features
        var resolver = (ResolveLocalFeaturesAsyncDelegate) addedDescriptor.ImplementationInstance;
        var features = await resolver([], CancellationToken.None);

        Assert.Equivalent(new[] { feature1, feature2 }, features.ToImmutableArray());
    }

    [Fact]
    public async Task AddLocalFeatures_SingleFeature_RegistersResolverDelegate_Async()
    {
        // Arrange
        var feature = new EvaluatedFeature("test-feature", true);

        ServiceDescriptor? addedDescriptor = null;
        _ = _mockServiceCollection.Setup(s => s.Add(It.IsAny<ServiceDescriptor>()))
            .Callback<ServiceDescriptor>(sd => { addedDescriptor = sd; });

        // Act
        var result = _featureServiceBuilder.AddLocalFeatures(feature);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(addedDescriptor);
        Assert.Equal(typeof(ResolveLocalFeaturesAsyncDelegate), addedDescriptor.ServiceType);
        _ = Assert.IsType<ResolveLocalFeaturesAsyncDelegate>(addedDescriptor.ImplementationInstance);
        Assert.Equal(ServiceLifetime.Singleton, addedDescriptor.Lifetime);
        Assert.Same(_featureServiceBuilder, result);

        // Verify the resolver returns the expected feature
        var resolver = (ResolveLocalFeaturesAsyncDelegate) addedDescriptor.ImplementationInstance;
        var features = await resolver([], CancellationToken.None);

        Assert.Equal([feature], features);
    }

    private class TestFeatureServiceBuilder(IServiceCollection services): IFeatureServiceBuilder
    {
        public IServiceCollection Services { get; } = services;
    }
}
