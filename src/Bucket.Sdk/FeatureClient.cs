namespace Bucket.Sdk;

using System.Net.Http.Json;

using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
///     A client for interacting with the Bucket API.
/// </summary>
[PublicAPI]
public sealed class FeatureClient: IFeatureClient, IDisposable, IAsyncDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly HttpClient _httpClient;
    private readonly IReadOnlyList<ResolveLocalFeaturesAsyncDelegate> _localFeaturesResolvers;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly ILogger<FeatureClient> _logger;
    private readonly OperationMode _mode;
    private readonly int _outputMaxMessages;
    private readonly Dictionary<OutputMessage, long> _outputMessageLog;
    private readonly List<(OutputMessage message, ActivityContext? activityContext)> _outputMessages;
    private readonly long _outputRollingWindow;
    private readonly Ticker<bool> _outputTicker;
    private readonly Ticker<IReadOnlyList<CompiledFeature>> _refreshTicker;
    private readonly long _staleFeaturesAge;

    private int _disposed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="FeatureClient" /> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="logger">The logger (optional).</param>
    /// <param name="localFeaturesResolvers">The local features resolvers (optional).</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="configuration" /> is  <see langword="null"/></exception>
    public FeatureClient(
        Configuration configuration,
        ILogger<FeatureClient>? logger = null,
        IEnumerable<ResolveLocalFeaturesAsyncDelegate>? localFeaturesResolvers = null
    ) : this(configuration, new HttpClient(), logger ?? NullLogger<FeatureClient>.Instance, localFeaturesResolvers)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="FeatureClient" /> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="httpClient">The HTTP client (optional).</param>
    /// <param name="logger">The logger (optional).</param>
    /// <param name="localFeaturesResolvers">The local features resolvers (optional).</param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when either <paramref name="configuration" />, <paramref name="logger" /> or
    ///     <paramref name="httpClient" /> are <see langword="null"/>.
    /// </exception>
    /// <remarks>
    ///     The supplied <paramref name="httpClient" /> will be modified to include the necessary headers and base address.
    /// </remarks>
    internal FeatureClient(
        Configuration configuration,
        HttpClient httpClient,
        ILogger<FeatureClient> logger,
        IEnumerable<ResolveLocalFeaturesAsyncDelegate>? localFeaturesResolvers = null
    )
    {
        // Guard against double dispose (through finalizer and Dispose/Async)
        _ = Interlocked.Increment(ref _disposed);

        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(logger);

        _outputRollingWindow = configuration.Output.RollingWindow.Ticks;
        _outputMaxMessages = configuration.Output.MaxMessages;
        _outputTicker = new(InternalFlushAsync, configuration.Output.FlushInterval);

        _staleFeaturesAge = configuration.Features.StaleAge.Ticks;
        _refreshTicker = new(InternalRefreshFeaturesAsync, configuration.Features.RefreshInterval);

        _localFeaturesResolvers = localFeaturesResolvers?.ToImmutableArray() ?? [];
        _cancellationTokenSource = new();

        _httpClient = httpClient;
        _logger = logger;

        _httpClient.BaseAddress = configuration.ApiBaseUri;
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {configuration.SecretKey}");

        var version = typeof(FeatureClient).Assembly.GetName()
            .Version?.ToString() ?? "unknown";

        _httpClient.DefaultRequestHeaders.Add("bucket-sdk-version", $"dotnet-sdk/{version}");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        _mode = configuration.Mode;

        _outputMessageLog = [];
        _outputMessages = [];

        // Release the double dispose guard
        _ = Interlocked.Decrement(ref _disposed);
    }

    /// <summary>
    ///     Specifies whether this instance is disposed.
    /// </summary>
    public bool Disposed => _disposed == 1;

    /// <summary>
    ///     Disposes the client and releases all resources asynchronously.
    /// </summary>
    /// <returns>A task that completes when the client is disposed.</returns>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 1)
        {
            return;
        }

        using var scope = _logger.Scope();

        await _cancellationTokenSource.CancelAsync();
        await _outputTicker.DisposeAsync();
        await _refreshTicker.DisposeAsync();

        _httpClient.CancelPendingRequests();
        _httpClient.Dispose();

        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Disposes the client and releases all resources.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 1)
        {
            return;
        }

        using var scope = _logger.Scope();

        _cancellationTokenSource.Cancel();
        _outputTicker.Dispose();
        _refreshTicker.Dispose();
        _httpClient.CancelPendingRequests();
        _httpClient.Dispose();

        GC.SuppressFinalize(this);
    }

    /// <inheritdoc cref="IFeatureClient.RefreshAsync" />
    /// <exception cref="InvalidOperationException">Thrown when the client is disposed.</exception>
    public async Task RefreshAsync()
    {
        AssertNotDisposed();

        using var scope = _logger.Scope();
        using var activity = Telemetry.StartRefreshingFeaturesDefinitions();

        if (_mode != OperationMode.LocalEvaluation)
        {
            _logger.NotInLocalEvaluationMode("not refreshing features definitions");
            activity.NotifyNotInLocalEvaluationMode();

            return;
        }

        await _refreshTicker.TickAsync();

        activity.NotifyFinished();
    }

    /// <inheritdoc cref="IFeatureClient.FlushAsync" />
    /// <exception cref="InvalidOperationException">Thrown when the client is disposed.</exception>
    public async Task FlushAsync()
    {
        AssertNotDisposed();

        using var scope = _logger.Scope();
        using var activity = Telemetry.StartFlushingOutputEvents();

        await _outputTicker.TickAsync();

        activity.NotifyFinished();
    }

    /// <inheritdoc cref="IFeatureClient.UpdateUserAsync" />
    /// <exception cref="InvalidOperationException">Thrown when the client is disposed.</exception>
    public async Task UpdateUserAsync(User user, UpdateStrategy updateStrategy = UpdateStrategy.Default)
    {
        ArgumentNullException.ThrowIfNull(user);

        AssertNotDisposed();

        using var scope = _logger.Scope();
        using var activity = Telemetry.StartUpdatingUserDetails().Initialize(user, updateStrategy);

        if (!CheckOnline("user update"))
        {
            activity.NotifyDiscardedDueToOfflineMode();
            return;
        }

        await EnqueueAsync(
            new UserMessage
            {
                UserId = user.Id,
                Attributes = new Dictionary<string, object?>(user.ToFields().Where(kv => kv.Key != "id")),
                Metadata = UpdateStrategyToTrackingMetadata(updateStrategy),
            },
            activity
        );

        activity.NotifyFinished();
    }

    /// <inheritdoc cref="IFeatureClient.UpdateCompanyAsync" />
    /// <exception cref="InvalidOperationException">Thrown when the client is disposed.</exception>
    public async Task UpdateCompanyAsync(Company company, User? user = null,
        UpdateStrategy updateStrategy = UpdateStrategy.Default)
    {
        ArgumentNullException.ThrowIfNull(company);

        AssertNotDisposed();

        using var scope = _logger.Scope();
        using var activity = Telemetry.StartUpdatingCompanyDetails().Initialize(company, user, updateStrategy);

        if (!CheckOnline("company update"))
        {
            activity.NotifyDiscardedDueToOfflineMode();
            return;
        }

        await EnqueueAsync(
            new CompanyMessage
            {
                CompanyId = company.Id,
                UserId = user?.Id,
                Attributes = new Dictionary<string, object?>(company.ToFields().Where(kv => kv.Key != "id")),
                Metadata = UpdateStrategyToTrackingMetadata(updateStrategy),
            },
            activity
        );

        activity.NotifyFinished();
    }

    /// <inheritdoc cref="IFeatureClient.TrackAsync" />
    /// <exception cref="InvalidOperationException">Thrown when the client is disposed.</exception>
    public async Task TrackAsync(Event @event, UpdateStrategy updateStrategy = UpdateStrategy.Default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        AssertNotDisposed();

        using var scope = _logger.Scope();
        using var activity = Telemetry.StartSendingTrackEvent().Initialize(@event, updateStrategy);

        if (!CheckOnline("track event"))
        {
            activity.NotifyDiscardedDueToOfflineMode();
            return;
        }

        await PostAsync("event", new TrackEventMessage
        {
            Name = @event.Name,
            UserId = @event.User.Id,
            CompanyId = @event.Company?.Id,
            Attributes = new Dictionary<string, object?>(@event),
            Metadata = UpdateStrategyToTrackingMetadata(updateStrategy),
        });

        activity.NotifyFinished();
    }

    /// <inheritdoc cref="IFeatureClient.GetFeaturesAsync" />
    /// <exception cref="InvalidOperationException">Thrown when the client is disposed.</exception>
    public async Task<IReadOnlyDictionary<string, EvaluatedFeature>> GetFeaturesAsync(Context context)
    {
        ArgumentNullException.ThrowIfNull(context);

        AssertNotDisposed();

        using var scope = _logger.Scope();
        using var activity = Telemetry.StartEvaluatingFeatures().Initialize(context, _mode);

        var contextFields = context.ToFields();
        var evaluatedFeatures = (_mode switch
        {
            OperationMode.LocalEvaluation => await EvaluateFetchedFeaturesAsync(contextFields),
            OperationMode.RemoteEvaluation => await EvaluateRemoteFeaturesAsync(contextFields),
            OperationMode.Offline => [],
            _ => [],
        }).ToDictionary(k => k.Key, v => v);

        var localFeatures = new List<EvaluatedFeature>();
        foreach (var resolver in _localFeaturesResolvers)
        {
            localFeatures.AddRange(await resolver(context, _cancellationTokenSource.Token));
        }

        foreach (var localFeature in localFeatures)
        {
            if (!evaluatedFeatures.TryGetValue(localFeature.Key, out var evaluatedFeature))
            {
                evaluatedFeatures[localFeature.Key] = localFeature with
                {
                    EvaluationContext = contextFields
                };
            }
            else if (localFeature.Override)
            {
                _logger.FeatureStatusIsOverriddenLocally(localFeature.Key);

                evaluatedFeatures[localFeature.Key] = evaluatedFeature with
                {
                    EvaluationContext = contextFields,
                    Enabled = localFeature.Enabled,
                    Config = localFeature.Config,
                    Override = true
                };
            }
        }

        activity.NotifyFeaturesEvaluated(evaluatedFeatures);

        var evaluationIssues = evaluatedFeatures.Values.SelectMany(f =>
        {
            var output = new HashSet<(string, EvaluationIssueType, string)>();

            if (f.FlagEvaluationDebugData is { EvaluationIssues.Count: > 0 })
            {
                output.UnionWith(f.FlagEvaluationDebugData.EvaluationIssues
                    .Select(issue => (f.Key, issue: issue.type, issue.name)));
            }

            if (f.ConfigEvaluationDebugData is { EvaluationIssues.Count: > 0 })
            {
                output.UnionWith(f.ConfigEvaluationDebugData.EvaluationIssues
                    .Select(issue => ($"{f.Key}/config", issue: issue.type, issue.name)));
            }

            return output;
        }).ToImmutableArray();

        if (evaluationIssues.Length > 0)
        {
            _logger.FeatureEvaluationReturnedIssues(evaluationIssues);
            activity.NotifyHadEvaluationIssues();
        }

        activity.NotifyFinished();

        return evaluatedFeatures;
    }

    /// <inheritdoc cref="IFeatureClient.GetFeatureAsync" />
    /// <exception cref="InvalidOperationException">Thrown when the client is disposed.</exception>
    public async Task<IFeature> GetFeatureAsync(
        string key,
        Context context,
        TrackingStrategy trackingStrategy = TrackingStrategy.Default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(context);

        AssertNotDisposed();

        using var scope = _logger.Scope();
        using var activity = Telemetry.StartEvaluatingFeature().Initialize(context, key, _mode);

        await UpdateContextAsync(context, trackingStrategy);

        var evaluatedFeatures = await GetFeaturesAsync(context);
        if (!evaluatedFeatures.TryGetValue(key, out var feature))
        {
            var defaultCheckFlagMessage = new FeatureEventMessage
            {
                FeatureKey = key,
                SubType = FeatureEventType.CheckFlag,
                EvaluationResult = false,
            };

            activity.NotifyFeatureNotFound();

            return new Feature(key, false,
                () => FeatureEventCallback(defaultCheckFlagMessage, trackingStrategy),
                () => FeatureTrackCallback(key, context, trackingStrategy)
            );
        }

        var checkFlagMessage = new FeatureEventMessage
        {
            FeatureKey = feature.Key,
            SubType = FeatureEventType.CheckFlag,
            EvaluationResult = feature.Enabled,
            TargetingVersion = feature.FlagEvaluationDebugData?.Version,
            Context = feature.EvaluationContext,
            EvaluatedRules = feature.FlagEvaluationDebugData?.EvaluatedRules,
            MissingFields = feature.FlagEvaluationDebugData?.EvaluationIssues
                .Where(issue => issue.type == EvaluationIssueType.MissingField)
                .Select(issue => issue.name).ToImmutableArray(),
        };

        activity.NotifyFeatureEvaluated(feature);

        return new Feature(
            feature.Key,
            feature.Enabled,
            () => FeatureEventCallback(checkFlagMessage, trackingStrategy),
            () => FeatureTrackCallback(key, context, trackingStrategy)
        );
    }

    /// <inheritdoc cref="IFeatureClient.GetFeatureAsync{TPayload}" />
    /// <exception cref="InvalidOperationException">Thrown when the client is disposed.</exception>
    public async Task<IFeature<TPayload>> GetFeatureAsync<TPayload>(
        string key, Context context, TrackingStrategy trackingStrategy = TrackingStrategy.Default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(context);

        AssertNotDisposed();

        using var scope = _logger.Scope();
        using var activity = Telemetry.StartEvaluatingFeature().Initialize(context, key, _mode);

        await UpdateContextAsync(context, trackingStrategy);

        var evaluatedFeatures = await GetFeaturesAsync(context);

        if (!evaluatedFeatures.TryGetValue(key, out var feature))
        {
            var defaultCheckFlagMessage = new FeatureEventMessage
            {
                FeatureKey = key,
                SubType = FeatureEventType.CheckFlag,
                EvaluationResult = false,
            };
            var defaultCheckConfigMessage = new FeatureEventMessage
            {
                FeatureKey = key,
                SubType = FeatureEventType.CheckConfig,
                EvaluationResult = ToJsonElement(new { }),
            };

            activity.NotifyFeatureNotFound();

            return new Feature<TPayload>(key, false,
                () => FeatureEventCallback(defaultCheckFlagMessage, trackingStrategy),
                () => FeatureEventCallback(defaultCheckConfigMessage, trackingStrategy),
                () => FeatureTrackCallback(key, context, trackingStrategy), (null, default));
        }

        TPayload? payload = default;
        if (feature.Config.HasValue)
        {
            try
            {
                payload = feature.Config.Value.Payload.As<TPayload>();
            }
            catch (Exception exception)
            {
                activity.NotifyConfigDeserializationError(exception);
                if (exception is JsonException or NotSupportedException)
                {
                    _logger.FeatureConfigDeserializationFailed(key, typeof(TPayload).Name, exception);
                }
                else
                {
                    throw;
                }
            }
        }

        var checkFlagMessage = new FeatureEventMessage
        {
            FeatureKey = feature.Key,
            SubType = FeatureEventType.CheckFlag,
            EvaluationResult = feature.Enabled,
            TargetingVersion = feature.FlagEvaluationDebugData?.Version,
            Context = feature.EvaluationContext,
            EvaluatedRules = feature.FlagEvaluationDebugData?.EvaluatedRules,
            MissingFields = feature.FlagEvaluationDebugData?.EvaluationIssues
                .Where(issue => issue.type == EvaluationIssueType.MissingField)
                .Select(issue => issue.name).ToImmutableArray(),
        };

        var checkConfigMessage = new FeatureEventMessage
        {
            FeatureKey = feature.Key,
            SubType = FeatureEventType.CheckConfig,
            EvaluationResult = ToJsonElement(new
            {
                key = feature.Config?.Key,
                payload = feature.Config?.Payload
            }),
            TargetingVersion = feature.ConfigEvaluationDebugData?.Version,
            Context = feature.EvaluationContext,
            EvaluatedRules = feature.ConfigEvaluationDebugData?.EvaluatedRules,
            MissingFields = feature.FlagEvaluationDebugData?.EvaluationIssues
                .Where(issue => issue.type == EvaluationIssueType.MissingField)
                .Select(issue => issue.name).ToImmutableArray(),
        };

        activity.NotifyFeatureEvaluated(feature);

        return new Feature<TPayload>(
            feature.Key,
            feature.Enabled,
            () => FeatureEventCallback(checkFlagMessage, trackingStrategy),
            () => FeatureEventCallback(checkConfigMessage, trackingStrategy),
            () => FeatureTrackCallback(key, context, trackingStrategy),
            (
                feature.Config?.Key,
                payload
            )
        );
    }

    private void AssertNotDisposed() => ObjectDisposedException.ThrowIf(_disposed == 1, this);

    private bool CheckOnline(string entity)
    {
        Debug.Assert(!string.IsNullOrEmpty(entity));

        var online = _mode != OperationMode.Offline;
        if (!online)
        {
            _logger.UsingOfflineMode(entity);
        }

        return online;
    }

    private async ValueTask<TResponse?> RequestAsync<TBody, TResponse>(
        HttpMethod method,
        string path,
        IReadOnlyDictionary<string, object?>? query,
        TBody? body = default
    )
    {
        Debug.Assert(_mode != OperationMode.Offline);
        Debug.Assert(!string.IsNullOrWhiteSpace(path));
        Debug.Assert(method == HttpMethod.Get || method == HttpMethod.Post);

        using var scope = _logger.Scope();

        try
        {
            var queryStrEnumerable = query?.Select(kv =>
                $"context.{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value?.ToString() ?? "null")}").OrderBy(k => k);

            if (queryStrEnumerable != null)
            {
                path += "?" + string.Join("&", queryStrEnumerable);
            }

            _logger.RequestToServerInitiated(method, path, body);

            var response = method == HttpMethod.Post
                ? await _httpClient.PostAsJsonAsync(
                    path, body, JsonContext.TransferOptions,
                    _cancellationTokenSource.Token
                )
                : await _httpClient.GetAsync(path, _cancellationTokenSource.Token);


            _logger.RequestToServerCompleted(method, path, response);

            if (response.IsSuccessStatusCode)
            {
                var jsonElement = await response.Content.ReadFromJsonAsync<JsonElement>(
                    JsonContext.TransferOptions
                );

                var pre = jsonElement.Deserialize<ResponseBase>(JsonContext.TransferOptions);
                if (pre is { Success: true })
                {
                    var result = jsonElement.Deserialize<TResponse>(JsonContext.TransferOptions);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            ResponseBase? errorResponse = null;
            try
            {
                errorResponse = await response.Content.ReadFromJsonAsync<ResponseBase>(
                    JsonContext.TransferOptions
                );
            }
            catch
            {
                // Ignore
            }

            _logger.RequestToServerReturnedInvalidResponse(HttpMethod.Get, response.StatusCode,
                path, errorResponse);
        }
        catch (OperationCanceledException)
        {
            _logger.RequestToServerCancelled(method, path);
        }
        catch (Exception exception)
        {
            _logger.RequestToServerFailedWithException(method, path, exception);
        }

        return default;
    }

    private async ValueTask PostAsync<TBody>(string path, TBody body) =>
        _ = await RequestAsync<TBody, ResponseBase>(HttpMethod.Post, path, null, body);

    private ValueTask<TResponse?> GetAsync<TResponse>(
        string path,
        IReadOnlyDictionary<string, object?>? query = null
    ) => RequestAsync<object, TResponse>(HttpMethod.Get, path, query);

    private async Task EnqueueAsync(
        OutputMessage message,
        IOutputBucketActivity bucketActivity,
        bool rateLimit = true)
    {
        Debug.Assert(bucketActivity != null);
        Debug.Assert(_mode != OperationMode.Offline);
        Debug.Assert(message != null);

        AssertNotDisposed();

        using var scope = _logger.Scope();

        await _lock.WaitAsync(_cancellationTokenSource.Token);
        try
        {
            var tick = Timing.TickCount;

            if (rateLimit)
            {
                if (rateLimit && _outputMessageLog.TryGetValue(message, out var sentTick) &&
                    tick - sentTick <= _outputRollingWindow)
                {
                    _logger.MessageRateLimited(message);
                    bucketActivity.NotifyOutputMessageDiscardedTueToRateLimiting();

                    return;
                }

                _outputMessageLog[message] = tick;
            }

            _outputMessages.Add((message, bucketActivity.Context));
            if (_outputMessages.Count >= _outputMaxMessages)
            {
                _logger.MaxMessagesReached(_outputMessages.Count);
                _ = await InternalFlushAsync();
            }
        }
        finally
        {
            _ = _lock.Release();
        }
    }

    private async ValueTask<(bool, bool)> InternalFlushAsync()
    {
        using var scope = _logger.Scope();
        using var activity = Telemetry.StartFlushingOutputEvents();

        var messages = new List<(OutputMessage message, ActivityContext? activityContext)>();
        await _lock.WaitAsync(_cancellationTokenSource.Token);
        try
        {
            messages.AddRange(_outputMessages);
            _outputMessages.Clear();

            var tick = Timing.TickCount;
            var oldLoggedMessages =
                _outputMessageLog.Where(m => tick - m.Value > _outputRollingWindow).ToImmutableArray();

            foreach (var message in oldLoggedMessages)
            {
                _ = _outputMessageLog.Remove(message.Key);
            }
        }
        finally
        {
            _ = _lock.Release();
        }

        if (messages.Count <= 0)
        {
            activity.NotifyNothingToFlush();
            return (true, true);
        }

        _logger.FlushingMessages(messages.Count);

        await PostAsync<IReadOnlyList<OutputMessage>>("bulk", messages.Select(m => m.message).ToImmutableArray());

        activity.NotifyFlushed(
            _outputMessages
                .Where(m => m.activityContext.HasValue)
                .Select(m => m.activityContext!.Value)
        );

        return (true, true);
    }

    private async ValueTask<(bool, IReadOnlyList<CompiledFeature>?)> InternalRefreshFeaturesAsync()
    {
        using var scope = _logger.Scope();

        using var activity = Telemetry.StartRefreshingFeaturesDefinitions();

        var response = await GetAsync<FeaturesDefinitionsResponse>("features");
        if (response?.Success != true)
        {
            _logger.FailedToRefreshFeatureDefinitions(response);
            activity.NotifyFailedToRefreshFeatureDefinitions();

            return (false, null);
        }

        var features = response.Features.Select(
            f => new CompiledFeature(f)
        ).ToImmutableArray();

        _logger.FeaturesRefreshed(features.Length);

        activity.NotifyFinished();

        return (true, features);
    }

    private void FeatureEventCallback(OutputBulkMessage message, TrackingStrategy trackingStrategy)
    {
        using var scope = _logger.Scope();
        using var activity = Telemetry.StartSendingFeatureEvent().Initialize(message, trackingStrategy);

        Debug.Assert(message != null);

        if (!CheckOnline("feature event"))
        {
            return;
        }

        if (trackingStrategy == TrackingStrategy.Disabled)
        {
            _logger.EventDiscardedDueToDisabledTracking(message);
            return;
        }

        EnqueueAsync(message, activity).Forget();

        activity.NotifyFinished();
    }

    private void FeatureTrackCallback(string featureKey, Context context, TrackingStrategy trackingStrategy)
    {
        Debug.Assert(!string.IsNullOrEmpty(featureKey));
        Debug.Assert(context != null);

        using var scope = _logger.Scope();
        using var activity = Telemetry.StartSendingTrackEvent().Initialize(featureKey, context, trackingStrategy);

        var updateStrategy = TrackingStrategyToUpdateStrategy(trackingStrategy);
        if (updateStrategy == null)
        {
            _logger.EventDiscardedDueToDisabledTracking(null);
            activity.NotifyDiscardedDueToDisabledTracking();

            return;
        }

        if (context.User == null)
        {
            _logger.EventDiscardedDueToMissingUser();
            activity.NotifyDiscardedDueToMissingUserDetails();

            return;
        }

        TrackAsync(
            new Event(featureKey, context.User)
            {
                Company = context.Company
            },
            updateStrategy.Value
        ).Forget();

        activity.NotifyFinished();
    }

    private async ValueTask UpdateContextAsync(Context context,
        TrackingStrategy trackingStrategy)
    {
        using var scope = _logger.Scope();

        Debug.Assert(context != null);

        var updateStrategy = TrackingStrategyToUpdateStrategy(trackingStrategy);
        if (updateStrategy == null)
        {
            _logger.EventDiscardedDueToDisabledTracking(null);
            return;
        }

        if (context.User != null)
        {
            await UpdateUserAsync(context.User, updateStrategy.Value);
        }

        if (context.Company != null)
        {
            await UpdateCompanyAsync(context.Company, context.User, updateStrategy.Value);
        }
    }

    private static UpdateStrategy? TrackingStrategyToUpdateStrategy(TrackingStrategy trackingStrategy)
    {
        return trackingStrategy switch
        {
            TrackingStrategy.Default => UpdateStrategy.Default,
            TrackingStrategy.Inactive => UpdateStrategy.Inactive,
            TrackingStrategy.Active => UpdateStrategy.Active,
            TrackingStrategy.Disabled => null,
            _ => null,
        };
    }

    private static TrackingMetadata? UpdateStrategyToTrackingMetadata(UpdateStrategy updateStrategy)
    {
        return updateStrategy switch
        {
            UpdateStrategy.Default => null,
            UpdateStrategy.Active => new() { Active = true },
            UpdateStrategy.Inactive => new() { Active = false },
            _ => new(),
        };
    }

    private async ValueTask<IEnumerable<EvaluatedFeature>> EvaluateFetchedFeaturesAsync(
        IReadOnlyDictionary<string, object?> contextFields)
    {
        Debug.Assert(_mode == OperationMode.LocalEvaluation);
        Debug.Assert(contextFields != null);

        using var scope = _logger.Scope();
        using var activity = Telemetry.StartEvaluatingFeatures().Initialize(contextFields, _mode);

        var features = await _refreshTicker.GetValueAsync();
        var evaluatedFeatures = new List<EvaluatedFeature>();
        if (!features.HasValue)
        {
            _logger.FeatureDefinitionsNotFetchedUsingLocalOnes();
            activity.NotifyNoFeatureDefinitionsAvailable();
        }
        else
        {
            Debug.Assert(features.Value != null);

            var stale = features.Age.Ticks > _staleFeaturesAge;
            if (stale)
            {
                _logger.FeatureDefinitionsAreStale(features.Age);
                activity.NotifyFeaturesDefinitionsAreStale();
            }

            foreach (var feature in features.Value)
            {
                var evaluatedFeature = feature.Evaluate(contextFields);

                if (evaluatedFeature.FlagEvaluationDebugData != null)
                {
                    await EnqueueAsync(
                        new FeatureEventMessage
                        {
                            FeatureKey = evaluatedFeature.Key,
                            SubType = FeatureEventType.EvaluateFlag,
                            EvaluationResult = evaluatedFeature.Enabled,
                            TargetingVersion = evaluatedFeature.FlagEvaluationDebugData?.Version,
                            Context = evaluatedFeature.EvaluationContext,
                            EvaluatedRules = evaluatedFeature.FlagEvaluationDebugData?.EvaluatedRules,
                            MissingFields = evaluatedFeature.FlagEvaluationDebugData?.EvaluationIssues
                                .Where(issue => issue.type == EvaluationIssueType.MissingField)
                                .Select(issue => issue.name).ToImmutableArray(),
                        },
                        activity
                    );
                }

                if (evaluatedFeature.ConfigEvaluationDebugData != null)
                {
                    await EnqueueAsync(
                        new FeatureEventMessage
                        {
                            FeatureKey = evaluatedFeature.Key,
                            SubType = FeatureEventType.EvaluateConfig,
                            EvaluationResult =
                                ToJsonElement(new
                                {
                                    key = evaluatedFeature.Config?.Key,
                                    payload = evaluatedFeature.Config?.Payload
                                }),
                            TargetingVersion = evaluatedFeature.ConfigEvaluationDebugData.Version,
                            Context = evaluatedFeature.EvaluationContext,
                            EvaluatedRules = evaluatedFeature.ConfigEvaluationDebugData.EvaluatedRules,
                            MissingFields =
                            [
                                .. evaluatedFeature.ConfigEvaluationDebugData.EvaluationIssues
                                    .Where(issue => issue.type == EvaluationIssueType.MissingField)
                                    .Select(issue => issue.name),
                            ],
                        },
                        activity
                    );
                }

                evaluatedFeatures.Add(evaluatedFeature);
            }
        }

        activity.NotifyFinished();

        return evaluatedFeatures;
    }

    private async ValueTask<IEnumerable<EvaluatedFeature>> EvaluateRemoteFeaturesAsync(
        IReadOnlyDictionary<string, object?> contextFields)
    {
        Debug.Assert(_mode == OperationMode.RemoteEvaluation);
        Debug.Assert(contextFields != null);

        using var scope = _logger.Scope();
        using var activity = Telemetry.StartEvaluatingFeatures().Initialize(contextFields, _mode);

        IEnumerable<EvaluatedFeature> evaluatedFeatures;
        var response = await GetAsync<FeaturesEvaluateResponse>("features/evaluated", contextFields);
        if (response == null)
        {
            _logger.RemoteFeatureEvaluationFailed();
            activity.NotifyFailedToEvaluateFeaturesRemotely();

            evaluatedFeatures = [];
        }
        else
        {
            evaluatedFeatures = response.Features.Select(feature => feature.Config != null
                ? new EvaluatedFeature(feature.Key, feature.Enabled, (feature.Config.Key, feature.Config.Payload))
                {
                    EvaluationContext = contextFields,
                    FlagEvaluationDebugData =
                        new()
                        {
                            Version = feature.TargetingVersion,
                            EvaluatedRules = feature.EvaluatedRules ?? [],
                            EvaluationIssues = feature.MissingFields?.Select(mf =>
                                (EvaluationIssueType.MissingField, mf)
                            ).ToImmutableArray() ?? [],
                        },
                    ConfigEvaluationDebugData =
                        feature.Config != null
                            ? new()
                            {
                                Version = feature.Config.Version,
                                EvaluatedRules = feature.Config.EvaluatedRules ?? [],
                                EvaluationIssues = feature.Config.MissingFields
                                    ?.Select(mf => (EvaluationIssueType.MissingField, mf)
                                    ).ToImmutableArray() ?? [],
                            }
                            : null,
                }
                : new(feature.Key, feature.Enabled)
                {
                    EvaluationContext = contextFields,
                    FlagEvaluationDebugData =
                        new()
                        {
                            Version = feature.TargetingVersion,
                            EvaluatedRules = feature.EvaluatedRules ?? [],
                            EvaluationIssues = feature.MissingFields?.Select(mf =>
                                (EvaluationIssueType.MissingField, mf)
                            ).ToImmutableArray() ?? [],
                        },
                });
        }

        activity.NotifyFinished();

        return evaluatedFeatures;
    }

    private static JsonElement ToJsonElement<T>(T @object) =>
        JsonSerializer.SerializeToElement(@object, JsonContext.PayloadOptions);

    /// <summary>
    ///     Finalizes the client and releases all resources.
    /// </summary>
    ~FeatureClient() => Dispose();
}
