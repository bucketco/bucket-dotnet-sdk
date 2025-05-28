namespace Bucket.Sdk;

/// <summary>
///     Extension methods for <see cref="IEndpointConventionBuilder" /> to add feature management
///     integration for ASP.NET application building.
/// </summary>
[PublicAPI]
public static class EndpointConventionBuilderExtensions
{
    /// <summary>
    ///     Adds a filter to the endpoint that will only activate during a request if the specified feature
    ///     restriction is satisfied.
    /// </summary>
    /// <param name="builder">The endpoint convention builder.</param>
    /// <param name="featureKey">The feature key to check for.</param>
    /// <param name="enabled">Whether the feature should be enabled or disabled.</param>
    /// <returns>The current <see cref="IEndpointConventionBuilder" /> for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder" /> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="featureKey" /> is <see langword="null"/> or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the Bucket feature service is not registered.</exception>
    public static IEndpointConventionBuilder WithFeatureRestriction(
        this IEndpointConventionBuilder builder,
        string featureKey,
        bool enabled = true)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(featureKey);

        return builder.AddEndpointFilter(new FeatureRestrictedEndpointFilter(featureKey, enabled));
    }
}
