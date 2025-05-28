namespace Bucket.Sdk;

using System.Net;
using System.Runtime.CompilerServices;

/// <summary>
///     This class contains logging methods for the <see cref="FeatureClient"/>.
/// </summary>
internal static partial class Logging
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "this client is running in offline mode, {entity} discarded.")]
    [DebuggerStepThrough]
    public static partial void UsingOfflineMode(this ILogger<FeatureClient> logger, string entity);

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "this client is not running in local evaluation mode, {message}.")]
    [DebuggerStepThrough]
    public static partial void NotInLocalEvaluationMode(this ILogger<FeatureClient> logger, string message);

    [LoggerMessage(EventId = 3, Level = LogLevel.Trace, Message = "{method}: initiating request to {args} with: {body}")]
    [DebuggerStepThrough]
    public static partial void RequestToServerInitiated(this ILogger<FeatureClient> logger, HttpMethod method, string args, object? body);

    [LoggerMessage(EventId = 4, Level = LogLevel.Trace, Message = "{method}: request to {args} returned: {response}")]
    [DebuggerStepThrough]
    public static partial void RequestToServerCompleted(this ILogger<FeatureClient> logger, HttpMethod method, string args,
        HttpResponseMessage response);

    [LoggerMessage(EventId = 5, Level = LogLevel.Error, Message = "{method}: request to {args} failed with error")]
    [DebuggerStepThrough]
    public static partial void RequestToServerFailedWithException(this ILogger<FeatureClient> logger, HttpMethod method, string args,
        Exception error);

    [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "{method}: request to {args} was cancelled")]
    [DebuggerStepThrough]
    public static partial void RequestToServerCancelled(this ILogger<FeatureClient> logger, HttpMethod method, string args);

    [LoggerMessage(EventId = 7, Level = LogLevel.Error, Message = "{method}: invalid {status} response received from {args}: {data}")]
    [DebuggerStepThrough]
    public static partial void RequestToServerReturnedInvalidResponse(this ILogger<FeatureClient> logger, HttpMethod method,
        HttpStatusCode status, string args, ResponseBase? data);

    [LoggerMessage(EventId = 8, Level = LogLevel.Debug, Message = "message {message} will not be sent (rate limited)")]
    [DebuggerStepThrough]
    public static partial void MessageRateLimited(this ILogger<FeatureClient> logger, OutputMessage message);

    [LoggerMessage(EventId = 9, Level = LogLevel.Debug, Message = "reached maximum {count} of allowed output messages, flushing")]
    [DebuggerStepThrough]
    public static partial void MaxMessagesReached(this ILogger<FeatureClient> logger, int count);

    [LoggerMessage(EventId = 10, Level = LogLevel.Debug, Message = "flushing {count} output messages")]
    [DebuggerStepThrough]
    public static partial void FlushingMessages(this ILogger<FeatureClient> logger, int count);

    [LoggerMessage(EventId = 11, Level = LogLevel.Warning, Message = "failed to refresh feature definitions: {response}")]
    [DebuggerStepThrough]
    public static partial void FailedToRefreshFeatureDefinitions(this ILogger<FeatureClient> logger, FeaturesDefinitionsResponse? response);

    [LoggerMessage(EventId = 12, Level = LogLevel.Debug, Message = "refreshed {count} features definitions")]
    [DebuggerStepThrough]
    public static partial void FeaturesRefreshed(this ILogger<FeatureClient> logger, int count);

    [LoggerMessage(EventId = 13, Level = LogLevel.Warning, Message = "tracking is explicitly disabled, discarded {event}")]
    [DebuggerStepThrough]
    public static partial void EventDiscardedDueToDisabledTracking(this ILogger<FeatureClient> logger, OutputMessage? @event);

    [LoggerMessage(EventId = 14, Level = LogLevel.Warning, Message = "user is not set in context, discarded track event")]
    [DebuggerStepThrough]
    public static partial void EventDiscardedDueToMissingUser(this ILogger<FeatureClient> logger);

    [LoggerMessage(EventId = 15, Level = LogLevel.Information, Message = "feature definitions have not been refreshed yet, using local ones")]
    [DebuggerStepThrough]
    public static partial void FeatureDefinitionsNotFetchedUsingLocalOnes(this ILogger<FeatureClient> logger);

    [LoggerMessage(EventId = 16, Level = LogLevel.Warning, Message = "cached features are stale with age of {age}")]
    [DebuggerStepThrough]
    public static partial void FeatureDefinitionsAreStale(this ILogger<FeatureClient> logger, TimeSpan age);

    [LoggerMessage(EventId = 17, Level = LogLevel.Warning, Message = "local feature {key} is overriding previous status of feature")]
    [DebuggerStepThrough]
    public static partial void FeatureStatusIsOverriddenLocally(this ILogger<FeatureClient> logger, string key);

    [LoggerMessage(EventId = 18, Level = LogLevel.Warning, Message = "flags and configs are potentially incorrectly evaluated: {evaluationIssues}")]
    [DebuggerStepThrough]
    public static partial void FeatureEvaluationReturnedIssues(this ILogger<FeatureClient> logger,
        IReadOnlyList<(string, EvaluationIssueType, string)> evaluationIssues);

    [LoggerMessage(EventId = 19, Level = LogLevel.Error, Message = "failed to deserialize feature config payload for feature {key} as {payloadType}")]
    [DebuggerStepThrough]
    public static partial void FeatureConfigDeserializationFailed(this ILogger<FeatureClient> logger, string key, string payloadType,
        Exception exception);

    [LoggerMessage(EventId = 20, Level = LogLevel.Error, Message = "failed to evaluate remotely")]
    [DebuggerStepThrough]
    public static partial void RemoteFeatureEvaluationFailed(this ILogger<FeatureClient> logger);

    /// <summary>
    ///     Creates a scope for logging with the specified name.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="name">The name of the scope (automatic).</param>
    /// <returns>An <see cref="IDisposable" /> instance representing the scope.</returns>
    [DebuggerStepThrough]
    public static IDisposable? Scope(this ILogger<FeatureClient> logger, [CallerMemberName] string name = "") =>
        logger.BeginScope(name);
}
