namespace Bucket.Sdk;

/// <summary>
///     An endpoint filter that will run in place of any filter that requires a feature that is disabled to be enabled.
/// </summary>
[DebuggerDisplay("Feature: `{_featureKey}`, RequiresEnabled: `{_requiresEnabled}`")]
internal sealed class FeatureRestrictedEndpointFilter: IEndpointFilter
{
    // The feature key to check during endpoint execution
    private readonly string _featureKey;

    // Whether the feature needs to be enabled (true) or disabled (false) for access
    private readonly bool _requiresEnabled;

    /// <summary>
    ///     Creates a new instance of the <see cref="FeatureRestrictedEndpointFilter" /> class.
    /// </summary>
    /// <param name="featureKey">The feature key that the filter will check for.</param>
    /// <param name="enabled">Whether the feature should be enabled or disabled.</param>
    public FeatureRestrictedEndpointFilter(string featureKey, bool enabled = true)
    {
        ArgumentException.ThrowIfNullOrEmpty(featureKey);

        _featureKey = featureKey;
        _requiresEnabled = enabled;
    }

    /// <inheritdoc />
    /// <remarks>
    ///     If the feature check fails, the request will be short-circuited and the appropriate response will be returned.
    ///     By default, this is a 404 response, unless a custom <see cref="RestrictedFeatureEndpointHandlerAsyncDelegate" />
    ///     handler is registered.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when either <paramref name="context" /> or <paramref name="next" /> are
    ///     <see langword="null"/>.
    /// </exception>
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        // Check the feature state for the current request
        var feature = await context.HttpContext.GetFeatureAsync(_featureKey);

        // Determine if the endpoint should be accessible
        // - If `_requiresEnabled` is true, the feature must be enabled
        // - If `_requiresEnabled` is false, the feature must be disabled
        var isAllowed = _requiresEnabled ? feature.Enabled : !feature.Enabled;

        if (isAllowed)
        {
            return await next(context); // Allow access and continue the pipeline
        }

        // Handle the case where the feature is disabled
        var handler = context.HttpContext.RequestServices.GetService<RestrictedFeatureEndpointHandlerAsyncDelegate>();
        return handler != null
            ? await handler(feature, context)
            : Results.NotFound();
    }
}
