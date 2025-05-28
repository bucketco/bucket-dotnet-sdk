namespace Bucket.Sdk;

/// <summary>
///     This class contains logging methods for the OpenFeature Bucket provider.
/// </summary>
internal static partial class Logging
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning,
        Message = "user is not set in context, discarded track event")]
    public static partial void TrackingEventDiscardedDueToMissingUser(this ILogger<BucketOpenFeatureProvider> logger);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning,
        Message = "evaluation failed for feature {featureKey} with error {errorType}: {message}")]
    public static partial void EvaluationFailed(this ILogger<BucketOpenFeatureProvider> logger, string featureKey,
        ErrorType errorType, string? message);
}
