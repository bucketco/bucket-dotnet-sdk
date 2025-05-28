namespace Bucket.Sdk;

/// <summary>
///     An attribute that can be used to gate controllers, actions or pages based on the enabled state of a feature.
/// </summary>
[PublicAPI]
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
[DebuggerDisplay("Feature: `{FeatureKey}`, RequiresEnabled: `{RequireEnabled}`")]
public sealed class FeatureRestrictedAttribute: ActionFilterAttribute, IAsyncPageFilter
{
    /// <summary>
    ///     Creates a new instance of the <see cref="FeatureRestrictedAttribute" /> class.
    /// </summary>
    /// <param name="featureKey">The feature key that the attribute will check for.</param>
    /// <param name="enabled">Whether the feature should be enabled or disabled.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="featureKey" /> is <see langword="null"/> or empty.</exception>
    public FeatureRestrictedAttribute(string featureKey, bool enabled = true)
    {
        ArgumentException.ThrowIfNullOrEmpty(featureKey);

        FeatureKey = featureKey;
        RequireEnabled = enabled;
    }

    /// <summary>
    ///     The feature key that the attribute will check for.
    /// </summary>
    public string FeatureKey
    {
        get;
    }

    /// <summary>
    ///     Whether the feature is expected to be enabled. This is the opposite of <see cref="RequireDisabled" />.
    /// </summary>
    public bool RequireEnabled
    {
        get; set;
    }

    /// <summary>
    ///     Whether the feature is expected to be disabled. This is the opposite of <see cref="RequireEnabled" />.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public bool RequireDisabled
    {
        get => !RequireEnabled;
        set => RequireEnabled = !value;
    }

    /// <inheritdoc />
    /// <remarks>
    ///     If the feature check fails, the request will be short-circuited and the appropriate response will be returned.
    ///     By default, this is a 404 response, unless a custom <see cref="RestrictedFeatureActionHandlerAsyncDelegate" />
    ///     handler is registered.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when either <paramref name="context" /> or <paramref name="next" /> are
    ///     <see langword="null"/>.
    /// </exception>
    public async Task OnPageHandlerExecutionAsync(
        PageHandlerExecutingContext context,
        PageHandlerExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        // Check the feature flag state for the current request
        var feature = await context.HttpContext.GetFeatureAsync(FeatureKey);

        // Determine if the user should be allowed to access the page
        // - If `RequireEnabled` is true, the feature must be enabled
        // - If `RequireEnabled` is false, the feature must be disabled
        var isAllowed = RequireEnabled ? feature.Enabled : !feature.Enabled;

        if (isAllowed)
        {
            // Access is allowed, continue the page handler execution
            _ = await next();
        }
        else
        {
            // Access is denied, return a 404 Not Found response
            var handler = context.HttpContext.RequestServices.GetService<RestrictedFeatureActionHandlerAsyncDelegate>();
            context.Result = handler != null ?
                await handler(feature, context) :
                new NotFoundResult();
        }
    }

    /// <inheritdoc />
    /// <remarks>
    ///     This method is a no-op.
    /// </remarks>
    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

    /// <inheritdoc />
    /// <remarks>
    ///     If the feature check fails, the request will be short-circuited and the appropriate response will be returned.
    ///     By default, this is a 404 response, unless a custom <see cref="RestrictedFeatureActionHandlerAsyncDelegate" />
    ///     handler is registered.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when either <paramref name="context" /> or <paramref name="next" /> are
    ///     <see langword="null"/>.
    /// </exception>
    public override async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        // Check the feature flag state for the current request
        var feature = await context.HttpContext.GetFeatureAsync(FeatureKey);

        // Determine if the user should be allowed to access the action
        var isAllowed = RequireEnabled ? feature.Enabled : !feature.Enabled;

        if (isAllowed)
        {
            // Access is allowed, continue the action execution pipeline
            _ = await next();
        }
        else
        {
            // Access is denied, get the registered handler or use the default
            var handler = context.HttpContext.RequestServices.GetService<RestrictedFeatureActionHandlerAsyncDelegate>();
            context.Result = handler != null ?
                await handler(feature, context) :
                new NotFoundResult();
        }
    }
}
