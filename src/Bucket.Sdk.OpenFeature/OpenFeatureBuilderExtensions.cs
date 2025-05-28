namespace Bucket.Sdk;

/// <summary>
///     Extension methods for <see cref="OpenFeatureBuilder" />.
/// </summary>
[PublicAPI]
public static class OpenFeatureBuilderExtensions
{
    /// <summary>
    ///     Add the Bucket features provider to the OpenFeature builder.
    /// </summary>
    /// <param name="builder">The OpenFeature builder.</param>
    /// <param name="evaluationContextTranslator">The evaluation context translator.</param>
    /// <returns>The OpenFeature builder.</returns>
    public static OpenFeatureBuilder AddBucketFeaturesProvider(
        this OpenFeatureBuilder builder,
        EvaluationContextTranslatorDelegate? evaluationContextTranslator = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Add the Bucket features provider
        return builder.AddProvider(sp =>
            new BucketOpenFeatureProvider(
                sp.GetRequiredService<IFeatureClient>(),
                sp.GetService<ILoggerFactory>()?.CreateLogger<BucketOpenFeatureProvider>(),
                evaluationContextTranslator
            )
        );
    }
}
