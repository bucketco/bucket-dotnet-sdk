namespace Bucket.Sdk;

/// <summary>
///     Interface for telemetry activities related to Bucket operations.
/// </summary>
internal interface IBucketActivity: IDisposable
{
    /// <summary>
    ///     The wrapped activity.
    /// </summary>
    /// <remarks>
    ///     The activity is <see langword="null" /> if the activity was not started or requested.
    /// </remarks>
    protected Activity? Activity
    {
        get;
    }

    /// <summary>
    ///     Indicates whether the activity is in a state where all data is requested.
    /// </summary>
    protected bool NeedsDetails => Activity?.IsAllDataRequested == true;

    /// <summary>
    ///     Includes a tag in the activity.
    /// </summary>
    /// <param name="key">The key of the tag.</param>
    /// <param name="object">The value of the tag.</param>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <remarks>
    ///     If the activity is <see langword="null" /> or the value is <see langword="null" />, nothing happens.
    ///     If the <paramref name="object" /> is <see langword="null" />, it is discarded.
    /// </remarks>
    protected void Include<T>(string key, T? @object)
    {
        Debug.Assert(!string.IsNullOrEmpty(key));

        if (Activity != null && @object != null)
        {
            _ = Activity.SetTag(key, @object);
        }
    }

    /// <summary>
    ///     Emits an event with the specified name and tags.
    /// </summary>
    /// <param name="event">The name of the event.</param>
    /// <param name="tags">The tags associated with the event.</param>
    /// <remarks>
    ///     If the activity is not started or requested, nothing happens.
    /// </remarks>
    protected void Emit(string @event, Dictionary<string, object?> tags)
    {
        Debug.Assert(!string.IsNullOrEmpty(@event));
        Debug.Assert(tags != null);

        _ = Activity?.AddEvent(
            new(
                @event,
                default,
                [.. tags.Where(kv => kv.Value != null)]
            )
        );
    }

    /// <summary>
    ///     Emits an event with the specified name.
    /// </summary>
    /// <param name="event">The name of the event.</param>
    /// <remarks>
    ///     If the activity is not started or requested, nothing happens.
    /// </remarks>
    protected void Emit(string @event)
    {
        Debug.Assert(!string.IsNullOrEmpty(@event));

        _ = Activity?.AddEvent(new(@event));
    }

    /// <summary>
    ///     Links the specified contexts to the activity.
    /// </summary>
    /// <param name="contexts">The contexts to link.</param>
    /// <remarks>
    ///     If the activity is not started or requested, nothing happens.
    /// </remarks>
    protected void Link(params ActivityContext[] contexts)
    {
        Debug.Assert(contexts != null);

        if (Activity != null)
        {
            foreach (var context in contexts)
            {
                _ = Activity.AddLink(new(context));
            }
        }
    }

    /// <summary>
    ///     Notifies that the activity has finished.
    /// </summary>
    void NotifyFinished()
    {
        if (Activity?.Status == ActivityStatusCode.Unset)
        {
            _ = Activity.SetStatus(ActivityStatusCode.Ok);
        }
    }
}

/// <summary>
///     Represents an operation that generates output events.
/// </summary>
internal interface IOutputBucketActivity: IBucketActivity
{
    private const string _rateLimitedTag = "rate_limited";

    /// <summary>
    ///     The context of the wrapped activity. Used for linking.
    /// </summary>
    /// <remarks>
    ///     The context is <see langword="null" /> if no activity was started or requested.
    /// </remarks>
    ActivityContext? Context => Activity?.Context;

    /// <summary>
    ///     Notifies that the output message was discarded due to rate limiting.
    /// </summary>
    void NotifyOutputMessageDiscardedTueToRateLimiting()
    {
        if (NeedsDetails)
        {
            Include(_rateLimitedTag, true);
        }
    }
}

/// <summary>
///     Represents a feature evaluation operation.
/// </summary>
internal interface IFeatureEvaluationBucketActivity: IOutputBucketActivity
{
    /// <summary>
    ///     Notifies that the feature was not found.
    /// </summary>
    void NotifyFeatureNotFound()
    {
        if (NeedsDetails)
        {
            Include("feature_flag.not_found", true);
        }
    }

    /// <summary>
    ///     Notifies that feature config could not be deserialized.
    /// </summary>
    /// <param name="exception">The exception that occurred.</param>
    void NotifyConfigDeserializationError(Exception exception)
    {
        Debug.Assert(exception != null);
        _ = Activity?.AddException(exception);
    }

    /// <summary>
    ///     Notifies that the feature was evaluated.
    /// </summary>
    /// <param name="feature">The evaluated feature.</param>
    void NotifyFeatureEvaluated(EvaluatedFeature feature)
    {
        Debug.Assert(feature != null);

        if (NeedsDetails)
        {
            if (feature.ConfigEvaluationDebugData?.EvaluationIssues.Count > 0 ||
                feature.FlagEvaluationDebugData?.EvaluationIssues.Count > 0)
            {
                Include("feature_flag.evaluation_issues", true);
            }

            Emit(
                "feature_flag.evaluated",
                new()
                {
                    { "feature_flag.key", feature.Key },
                    { "feature_flag.version", feature.FlagEvaluationDebugData?.Version },
                    { "feature_flag.config.key", feature.Config?.Key },
                    { "feature_flag.overridden", feature.Override },
                    { "feature_flag.enabled", feature.Enabled },
                    {
                        "feature_flag.issues.missing_fields", feature
                            .FlagEvaluationDebugData?.EvaluationIssues
                            .Where(issue => issue.type == EvaluationIssueType.MissingField)
                            .Select(issue => issue.name).ToImmutableArray()
                    },
                    {
                        "feature_flag.issues.invalid_fields", feature
                            .FlagEvaluationDebugData?.EvaluationIssues
                            .Where(issue => issue.type == EvaluationIssueType.InvalidFieldType)
                            .Select(issue => issue.name).ToImmutableArray()
                    },
                    {
                        "feature_flag.config.issues.missing_fields", feature
                            .ConfigEvaluationDebugData?.EvaluationIssues
                            .Where(issue => issue.type == EvaluationIssueType.MissingField)
                            .Select(issue => issue.name).ToImmutableArray()
                    },
                    {
                        "feature_flag.config.issues.invalid_fields", feature
                            .ConfigEvaluationDebugData?.EvaluationIssues
                            .Where(issue => issue.type == EvaluationIssueType.InvalidFieldType)
                            .Select(issue => issue.name).ToImmutableArray()
                    },
                }
            );
        }
    }

    /// <summary>
    ///     Initializes the activity with the specified context and feature key.
    /// </summary>
    /// <param name="context">The context of the evaluation.</param>
    /// <param name="featureKey">The key of the feature being evaluated.</param>
    /// <param name="mode">The operation mode of the client.</param>
    /// <returns>The initialized activity.</returns>
    IFeatureEvaluationBucketActivity Initialize(Context context, string featureKey,
        OperationMode mode)
    {
        Include("feature_flag.key", featureKey);

        if (NeedsDetails)
        {
            Include("feature_flag.context.user_id", context.User?.Id);
            Include("feature_flag.context.organization_id", context.Company?.Id);
            Include("feature_flag.evaluation.mode", mode);
        }

        return this;
    }
}

/// <summary>
///     Represents the operation of evaluating features.
/// </summary>
internal interface IFeaturesEvaluationBucketActivity: IFeatureEvaluationBucketActivity
{
    /// <summary>
    ///     Notifies that the feature definitions are not available.
    /// </summary>
    void NotifyNoFeatureDefinitionsAvailable()
    {
        _ = Activity?
            .SetStatus(ActivityStatusCode.Error, "feature definitions have not been downloaded yet");
    }

    /// <summary>
    ///     Notifies that the feature definitions are stale.
    /// </summary>
    void NotifyFeaturesDefinitionsAreStale()
    {
        if (NeedsDetails)
        {
            Include("stale", true);
        }
    }

    /// <summary>
    ///     Notifies that remote evaluation failed.
    /// </summary>
    void NotifyFailedToEvaluateFeaturesRemotely()
    {
        _ = Activity?
            .SetStatus(ActivityStatusCode.Error, "failed to evaluate features remotely");
    }

    /// <summary>
    ///     Notifies that the features were evaluated.
    /// </summary>
    /// <param name="features">The evaluated features.</param>
    void NotifyFeaturesEvaluated(IReadOnlyDictionary<string, EvaluatedFeature> features)
    {
        Debug.Assert(features != null);

        if (NeedsDetails)
        {
            foreach (var (_, evaluatedFeature) in features)
            {
                NotifyFeatureEvaluated(evaluatedFeature);
            }
        }
    }

    /// <summary>
    ///     Notifies that the evaluation encountered issues.
    /// </summary>
    void NotifyHadEvaluationIssues() =>
        Activity?.SetStatus(ActivityStatusCode.Unset, "feature evaluation encountered issues.");

    /// <summary>
    ///     Initializes the activity with the specified context and operation mode.
    /// </summary>
    /// <param name="context">The context of the evaluation.</param>
    /// <param name="mode">The operation mode of the client.</param>
    /// <returns>The initialized activity.</returns>
    IFeaturesEvaluationBucketActivity Initialize(Context context, OperationMode mode)
    {
        if (NeedsDetails)
        {
            Include("feature_flag.context.user_id", context.User?.Id);
            Include("feature_flag.context.organization_id", context.Company?.Id);
            Include("feature_flag.evaluation.mode", mode);
        }

        return this;
    }

    /// <summary>
    ///     Initializes the activity with the specified context fields and operation mode.
    /// </summary>
    /// <param name="contextFields">The context fields of the evaluation.</param>
    /// <param name="mode">The operation mode of the client.</param>
    /// <returns>The initialized activity.</returns>
    IFeaturesEvaluationBucketActivity Initialize(
        IReadOnlyDictionary<string, object?> contextFields, OperationMode mode)
    {
        Debug.Assert(contextFields != null);

        if (NeedsDetails)
        {
            Include("feature_flag.context.user_id", contextFields.GetValueOrDefault("user.id"));
            Include("feature_flag.context.organization_id", contextFields.GetValueOrDefault("company.id"));
            Include("feature_flag.evaluation.mode", mode);
        }

        return this;
    }
}

/// <summary>
///     Represents the operation of refreshing feature definitions.
/// </summary>
internal interface IFeaturesDefinitionsRefreshBucketActivity: IBucketActivity
{
    /// <summary>
    ///     Notifies that the client is not in local evaluation mode.
    /// </summary>
    void NotifyNotInLocalEvaluationMode() =>
        Activity?.SetStatus(ActivityStatusCode.Unset, "not in local evaluation mode, not refreshing");

    /// <summary>
    ///     Notifies that the feature definitions were not refreshed due to a failure.
    /// </summary>
    void NotifyFailedToRefreshFeatureDefinitions() =>
        Activity?.SetStatus(ActivityStatusCode.Error, "failed to refresh feature definitions");
}

/// <summary>
///     Represents the operation of flushing output events.
/// </summary>
internal interface IOutputEventsFlushBucketActivity: IBucketActivity
{
    /// <summary>
    ///     Notifies that the flush operation has been performed.
    /// </summary>
    /// <param name="messageContexts">The contexts of the operations for messages that were flushed.</param>
    void NotifyFlushed(IEnumerable<ActivityContext> messageContexts)
    {
        Debug.Assert(messageContexts != null);

        if (NeedsDetails)
        {
            Emit("feature_flag.output_events_flushed");
            Link([.. messageContexts]);
        }
    }

    /// <summary>
    ///     Notifies that nothing was flushed.
    /// </summary>
    void NotifyNothingToFlush()
    {
        _ = Activity?
            .SetStatus(ActivityStatusCode.Ok, "nothing was flushed, queue is empty");
    }
}

/// <summary>
///     Represents the operation of updating user details.
/// </summary>
internal interface IUserDetailsUpdateBucketActivity: IOutputBucketActivity
{
    /// <summary>
    ///     Notifies that the user details update was not performed due to offline mode.
    /// </summary>
    void NotifyDiscardedDueToOfflineMode() =>
        Activity?.SetStatus(ActivityStatusCode.Unset, "offline mode, user update discarded");

    /// <summary>
    ///     Initializes the activity with the specified user and update strategy.
    /// </summary>
    /// <param name="user">The user whose details are being updated.</param>
    /// <param name="updateStrategy">The strategy used for the update.</param>
    /// <returns>The initialized activity.</returns>
    IUserDetailsUpdateBucketActivity Initialize(User user, UpdateStrategy updateStrategy)
    {
        Debug.Assert(user != null);

        Include("feature_flag.context.user_id", user.Id);

        if (NeedsDetails)
        {
            Include("feature_flag.update_strategy", updateStrategy);
        }

        return this;
    }
}

/// <summary>
///     Represents the operation of updating company details.
/// </summary>
internal interface ICompanyDetailsUpdateBucketActivity: IOutputBucketActivity
{
    /// <summary>
    ///     Notifies that the company details update was not performed due to offline mode.
    /// </summary>
    void NotifyDiscardedDueToOfflineMode() =>
        Activity?.SetStatus(ActivityStatusCode.Unset, "offline mode, company update discarded");

    /// <summary>
    ///     Initializes the activity with the specified company, user, and update strategy.
    /// </summary>
    /// <param name="company">The company whose details are being updated.</param>
    /// <param name="user">The user associated with the company.</param>
    /// <param name="updateStrategy">The strategy used for the update.</param>
    /// <returns>The initialized activity.</returns>
    ICompanyDetailsUpdateBucketActivity Initialize(
        Company company, User? user, UpdateStrategy updateStrategy)
    {
        Include("feature_flag.context.company_id", company.Id);
        Include("feature_flag.context.user_id", user?.Id);

        if (NeedsDetails)
        {
            Include("feature_flag.update_strategy", updateStrategy);
        }

        return this;
    }
}

/// <summary>
///     Represents the operation of sending a track event.
/// </summary>
internal interface ITrackEventSendBucketActivity: IOutputBucketActivity
{
    /// <summary>
    ///     Notifies that the track event was not sent due to tracking being disabled.
    /// </summary>
    void NotifyDiscardedDueToDisabledTracking() =>
        Activity?.SetStatus(ActivityStatusCode.Unset, "event is discarded, tracking is disabled");

    /// <summary>
    ///     Notifies that the track event was not sent due to the user not being set.
    /// </summary>
    void NotifyDiscardedDueToMissingUserDetails() =>
        Activity?.SetStatus(ActivityStatusCode.Unset, "event is discarded, user is not set");

    /// <summary>
    ///     Notifies that the track event was not sent due to offline mode.
    /// </summary>
    void NotifyDiscardedDueToOfflineMode() =>
        Activity?.SetStatus(ActivityStatusCode.Unset, "offline mode, track event discarded");

    /// <summary>
    ///     Initializes the activity with the specified event and update strategy.
    /// </summary>
    /// <param name="event">The event to be sent.</param>
    /// <param name="updateStrategy">The strategy used for the update.</param>
    /// <returns>The initialized activity.</returns>
    ITrackEventSendBucketActivity Initialize(Event @event, UpdateStrategy updateStrategy)
    {
        Debug.Assert(@event != null);

        Include("feature_flag.context.event_name", @event.Name);

        if (NeedsDetails)
        {
            Include("feature_flag.update_strategy", updateStrategy);
            Include("feature_flag.context.user_id", @event.User.Id);
            Include("feature_flag.context.company_id", @event.Company?.Id);
        }

        return this;
    }

    /// <summary>
    ///     Initializes the activity with the specified feature key, context, and tracking strategy.
    /// </summary>
    /// <param name="featureKey">The key of the feature being tracked.</param>
    /// <param name="context">The context of the event.</param>
    /// <param name="trackingStrategy">The strategy used for tracking.</param>
    /// <returns>The initialized activity.</returns>
    ITrackEventSendBucketActivity Initialize(
        string featureKey, Context context, TrackingStrategy trackingStrategy)
    {
        Debug.Assert(!string.IsNullOrEmpty(featureKey));
        Debug.Assert(context != null);

        Include("feature_flag.context.event_name", featureKey);

        if (NeedsDetails)
        {
            Include("feature_flag.tracking_strategy", trackingStrategy);
            Include("feature_flag.context.user_id", context.User?.Id);
            Include("feature_flag.context.company_id", context.Company?.Id);
        }

        return this;
    }
}

/// <summary>
///     Represents the operation of sending a feature event.
/// </summary>
internal interface IFeatureEventSendBucketActivity: IOutputBucketActivity
{
    /// <summary>
    ///     Initializes the activity with the specified event and tracking strategy.
    /// </summary>
    /// <param name="event">The event to be sent.</param>
    /// <param name="trackingStrategy">The strategy used for tracking.</param>
    /// <returns>The initialized activity.</returns>
    IFeatureEventSendBucketActivity Initialize(
        OutputBulkMessage @event, TrackingStrategy trackingStrategy)
    {
        Include("feature_flag.context.event_type", @event.Type);

        if (NeedsDetails)
        {
            Include("feature_flag.tracking_strategy", trackingStrategy);
        }

        return this;
    }
}

/// <summary>
///     Contains telemetry-related methods and properties.
/// </summary>
/// <remarks>
///     Consumers are only interested in <see cref="ActivitySourceName" /> property that should be used in their
///     applications when setting up telemetry.
///     When using OpenTelemetry register the source using:
///     <code>
///         using var tracerProvider = Sdk.CreateTracerProviderBuilder()
///             .AddSource(Bucket.Sdk.Telemetry.ActivitySourceName)
///             .AddAzureMonitorTraceExporter()
///             .Build();
///     </code>
/// </remarks>
public static class Telemetry
{
    /// <summary>
    ///     The name of the activity source used for telemetry.
    /// </summary>
    [PublicAPI] public const string ActivitySourceName = "Bucket.Features";

    private static readonly ActivitySource _activitySource = new(ActivitySourceName);

    private static TBucketActivity GetActivity<TBucketActivity>(string operationName)
        where TBucketActivity : class, IBucketActivity
    {
        var currentActivity = Activity.Current;

        var wrapped = currentActivity != null &&
            currentActivity.OperationName == operationName &&
            currentActivity.Source == _activitySource
            ? new WrappedActivity(currentActivity, false)
            : new WrappedActivity(_activitySource.StartActivity(operationName), true);

        var result = wrapped as TBucketActivity;

        Debug.Assert(result != null);

        return result;
    }

    /// <summary>
    ///     Starts a new activity for evaluating features.
    /// </summary>
    /// <returns>The started activity.</returns>
    internal static IFeaturesEvaluationBucketActivity StartEvaluatingFeatures() =>
        GetActivity<IFeaturesEvaluationBucketActivity>("feature_flag.evaluation");

    /// <summary>
    ///     Starts a new activity for evaluating a single feature.
    /// </summary>
    /// <returns>The started activity.</returns>
    internal static IFeatureEvaluationBucketActivity StartEvaluatingFeature() =>
        GetActivity<IFeatureEvaluationBucketActivity>("feature_flag.evaluation");

    /// <summary>
    ///     Starts a new activity for refreshing feature definitions.
    /// </summary>
    /// <returns>The started activity.</returns>
    internal static IFeaturesDefinitionsRefreshBucketActivity StartRefreshingFeaturesDefinitions() =>
        GetActivity<IFeaturesDefinitionsRefreshBucketActivity>("feature_flag.refresh");

    /// <summary>
    ///     Starts a new activity for flushing output events.
    /// </summary>
    /// <returns>The started activity.</returns>
    internal static IOutputEventsFlushBucketActivity StartFlushingOutputEvents() =>
        GetActivity<IOutputEventsFlushBucketActivity>("feature_flag.flush");

    /// <summary>
    ///     Starts a new activity for updating user details.
    /// </summary>
    /// <returns>The started activity.</returns>
    internal static IUserDetailsUpdateBucketActivity StartUpdatingUserDetails()
        => GetActivity<IUserDetailsUpdateBucketActivity>("feature_flag.user_update");

    /// <summary>
    ///     Starts a new activity for updating company details.
    /// </summary>
    /// <returns>The started activity.</returns>
    internal static ICompanyDetailsUpdateBucketActivity StartUpdatingCompanyDetails() =>
        GetActivity<ICompanyDetailsUpdateBucketActivity>("feature_flag.company_update");

    /// <summary>
    ///     Starts a new activity for sending a track event.
    /// </summary>
    /// <returns>The started activity.</returns>
    internal static ITrackEventSendBucketActivity StartSendingTrackEvent() =>
        GetActivity<ITrackEventSendBucketActivity>("feature_flag.tracking");

    /// <summary>
    ///     Starts a new activity for sending a feature event.
    /// </summary>
    /// <returns>The started activity.</returns>
    internal static IFeatureEventSendBucketActivity StartSendingFeatureEvent() =>
        GetActivity<IFeatureEventSendBucketActivity>("feature_flag.event");

    private readonly struct WrappedActivity(Activity? activity, bool dispose):
        IFeaturesEvaluationBucketActivity,
        IFeaturesDefinitionsRefreshBucketActivity,
        IOutputEventsFlushBucketActivity,
        IUserDetailsUpdateBucketActivity,
        ICompanyDetailsUpdateBucketActivity,
        ITrackEventSendBucketActivity,
        IFeatureEventSendBucketActivity
    {
        public Activity? Activity => activity;

        public void Dispose()
        {
            if (Activity != null && dispose)
            {
                Activity.Dispose();
            }
        }
    }
}
