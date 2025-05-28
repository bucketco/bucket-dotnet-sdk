namespace Bucket.Sdk;

using Microsoft.Extensions.Options;

/// <summary>
///     Extension methods for <see cref="IServiceCollection" /> to add feature management
///     integration for any DI container.
/// </summary>
[PublicAPI]
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Adds Bucket feature management integration to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add the feature management integration to.</param>
    /// <returns>The <see cref="IFeatureServiceBuilder" /> to aid configuring the feature management integration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services" /> is <see langword="null"/>.</exception>
    public static IFeatureServiceBuilder AddBucketFeatures(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        _ = services.AddSingleton<IFeatureClient>(sp => new FeatureClient(
                sp.GetRequiredService<IOptions<Configuration>>().Value,
                sp.GetService<ILoggerFactory>()?.CreateLogger<FeatureClient>(),
                sp.GetService<IEnumerable<ResolveLocalFeaturesAsyncDelegate>>()
            )
        );

        _ = services.AddSingleton<BucketFeatureServiceGuard>();

        return new FeatureServiceBuilder(services);
    }

    /// <summary>
    ///     Adds Bucket feature management integration to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add the feature management integration to.</param>
    /// <param name="configuration">The configuration to use for the feature management integration.</param>
    /// <returns>The <see cref="IFeatureServiceBuilder" /> to aid configuring the feature management integration.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when either <paramref name="services" /> or
    ///     <paramref name="configuration" /> are <see langword="null"/>.
    /// </exception>
    public static IFeatureServiceBuilder AddBucketFeatures(
        this IServiceCollection services, Configuration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        _ = services.AddSingleton<IFeatureClient>(sp => new FeatureClient(
                configuration,
                sp.GetService<ILoggerFactory>()?.CreateLogger<FeatureClient>(),
                sp.GetService<IEnumerable<ResolveLocalFeaturesAsyncDelegate>>()
            )
        );

        _ = services.AddSingleton<BucketFeatureServiceGuard>();

        return new FeatureServiceBuilder(services);
    }

    private sealed record FeatureServiceBuilder(IServiceCollection Services): IFeatureServiceBuilder;
}
