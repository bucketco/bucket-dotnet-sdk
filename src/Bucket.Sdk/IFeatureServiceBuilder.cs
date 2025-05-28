namespace Bucket.Sdk;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
///     The <see cref="IFeatureServiceBuilder" /> interface is used to configure the feature management integration.
/// </summary>
[PublicAPI]
public interface IFeatureServiceBuilder
{
    /// <summary>
    ///     The service collection to add services to.
    /// </summary>
    IServiceCollection Services
    {
        get;
    }

    /// <summary>
    ///     Specifies a feature resolver to use with <see cref="IFeatureClient" />.
    /// </summary>
    /// <param name="localFeaturesResolver">The local features' resolver.</param>
    /// <returns>The current <see cref="IFeatureServiceBuilder" /> for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="localFeaturesResolver" /> is <see langword="null"/>.</exception>
    /// <remarks>
    ///     Multiple resolvers can be added. They will be invoked in the order they were added.
    /// </remarks>
    IFeatureServiceBuilder AddLocalFeatures(ResolveLocalFeaturesAsyncDelegate localFeaturesResolver)
    {
        ArgumentNullException.ThrowIfNull(localFeaturesResolver);

        _ = Services.AddSingleton(localFeaturesResolver);

        return this;
    }

    /// <summary>
    ///     Specifies a list of "local" features for <see cref="IFeatureClient" />.
    /// </summary>
    /// <param name="features">Additional features to add.</param>
    /// <returns>The current <see cref="IFeatureServiceBuilder" /> for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="features" /> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when duplicate features are supplied.</exception>
    /// <remarks>
    ///     This method is a convenience wrapper around <see cref="AddLocalFeatures(ResolveLocalFeaturesAsyncDelegate)" />.
    /// </remarks>
    IFeatureServiceBuilder AddLocalFeatures(params EvaluatedFeature[] features)
    {
        ArgumentNullException.ThrowIfNull(features);
        foreach (var feature in features)
        {
            ArgumentNullException.ThrowIfNull(feature);
        }

        var map = features.ToImmutableDictionary(k => k.Key, v => v);
        if (map.Count != features.Length)
        {
            throw new ArgumentException("Duplicate features supplied.", nameof(features));
        }

        features = [.. map.Values];
        return AddLocalFeatures((_, _) => ValueTask.FromResult(features.AsEnumerable()));
    }
}
