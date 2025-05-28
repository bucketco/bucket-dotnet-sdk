namespace Bucket.Sdk;

/// <summary>
///     Extension methods for <see cref="HttpContext" /> to add feature management
///     integration for ASP.NET application building.
/// </summary>
[PublicAPI]
public static class HttpContextExtensions
{
    // Used as a key to store the resolved evaluation context in `HttpContext.Items`
    // This allows caching the context during a single request
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private static readonly object _resolvedEvaluationContextKey = new();

    // Default empty context to use when no context is available
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private static readonly Context _emptyContext = [];

    /// <summary>
    ///     Obtains a feature from the current request context.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="featureKey">The feature key to retrieve.</param>
    /// <returns>A task that completes with the feature.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context" /> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="featureKey" /> is <see langword="null"/> or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the Bucket feature service is not registered.</exception>
    public static async Task<IFeature> GetFeatureAsync(
        this HttpContext context,
        string featureKey)
    {
        // Validate input parameters
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrEmpty(featureKey);

        // Get or resolve the evaluation context for the current request
        var (resolvedContext, trackingStrategy) = await GetEvaluationContextAsync(context);

        // Ensure the Bucket feature service is registered
        BucketFeatureServiceGuard.EnsureRegistered(context.RequestServices);

        // Get the feature client from the DI container and evaluate the feature
        var featureClient = context.RequestServices.GetRequiredService<IFeatureClient>();
        return await featureClient.GetFeatureAsync(featureKey, resolvedContext, trackingStrategy);
    }

    /// <summary>
    ///     Obtains a feature from the current request context.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="featureKey">The feature key to retrieve.</param>
    /// <typeparam name="TPayload">The type of the feature payload.</typeparam>
    /// <returns>A task that completes with the feature.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context" /> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="featureKey" /> is <see langword="null"/> or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the Bucket feature service is not registered.</exception>
    public static async Task<IFeature<TPayload>> GetFeatureAsync<TPayload>(
        this HttpContext context,
        string featureKey)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrEmpty(featureKey);

        var (resolvedContext, trackingStrategy) = await GetEvaluationContextAsync(context);

        // Ensure the Bucket feature service is registered
        BucketFeatureServiceGuard.EnsureRegistered(context.RequestServices);

        var featureClient = context.RequestServices.GetRequiredService<IFeatureClient>();
        return await featureClient.GetFeatureAsync<TPayload>(featureKey, resolvedContext, trackingStrategy);
    }

    /// <summary>
    ///     Obtains the evaluation context for the current request. Requires the
    ///     <see cref="ResolveEvaluationContextAsyncDelegate" />
    ///     to be registered with the service provider.
    /// </summary>
    /// <param name="httpContext">The HTTP context to use.</param>
    /// <returns>A task that completes with the evaluation context.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="httpContext" /> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the evaluation context is <see langword="null"/>.</exception>
    public static async ValueTask<(Context context, TrackingStrategy trackingStrategy)> GetEvaluationContextAsync(
        this HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        (Context context, TrackingStrategy strategy) resolvedContext;
        if (httpContext.Items.TryGetValue(_resolvedEvaluationContextKey, out var value))
        {
            Debug.Assert(value is (Context, TrackingStrategy));
            resolvedContext = ((Context, TrackingStrategy)) value;
        }
        else
        {
            var resolveContextAsync =
                httpContext.RequestServices.GetService<ResolveEvaluationContextAsyncDelegate>();

            if (resolveContextAsync != null)
            {
                resolvedContext = await resolveContextAsync(httpContext);
                httpContext.Items[_resolvedEvaluationContextKey] = resolvedContext;
            }
            else
            {
                resolvedContext = (_emptyContext, TrackingStrategy.Default);
            }
        }

        return resolvedContext.context == null
            ? throw new InvalidOperationException("The evaluation context cannot be null.")
            : resolvedContext;
    }
}
