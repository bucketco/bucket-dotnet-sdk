namespace Bucket.Sdk;

/// <summary>
///     Used by the SDK to invoke custom logic when a feature-restricted action is encountered.
/// </summary>
/// <remarks>
///     Clients should implement this delegate based on their own requirements and register it with
///     the DI container.
/// </remarks>
/// <param name="feature">The feature that was checked.</param>
/// <param name="context">The action context.</param>
/// <returns>A task that completes with the result of the handler.</returns>
[PublicAPI]
public delegate ValueTask<IActionResult> RestrictedFeatureActionHandlerAsyncDelegate(
    IFeature feature,
    FilterContext context
);
