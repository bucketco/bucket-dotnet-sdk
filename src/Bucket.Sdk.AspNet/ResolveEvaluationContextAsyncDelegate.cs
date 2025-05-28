namespace Bucket.Sdk;

/// <summary>
///     Used to resolve the context used for evaluation.
/// </summary>
/// <remarks>
///     Clients should implement this delegate based on their own requirements and register it with
///     the DI container.
/// </remarks>
/// <param name="httpContext">The HTTP context to resolve the context for.</param>
/// <returns>A task that completes with the resolved evaluation context and tracking strategy.</returns>
[PublicAPI]
public delegate ValueTask<(Context context, TrackingStrategy trackingStrategy)> ResolveEvaluationContextAsyncDelegate(
    HttpContext httpContext
);
