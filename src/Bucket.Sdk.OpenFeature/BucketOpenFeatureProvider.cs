namespace Bucket.Sdk;

/// <summary>
///     OpenFeature provider for Bucket feature management.
/// </summary>
[PublicAPI]
public sealed class BucketOpenFeatureProvider: FeatureProvider
{
    private readonly EvaluationContextTranslatorDelegate _evaluationContextTranslator;
    private readonly IFeatureClient _featureClient;
    private readonly ILogger<BucketOpenFeatureProvider> _logger;

    /// <summary>
    ///     Creates a new instance of <see cref="BucketOpenFeatureProvider" />.
    /// </summary>
    /// <param name="featureClient">The Bucket feature client.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="evaluationContextTranslator">The evaluation context translator.</param>
    /// <exception cref="ArgumentNullException">Thrown when the feature client is <see langword="null" />.</exception>
    public BucketOpenFeatureProvider(
        IFeatureClient featureClient,
        ILogger<BucketOpenFeatureProvider>? logger = null,
        EvaluationContextTranslatorDelegate? evaluationContextTranslator = null)
    {
        ArgumentNullException.ThrowIfNull(featureClient);

        _logger = logger ?? NullLogger<BucketOpenFeatureProvider>.Instance;
        _evaluationContextTranslator = evaluationContextTranslator ?? DefaultEvaluationContextTranslator.Translate;
        _featureClient = featureClient;
    }

    /// <inheritdoc />
    public override Metadata GetMetadata() => new("Bucket Feature Provider");

    private async Task<ResolutionDetails<T>> ResolveFeatureAsync<T>(
        string featureKey,
        T defaultValue,
        EvaluationContext? context,
        Func<EvaluatedFeature, ValueTask<ResolutionDetails<T>>> resolver,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(featureKey);

        var bucketContext = _evaluationContextTranslator(context);
        var features = await _featureClient.GetFeaturesAsync(bucketContext).WaitAsync(cancellationToken);

        var result = features.TryGetValue(featureKey, out var feature)
            ? await resolver(feature)
            : new ResolutionDetails<T>
            (
                featureKey,
                defaultValue,
                ErrorType.FlagNotFound,
                $"Feature {featureKey} not found"
            );

        if (result.ErrorType != ErrorType.None)
        {
            _logger.EvaluationFailed(featureKey, result.ErrorType, result.Reason);
        }

        return result;
    }

    /// <inheritdoc />
    public override Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(
        string flagKey,
        bool defaultValue,
        EvaluationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        return ResolveFeatureAsync(
            flagKey,
            defaultValue,
            context,
            feature => ValueTask.FromResult(
                new ResolutionDetails<bool>(
                    flagKey,
                    feature.Enabled,
                    variant: feature.Config?.Key,
                    reason: feature.Override ? Reason.Static : Reason.TargetingMatch
                )
            ),
            cancellationToken
        );
    }

    /// <inheritdoc />
    public override Task<ResolutionDetails<string>> ResolveStringValueAsync(
        string flagKey,
        string defaultValue,
        EvaluationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        return ResolveFeatureAsync(
            flagKey,
            defaultValue,
            context,
            feature => ValueTask.FromResult(
                feature.Config.HasValue
                    ? new ResolutionDetails<string>(
                        flagKey,
                        feature.Config.Value.Key,
                        variant: feature.Config.Value.Key,
                        reason: feature.Override ? Reason.Static : Reason.TargetingMatch
                    )
                    : new ResolutionDetails<string>(
                        flagKey,
                        defaultValue,
                        reason: Reason.Error,
                        errorType: ErrorType.TypeMismatch,
                        errorMessage: "Feature has no config"
                    )
            ),
            cancellationToken
        );
    }

    /// <inheritdoc />
    public override Task<ResolutionDetails<int>> ResolveIntegerValueAsync(
        string flagKey,
        int defaultValue,
        EvaluationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        return ResolveFeatureAsync(
            flagKey,
            defaultValue,
            context,
            feature => ValueTask.FromResult(
                    feature.Config.HasValue &&
                    feature.Config.Value.Payload.TryAsInt32(out var intValue)
                    ? new ResolutionDetails<int>(
                        flagKey,
                        intValue,
                        variant: feature.Config.Value.Key,
                        reason: feature.Override ? Reason.Static : Reason.TargetingMatch
                    )
                    : new ResolutionDetails<int>(
                        flagKey,
                        defaultValue,
                        variant: feature.Config?.Key,
                        reason: Reason.Error,
                        errorType: ErrorType.TypeMismatch,
                        errorMessage: "Feature has no config or payload is not of type `int`"
                    )
            ),
            cancellationToken
        );
    }

    /// <inheritdoc />
    public override Task<ResolutionDetails<double>> ResolveDoubleValueAsync(
        string flagKey,
        double defaultValue,
        EvaluationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        return ResolveFeatureAsync(
            flagKey,
            defaultValue,
            context,
            feature => ValueTask.FromResult(
                feature.Config.HasValue &&
                feature.Config.Value.Payload.TryAsDouble(out var doubleValue)
                    ? new ResolutionDetails<double>(
                        flagKey,
                        doubleValue,
                        variant: feature.Config.Value.Key,
                        reason: feature.Override ? Reason.Static : Reason.TargetingMatch
                    )
                    : new ResolutionDetails<double>(
                        flagKey,
                        defaultValue,
                        variant: feature.Config?.Key,
                        reason: Reason.Error,
                        errorType: ErrorType.TypeMismatch,
                        errorMessage: "Feature has no config or payload is not of type `double`"
                    )
            ),
            cancellationToken
        );
    }

    /// <inheritdoc />
    public override Task<ResolutionDetails<Value>> ResolveStructureValueAsync(
        string flagKey,
        Value defaultValue,
        EvaluationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        return ResolveFeatureAsync(
            flagKey,
            defaultValue,
            context,
            feature => ValueTask.FromResult(
                feature.Config.HasValue
                    ? new ResolutionDetails<Value>(
                        flagKey,
                        JsonElementToValue(feature.Config.Value.Payload.As<JsonElement>()),
                        variant: feature.Config.Value.Key,
                        reason: feature.Override ? Reason.Static : Reason.TargetingMatch
                    )
                    : new ResolutionDetails<Value>(
                        flagKey,
                        defaultValue,
                        reason: Reason.Error,
                        errorType: ErrorType.TypeMismatch,
                        errorMessage: "Feature has no config"
                    )
            ),
            cancellationToken
        );
    }

    private static Value JsonElementToValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => new Value(),
            JsonValueKind.False or JsonValueKind.True => new Value(value.GetBoolean()),
            JsonValueKind.Number => new Value(value.GetDouble()),
            JsonValueKind.String => new Value(value.GetString() ?? string.Empty),
            JsonValueKind.Array => new Value([.. value.EnumerateArray().Select(JsonElementToValue)]),
            JsonValueKind.Object => new Value(new Structure(value.EnumerateObject()
                .ToDictionary(x => x.Name, x => JsonElementToValue(x.Value)))),
            _ => new Value(), // Impossible case, but needed to satisfy the compiler
        };
    }

    /// <inheritdoc />
    public override async Task InitializeAsync(
        EvaluationContext context,
        CancellationToken cancellationToken = default) =>
        await _featureClient.RefreshAsync().WaitAsync(cancellationToken);

    /// <inheritdoc />
    public override async Task ShutdownAsync(CancellationToken cancellationToken = default) =>
        await _featureClient.FlushAsync().WaitAsync(cancellationToken);

    /// <inheritdoc />
    public override void Track(
        string trackingEventName,
        EvaluationContext? evaluationContext = null,
        TrackingEventDetails? trackingEventDetails = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(trackingEventName);
        var bucketContext = _evaluationContextTranslator(evaluationContext);

        if (bucketContext.User == null)
        {
            _logger.TrackingEventDiscardedDueToMissingUser();
            return;
        }

        var @event = new Event(trackingEventName, bucketContext.User) { Company = bucketContext.Company };

        if (trackingEventDetails != null)
        {
            foreach (var (key, value) in trackingEventDetails)
            {
                DefaultEvaluationContextTranslator.ExpandValue(@event, key, value);
            }
        }

        _ = _featureClient.TrackAsync(@event);
    }
}
