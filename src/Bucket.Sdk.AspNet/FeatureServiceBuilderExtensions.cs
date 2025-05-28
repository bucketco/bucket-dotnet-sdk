namespace Bucket.Sdk;

/// <summary>
///     Extension methods for <see cref="IFeatureServiceBuilder" /> to add feature management
///     integration for ASP.NET application building.
/// </summary>
[PublicAPI]
public static class FeatureServiceBuilderExtensions
{
    /// <summary>
    ///     Specifies a feature resolver to use with <see cref="IFeatureClient" />.
    /// </summary>
    /// <param name="builder">The feature service builder.</param>
    /// <param name="resolver">The context resolver.</param>
    /// <returns>The current <see cref="IFeatureServiceBuilder" /> for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when either <paramref name="builder" /> or <paramref name="resolver" />
    ///     are <see langword="null"/>.
    /// </exception>
    public static IFeatureServiceBuilder UseContextResolver(
        this IFeatureServiceBuilder builder,
        ResolveEvaluationContextAsyncDelegate resolver)
    {
        // Validate input parameters
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(resolver);

        // Register the context resolver as a singleton in the DI container
        // This will be used by the feature client to resolve contexts during feature evaluation
        _ = builder.Services.AddSingleton(_ => resolver);

        return builder;
    }

    /// <summary>
    ///     Registers a handler for when a feature check fails before an action or handler is executed.
    /// </summary>
    /// <remarks>
    ///     This handler allows for custom behavior when a feature is disabled, other than returning a 404 response.
    /// </remarks>
    /// <param name="builder">The feature service builder.</param>
    /// <param name="handler">The disabled feature handler.</param>
    /// <returns>The current <see cref="IFeatureServiceBuilder" /> for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when either <paramref name="builder" /> or <paramref name="handler" />
    ///     are <see langword="null"/>.
    /// </exception>
    public static IFeatureServiceBuilder UseRestrictedFeatureHandler(
        this IFeatureServiceBuilder builder,
        RestrictedFeatureActionHandlerAsyncDelegate handler)
    {
        // Validate input parameters
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(handler);

        // Register the handler as a singleton in the DI container
        _ = builder.Services.AddSingleton(_ => handler);

        return builder;
    }

    /// <summary>
    ///     Registers a handler for when a feature check fails before an endpoint is executed.
    /// </summary>
    /// <remarks>
    ///     This handler allows for custom behavior when a feature is disabled, other than returning a 404 response.
    /// </remarks>
    /// <param name="builder">The feature service builder.</param>
    /// <param name="handler">The disabled feature handler.</param>
    /// <returns>The current <see cref="IFeatureServiceBuilder" /> for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when either <paramref name="builder" /> or <paramref name="handler" />
    ///     are <see langword="null"/>.
    /// </exception>
    public static IFeatureServiceBuilder UseRestrictedFeatureHandler(
        this IFeatureServiceBuilder builder,
        RestrictedFeatureEndpointHandlerAsyncDelegate handler)
    {
        // Validate input parameters
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(handler);

        // Register the handler as a singleton in the DI container
        _ = builder.Services.AddSingleton(_ => handler);

        return builder;
    }
}
