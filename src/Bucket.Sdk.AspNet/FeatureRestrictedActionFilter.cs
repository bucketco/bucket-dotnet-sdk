namespace Bucket.Sdk;

/// <summary>
///     A wrapper around an action filter that will run in place of any filter that requires
///     a feature that is disabled to be enabled.
/// </summary>
/// <typeparam name="TFilter">The filter that will be used instead of this placeholder.</typeparam>
[DebuggerDisplay(
    "Feature: `{_featureKey}`, RequiresEnabled: `{_requiresEnabled}`, FilterType: `{typeof(TFilter).Name}`")]
internal sealed class FeatureRestrictedActionFilter<TFilter>: IAsyncActionFilter
    where TFilter : IAsyncActionFilter
{
    // The feature key to check before activating the wrapped filter
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly string _featureKey;

    // Whether the feature needs to be enabled (true) or disabled (false) to activate the wrapped filter
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly bool _requiresEnabled;

    /// <summary>
    ///     Creates a new instance of the <see cref="FeatureRestrictedActionFilter{TFilter}" /> class.
    /// </summary>
    /// <param name="featureKey">The feature key that the filter will check for.</param>
    /// <param name="enabled">Whether the feature should be enabled or disabled.</param>
    public FeatureRestrictedActionFilter(string featureKey, bool enabled = true)
    {
        ArgumentException.ThrowIfNullOrEmpty(featureKey);

        _featureKey = featureKey;
        _requiresEnabled = enabled;
    }

    /// <inheritdoc />
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        // Check the feature state for the current request
        var feature = await context.HttpContext.GetFeatureAsync(_featureKey);

        // Determine if the wrapped filter should be activated
        // - If `_requiresEnabled` is true, the feature must be enabled
        // - If `_requiresEnabled` is false, the feature must be disabled
        var isAllowed = _requiresEnabled ? feature.Enabled : !feature.Enabled;
        if (isAllowed)
        {
            // Feature condition is met - create and execute the wrapped filter
            // This uses the DI container to create the filter with any dependencies it needs
            var filter = ActivatorUtilities.CreateInstance<TFilter>(
                context.HttpContext.RequestServices
            );

            // Execute the wrapped filter
            await filter.OnActionExecutionAsync(context, next);
        }
        else
        {
            // Feature condition is not met - skip the wrapped filter and continue the pipeline
            _ = await next();
        }
    }
}
