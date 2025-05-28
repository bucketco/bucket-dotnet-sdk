namespace Bucket.Sdk.Tests.OpenFeature;

using global::OpenFeature;
using global::OpenFeature.DependencyInjection;
using global::OpenFeature.Model;

using Microsoft.Extensions.Logging.Abstractions;

public sealed class OpenFeatureBuilderExtensionsTests
{
    private readonly ServiceCollection _mockServiceCollection;

    public OpenFeatureBuilderExtensionsTests()
    {
        var mockFeatureClient = new Mock<Sdk.IFeatureClient>(MockBehavior.Strict);
        _mockServiceCollection = [];

        _ = _mockServiceCollection
            .AddSingleton(mockFeatureClient.Object);
    }

    [Fact]
    public void AddBucketFeaturesProvider_WorksWhenILoggerFactoryNotAvailable()
    {
        // Arrange
        var builder = new OpenFeatureBuilder(_mockServiceCollection);
        _ = builder.AddBucketFeaturesProvider();
        var serviceProvider = _mockServiceCollection.BuildServiceProvider();

        // Act
        var provider = serviceProvider.GetService<FeatureProvider>();

        // Assert
        _ = Assert.IsType<BucketOpenFeatureProvider>(provider);
    }

    [Fact]
    public void AddBucketFeaturesProvider_AddsProviderCorrectly()
    {
        // Arrange
        var builder = new OpenFeatureBuilder(_mockServiceCollection);
        var result = builder.AddBucketFeaturesProvider();
        var mockLoggerFactory = new Mock<ILoggerFactory>(MockBehavior.Strict);
        _ = mockLoggerFactory.Setup(
            x => x.CreateLogger(It.IsAny<string>())
        ).Returns(NullLogger.Instance);
        _ = _mockServiceCollection.AddSingleton(mockLoggerFactory.Object);

        var serviceProvider = _mockServiceCollection.BuildServiceProvider();

        // Act
        var provider = serviceProvider.GetService<FeatureProvider>();

        // Assert
        mockLoggerFactory.Verify(v => v.CreateLogger("Bucket.Sdk.BucketOpenFeatureProvider"), Times.Once);

        Assert.Same(builder, result);
        _ = Assert.IsType<BucketOpenFeatureProvider>(provider);
    }

    [Fact]
    public void AddBucketFeaturesProvider_WithCustomTranslator_AddsProviderWithTranslator()
    {
        // Arrange
        var customTranslator = new Mock<EvaluationContextTranslatorDelegate>(MockBehavior.Strict);
        _ = customTranslator
            .Setup(x => x(It.IsAny<EvaluationContext>()))
            .Returns([]);

        var builder = new OpenFeatureBuilder(_mockServiceCollection);
        _ = builder.AddBucketFeaturesProvider(customTranslator.Object);
        var serviceProvider = _mockServiceCollection.BuildServiceProvider();

        // Act
        var provider = serviceProvider.GetRequiredService<FeatureProvider>();
        var context = EvaluationContext.Empty;

        // Assert
        provider.Track("test", context);
        customTranslator.Verify(v => v.Invoke(context));
    }

    [Fact]
    public void AddBucketFeaturesProvider_ThrowsWhenArgumentsAreInvalid() =>
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ((OpenFeatureBuilder) null!).AddBucketFeaturesProvider());
}
