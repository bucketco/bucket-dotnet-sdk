namespace Bucket.Sdk.Tests.Conformance;

using PublicApiGenerator;

public sealed class ConformanceTests
{
    private static readonly ApiGeneratorOptions _options = new()
    {
        IncludeAssemblyAttributes = false,
        TreatRecordsAsClasses = false,
        ExcludeAttributes =
        [
            "PublicAPIAttribute",
        ]
    };

    [Fact]
    public async Task BucketSdk_PublicApi_Async()
    {
        var definition = typeof(FeatureClient).Assembly.GeneratePublicApi(_options);
        _ = await Verify(definition).UseFileName("Bucket.Sdk");
    }

    [Fact]
    public async Task BucketSdkAspNet_PublicApi_Async()
    {
        var definition = typeof(ApplicationBuilderExtensions).Assembly.GeneratePublicApi(_options);
        _ = await Verify(definition).UseFileName("Bucket.Sdk.AspNet");
    }

    [Fact]
    public async Task BucketSdkOpenFeature_PublicApi_Async()
    {
        var definition = typeof(BucketOpenFeatureProvider).Assembly.GeneratePublicApi(_options);
        _ = await Verify(definition).UseFileName("Bucket.Sdk.OpenFeature");
    }
}
