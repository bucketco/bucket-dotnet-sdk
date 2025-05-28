namespace Bucket.Sdk;

/// <summary>
///     Used to provide local features to the SDK.
/// </summary>
/// <param name="context">The context used for evaluation.</param>
/// <param name="cancellationToken">A cancellation token used to interrupt the operation.</param>
/// <returns>A task that completes with the evaluated features.</returns>
[PublicAPI]
public delegate ValueTask<IEnumerable<EvaluatedFeature>> ResolveLocalFeaturesAsyncDelegate(
    Context context, CancellationToken cancellationToken
);
