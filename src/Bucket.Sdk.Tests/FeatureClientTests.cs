namespace Bucket.Sdk.Tests;

public sealed class FeatureClientTests: IAsyncDisposable
{
    private const int _timeout = 5000;

    private const string _secretKey = "test-secret-key";
    private const string _trackEventName = "test-event";
    private const string _userId = "test-user-id";
    private const string _companyId = "test-company-id";
    private const string _featureKey = "test-feature-key";

    private static readonly Configuration _localConfiguration = new()
    {
        SecretKey = _secretKey,
        Mode = OperationMode.LocalEvaluation,
    };

    private static readonly Configuration _offlineConfiguration = new()
    {
        SecretKey = _secretKey,
        Mode = OperationMode.Offline,
    };

    private static readonly Configuration _remoteConfiguration = new()
    {
        SecretKey = _secretKey,
        Mode = OperationMode.RemoteEvaluation,
    };

    private readonly FakeLogCollector _fakeLogCollector;
    private readonly HttpClient _httpClient;

    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly ILogger<FeatureClient> _mockLogger;
    private readonly Mock<ResolveLocalFeaturesAsyncDelegate> _mockResolveFeatureDelegate1;
    private readonly Mock<ResolveLocalFeaturesAsyncDelegate> _mockResolveFeatureDelegate2;
    private readonly Queue<JsonElement> _outputMessages = new();

    private bool _assertFeaturesEvaluatedEndpointCalled;
    private bool _assertFeaturesDefinitionsEndpointCalled;

    private readonly List<Activity> _activityList = [];
    private readonly ActivityListener _activityListener;

    public FeatureClientTests()
    {
        // Mock system time for deterministic testing
        MockTime.MockSystemTimeAsync().Wait(TestContext.Current.CancellationToken);

        // Set up mocks
        _fakeLogCollector = new FakeLogCollector();
        _mockLogger = new FakeLogger<FeatureClient>(_fakeLogCollector);

        _ = _mockHttpMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        _ = _mockHttpMessageHandler.Protected().Setup(
            "Dispose", true, true);

        Task<bool> recordOutputMessage(HttpRequestMessage message)
        {
            var contentElement =
                message.Content != null
                    ? JsonSerializer.Deserialize<JsonElement>(
                        message.Content.ReadAsStringAsync().Result, JsonContext.TransferOptions
                    )
                    : default;

            if (contentElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in contentElement.EnumerateArray())
                {
                    _outputMessages.Enqueue(element);
                }
            }
            else
            {
                _outputMessages.Enqueue(contentElement);
            }

            return Task.FromResult(true);
        }

        _httpClient = new HttpClient(_mockHttpMessageHandler.Object, true);
        _ = _mockHttpMessageHandler.SetupAnyRequest()
            .ReturnsResponse(HttpStatusCode.NotFound);
        _ = _mockHttpMessageHandler.SetupAnyRequest()
            .ReturnsResponse(HttpStatusCode.NotFound);
        _ = _mockHttpMessageHandler
            .SetupRequest(HttpMethod.Post, $"{_remoteConfiguration.ApiBaseUri}bulk", recordOutputMessage)
            .ReturnsJsonResponse(
                new ResponseBase { Success = true },
                JsonContext.TransferOptions
            );
        _ = _mockHttpMessageHandler
            .SetupRequest(HttpMethod.Post, $"{_remoteConfiguration.ApiBaseUri}event", recordOutputMessage)
            .ReturnsJsonResponse(
                new ResponseBase { Success = true },
                JsonContext.TransferOptions
            );

        _ = _mockResolveFeatureDelegate1 = new Mock<ResolveLocalFeaturesAsyncDelegate>(MockBehavior.Strict);
        _ = _mockResolveFeatureDelegate2 = new Mock<ResolveLocalFeaturesAsyncDelegate>(MockBehavior.Strict);
        _ = _mockResolveFeatureDelegate1
            .Setup(m => m.Invoke(It.IsAny<Context>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync([]);
        _ = _mockResolveFeatureDelegate2
            .Setup(m => m.Invoke(It.IsAny<Context>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync([]);

        // Tracing
        _activityListener = new()
        {
            ShouldListenTo = activitySource => activitySource.Name == Telemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = _activityList.Add
        };

        ActivitySource.AddActivityListener(_activityListener);
    }

    public async ValueTask DisposeAsync()
    {
        // Restore system time
        await MockTime.RestoreSystemTimeAsync();

        if (TestContext.Current.TestState?.Result is
            TestResult.Passed or TestResult.Skipped)
        {
            if (_outputMessages.Count > 0)
            {
                var allMessages = string.Join("\n", _outputMessages.Select(m => $"  -  {m}"));
                Assert.Fail($"Not all output messages were checked:\n{allMessages}");
            }

            if (_assertFeaturesEvaluatedEndpointCalled)
            {
                _mockHttpMessageHandler.VerifyRequest(
                    HttpMethod.Get,
                    request => $"{request.RequestUri?.Scheme}://{request.RequestUri?.Host}{request.RequestUri?.AbsolutePath}" ==
                               _remoteConfiguration.ApiBaseUri + "features/evaluated",
                    Times.Once()
                );
            }

            if (_assertFeaturesDefinitionsEndpointCalled)
            {
                _mockHttpMessageHandler.VerifyRequest(
                    HttpMethod.Get,
                    $"{_remoteConfiguration.ApiBaseUri}features",
                    Times.AtLeastOnce()
                );
            }


            // Verify logs and diagnostics
            var logs = _fakeLogCollector.GetSnapshot().Select(log =>
                new
                {
                    log.Level,
                    log.Category,
                    log.Message,
                    Exception =
                        log.Exception != null ? new
                        {
                            log.Exception.GetType().Name,
                            log.Exception.Message
                        } : null,
                });

            var diagnostics = _activityList.Select(activity =>
                new
                {
                    Name = activity.OperationName,
                    Tags = activity.Tags.ToImmutableArray(),
                    Events = activity.Events.Select(@event =>
                        new
                        {
                            @event.Name,
                            Tags = @event.Tags.ToImmutableArray()
                        }
                    ).ToImmutableArray(),
                }
            );

            await new
            {
                Logs = logs,
                Traces = diagnostics,
            }.VerifySnapshotAsync();
        }

        _activityListener.Dispose();

    }

    private FeatureClient SetupClientAndGetFeature(OperationMode mode)
    {
        switch (mode)
        {
            case OperationMode.LocalEvaluation:
                SetupFeaturesDefinitionsEndpoint("response.features-definitions.none");
                break;
            case OperationMode.RemoteEvaluation:
                SetupFeaturesEvaluatedEndpoint("response.features-evaluated.none");
                break;
            case OperationMode.Offline:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
        }

        return new FeatureClient(
            new Configuration { SecretKey = _secretKey, Mode = mode },
            _httpClient,
            _mockLogger
        );
    }

    private void SetupFeaturesEvaluatedEndpoint(string fixture)
    {
        var fixtureData = JsonAssert.GetFixture<FeaturesEvaluateResponse>(fixture);

        _assertFeaturesEvaluatedEndpointCalled = true;

        _ = _mockHttpMessageHandler
            .SetupRequest(HttpMethod.Get, request =>
            {
                var matches =
                    $"{request.RequestUri?.Scheme}://{request.RequestUri?.Host}{request.RequestUri?.AbsolutePath}" ==
                    _remoteConfiguration.ApiBaseUri + "features/evaluated";
                return matches;
            })
            .ReturnsJsonResponse(
                fixtureData,
                JsonContext.TransferOptions
            );
    }

    private void SetupFeaturesDefinitionsEndpoint(string fixture)
    {
        var fixtureData = JsonAssert.GetFixture<FeaturesDefinitionsResponse>(fixture);

        _assertFeaturesDefinitionsEndpointCalled = true;

        _ = _mockHttpMessageHandler
            .SetupRequest(HttpMethod.Get, request =>
            {
                var matches =
                    $"{request.RequestUri?.Scheme}://{request.RequestUri?.Host}{request.RequestUri?.AbsolutePath}" ==
                    _remoteConfiguration.ApiBaseUri + "features";
                return matches;
            })
            .ReturnsJsonResponse(
                fixtureData,
                JsonContext.TransferOptions
            );
    }

    private async Task VerifyOutputAsync(OutputMessage expected, params OutputMessage[] more)
    {
        Debug.Assert(expected != null);
        Debug.Assert(more != null);

        var messages = new List<OutputMessage> { expected };
        messages.AddRange(more);

        // Wait for the output messages to be collected.
        var timeLeft = _timeout / 2;
        while (_outputMessages.Count < messages.Count)
        {
            if (timeLeft <= 0)
            {
                Assert.Fail(
                    $"Expected {messages.Count} output messages, but only {_outputMessages.Count} were collected." +
                    "\nLogs:\n" +
                    string.Join("\n", _fakeLogCollector.GetSnapshot().Select(log => $"  -  {log.Message}")) +
                    "\nDiagnostics:\n" +
                    string.Join("\n", _activityList.Select(activity =>
                        $"  -  {activity.OperationName} ({activity.Status} {activity.StatusDescription}) {string.Join("; ", activity.Tags.Select(tag => $"{tag.Key}: {tag.Value}"))}\n" +
                        $"  |  Events: {string.Join(", ", activity.Events.Select(@event => $"{@event.Name} {string.Join("; ", @event.Tags.Select(tag => $"{tag.Key}: {tag.Value}"))}"))}\n"
                    ))
                );
            }

            await Task.Delay(100, TestContext.Current.CancellationToken);
            timeLeft -= 100;
        }

        // Verify the output messages.
        for (var i = 0; i < messages.Count; i++)
        {
            var message = messages[i];
            Debug.Assert(message != null);

            _ = _outputMessages.TryDequeue(out var actual);
            var expectedElement = JsonSerializer.SerializeToElement(message, message.GetType(), JsonContext.TransferOptions);

            JsonAssert.Equivalent(expectedElement, actual, $"messages[{i}]<{message.GetType().Name}>");
        }
    }

    private static void AssertEvaluatedFeaturesEqual(EvaluatedFeature expected, EvaluatedFeature actual)
    {
        var expectedAsJsonElement = expected.AsJsonElement();
        var actualAsJsonElement = actual.AsJsonElement();

        JsonAssert.Equivalent(expectedAsJsonElement, actualAsJsonElement, "feature");
    }

    private static TrackingMetadata? TrackingStrategyTrackingMetadata(TrackingStrategy trackingStrategy)
    {
        return trackingStrategy switch
        {
            TrackingStrategy.Active => new() { Active = true },
            TrackingStrategy.Inactive => new() { Active = false },
            TrackingStrategy.Default => null,
            TrackingStrategy.Disabled => null,
            _ => null,
        };
    }

    #region Constructor Tests

    [Fact(Timeout = _timeout)]
    public void Constructor_ThrowsWhenArgumentsAreInvalid()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            new FeatureClient(null!, _mockLogger));
        _ = Assert.Throws<ArgumentNullException>(() =>
            new FeatureClient(null!, _httpClient, _mockLogger));
        _ = Assert.Throws<ArgumentNullException>(() =>
            new FeatureClient(_remoteConfiguration, null!, _mockLogger));
        _ = Assert.Throws<ArgumentNullException>(() =>
            new FeatureClient(_remoteConfiguration, _httpClient, null!));
    }

    [Fact(Timeout = _timeout)]
    public void Constructor_CreatesANonDisposedInstance()
    {
        // Act
        using var client = new FeatureClient(_offlineConfiguration, _httpClient, _mockLogger);

        // Assert
        Assert.False(client.Disposed);
    }

    [Fact(Timeout = _timeout)]
    public void Constructor_ConfiguresHttpClient()
    {
        // Act
        using var client = new FeatureClient(_offlineConfiguration, _httpClient, _mockLogger);

        // Assert
        var headers = _httpClient.DefaultRequestHeaders;

        Assert.Equal(_offlineConfiguration.ApiBaseUri, _httpClient.BaseAddress);
        Assert.Equal($"Bearer {_secretKey}", headers.GetValues("Authorization").First());
        Assert.Equal("application/json", headers.GetValues("Accept").First());
        Assert.StartsWith("dotnet-sdk/", headers.GetValues("bucket-sdk-version").First());
    }

    #endregion

    #region Dispose & DisposeAsync Tests

    [Fact(Timeout = _timeout)]
    public void Dispose_DisposesDependencies_AndSetsDisposedToTrue()
    {
        // Arrange
        var client = new FeatureClient(_localConfiguration, _httpClient, _mockLogger);

        _ = client.UpdateUserAsync(new User(_userId));

        // Act
        client.Dispose();

        // Advance time to trigger the flush and refresh intervals
        _ = MockTime.AdvanceTimeAsync(_localConfiguration.Output.FlushInterval).WaitAsync(TestContext.Current.CancellationToken);
        _ = MockTime.AdvanceTimeAsync(_localConfiguration.Features.RefreshInterval).WaitAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(client.Disposed);
        _mockHttpMessageHandler.Protected().Verify("Dispose", Times.Once(), true, true);
        _mockHttpMessageHandler.VerifyAnyRequest(
            Times.Never()
        );
    }

    [Fact(Timeout = _timeout)]
    public async Task DisposeAsync_DisposesDependencies_AndSetsDisposedToTrue_Async()
    {
        // Arrange
        var client = new FeatureClient(_localConfiguration, _httpClient, _mockLogger);

        await client.UpdateUserAsync(new User(_userId));

        // Act
        await client.DisposeAsync();

        // Advance time to trigger the flush and refresh intervals
        await MockTime.AdvanceTimeAsync(_localConfiguration.Output.FlushInterval);
        await MockTime.AdvanceTimeAsync(_localConfiguration.Features.RefreshInterval);

        // Assert
        Assert.True(client.Disposed);
        _mockHttpMessageHandler.Protected().Verify("Dispose", Times.Once(), true, true);
        _mockHttpMessageHandler.VerifyAnyRequest(
            Times.Never()
        );
    }

    [Fact(Timeout = _timeout)]
    public async Task DisposedClient_ThrowsObjectDisposedException_WhenMethodsAreCalled_Async()
    {
        // Arrange
        var client = new FeatureClient(_localConfiguration, _httpClient, _mockLogger);
        var user = new User(_userId);
        var company = new Company(_companyId);
        var context = new Context { User = user, Company = company };
        var @event = new Event(_trackEventName, user) { Company = company };

        // Act - Dispose the client
        await client.DisposeAsync();

        // Assert - All public methods should throw ObjectDisposedException
        _ = await Assert.ThrowsAsync<ObjectDisposedException>(
            client.RefreshAsync);
        _ = await Assert.ThrowsAsync<ObjectDisposedException>(
            client.FlushAsync);
        _ = await Assert.ThrowsAsync<ObjectDisposedException>(() => client.UpdateUserAsync(user));
        _ = await Assert.ThrowsAsync<ObjectDisposedException>(() => client.UpdateCompanyAsync(company));
        _ = await Assert.ThrowsAsync<ObjectDisposedException>(() => client.TrackAsync(@event));
        _ = await Assert.ThrowsAsync<ObjectDisposedException>(() => client.GetFeaturesAsync(context));
        _ = await Assert.ThrowsAsync<ObjectDisposedException>(() => client.GetFeatureAsync(_featureKey, context));
        _ = await Assert.ThrowsAsync<ObjectDisposedException>(() => client.GetFeatureAsync<string>(_featureKey, context));
    }

    [Fact(Timeout = _timeout)]
    public async Task DisposedClient_ThrowsObjectDisposedException_WhenFeaturesAreAccessed_Async()
    {
        // Arrange
        var context = new Context { User = new User(_userId) };
        var client = new FeatureClient(_remoteConfiguration, _httpClient, _mockLogger);

        // Get features before disposing the client
        var feature = await client.GetFeatureAsync(_featureKey, context);
        var featureWithConfig = await client.GetFeatureAsync<string>(_featureKey, context);

        // Act - Dispose the client
        await client.DisposeAsync();

        // Assert - Accessing features from a disposed client should throw
        MockTime.AssertForgottenException<ObjectDisposedException>(() => feature.Enabled);
        MockTime.AssertForgottenException<ObjectDisposedException>(feature.Track);

        MockTime.AssertForgottenException<ObjectDisposedException>(() => featureWithConfig.Enabled);
        MockTime.AssertForgottenException<ObjectDisposedException>(() => featureWithConfig.Config);
        MockTime.AssertForgottenException<ObjectDisposedException>(featureWithConfig.Track);
    }

    #endregion

    #region UpdateUserAsync Tests

    [Fact(Timeout = _timeout)]
    public async Task UpdateUserAsync_InOfflineMode_LogsAndDoesNotSendRequests_Async()
    {
        // Arrange
        var client = new FeatureClient(_offlineConfiguration, _httpClient, _mockLogger);
        var user = new User(_userId);

        // Act
        await client.UpdateUserAsync(user);

        _mockHttpMessageHandler.VerifyAnyRequest(Times.Never());
    }

    [Theory(Timeout = _timeout)]
    [InlineData(UpdateStrategy.Default)]
    [InlineData(UpdateStrategy.Active)]
    [InlineData(UpdateStrategy.Inactive)]
    public async Task UpdateUserAsync_GeneratesTheCorrectMessages_Async(UpdateStrategy strategy)
    {
        // Arrange
        var client = new FeatureClient(_remoteConfiguration, _httpClient, _mockLogger);
        var user = new User(_userId)
        {
            Name = "Test User",
            Email = "test@example.com",
            Avatar = new Uri("https://example.com/avatar.png"),
            ["custom-attribute"] = "custom-value",
        };

        // Act
        await client.UpdateUserAsync(user, strategy);
        await client.FlushAsync();

        // Assert
        await VerifyOutputAsync(new UserMessage
        {
            UserId = _userId,
            Attributes = new Dictionary<string, object?>
            {
                ["name"] = user.Name,
                ["email"] = user.Email,
                ["avatar"] = user.Avatar?.ToString(),
                ["custom-attribute"] = "custom-value",
            },
            Metadata = strategy == UpdateStrategy.Default
                ? null
                : new TrackingMetadata { Active = strategy == UpdateStrategy.Active },
        });
    }

    #endregion

    #region UpdateCompanyAsync Tests

    [Fact(Timeout = _timeout)]
    public async Task UpdateCompanyAsync_InOfflineMode_LogsAndDoesNotSendRequests_Async()
    {
        // Arrange
        var client = new FeatureClient(_offlineConfiguration, _httpClient, _mockLogger);
        var company = new Company(_companyId);

        // Act
        await client.UpdateCompanyAsync(company);

        _mockHttpMessageHandler.VerifyAnyRequest(Times.Never());
    }

    [Theory(Timeout = _timeout)]
    [InlineData(UpdateStrategy.Default)]
    [InlineData(UpdateStrategy.Active)]
    [InlineData(UpdateStrategy.Inactive)]
    public async Task UpdateCompanyAsync_GeneratesTheCorrectMessages_Async(UpdateStrategy strategy)
    {
        // Arrange
        var client = new FeatureClient(_remoteConfiguration, _httpClient, _mockLogger);
        var company = new Company(_companyId)
        {
            Name = "Test Company",
            Avatar = new Uri("https://example.com/avatar.png"),
            ["custom-attribute"] = "custom-value",
        };

        // Act
        await client.UpdateCompanyAsync(company, null, strategy);
        await client.FlushAsync();

        // Assert
        await VerifyOutputAsync(new CompanyMessage
        {
            CompanyId = _companyId,
            Attributes = new Dictionary<string, object?>
            {
                ["name"] = company.Name,
                ["avatar"] = company.Avatar?.ToString(),
                ["custom-attribute"] = "custom-value",
            },
            Metadata = strategy == UpdateStrategy.Default
                ? null
                : new TrackingMetadata { Active = strategy == UpdateStrategy.Active },
        });
    }

    [Fact(Timeout = _timeout)]
    public async Task UpdateCompanyAsync_AttachesUserIfNeeded_Async()
    {
        // Arrange
        var client = new FeatureClient(_remoteConfiguration, _httpClient, _mockLogger);
        var company = new Company(_companyId)
        {
            Name = "Test User",
            Avatar = new Uri("https://example.com/avatar.png"),
            ["custom-attribute"] = "custom-value",
        };
        var user = new User(_userId) { Email = "user@test.com" };

        // Act
        await client.UpdateCompanyAsync(company, user);
        await client.FlushAsync();

        // Assert
        await VerifyOutputAsync(new CompanyMessage
        {
            CompanyId = _companyId,
            UserId = _userId,
            Attributes = new Dictionary<string, object?>
            {
                ["name"] = company.Name,
                ["avatar"] = company.Avatar?.ToString(),
                ["custom-attribute"] = "custom-value",
            },
        });
    }

    #endregion

    #region Track Endpoint Tests

    [Fact(Timeout = _timeout)]
    public async Task TrackAsync_InOfflineMode_LogsAndDoesNotSendRequests_Async()
    {
        // Arrange
        var client = new FeatureClient(_offlineConfiguration, _httpClient, _mockLogger);
        var @event = new Event(_trackEventName, new User(_userId));

        // Act
        await client.TrackAsync(@event);

        // Assert
        _mockHttpMessageHandler.VerifyAnyRequest(Times.Never());
    }

    [Theory(Timeout = _timeout)]
    [InlineData(UpdateStrategy.Default)]
    [InlineData(UpdateStrategy.Active)]
    [InlineData(UpdateStrategy.Inactive)]
    public async Task TrackAsync_GeneratesTheCorrectMessages_Async(UpdateStrategy strategy)
    {
        // Arrange
        var client = new FeatureClient(_remoteConfiguration, _httpClient, _mockLogger);
        var user = new User(_userId) { Name = "Test User" };
        var @event = new Event(_trackEventName, user) { ["custom-attribute"] = "custom-value" };

        // Act
        await client.TrackAsync(@event, strategy);

        // Assert
        await VerifyOutputAsync(new TrackEventMessage
        {
            Name = _trackEventName,
            UserId = _userId,
            Attributes = new Dictionary<string, object?> { ["custom-attribute"] = "custom-value" },
            Metadata = strategy == UpdateStrategy.Default
                ? null
                : new TrackingMetadata { Active = strategy == UpdateStrategy.Active },
        });
    }

    [Fact(Timeout = _timeout)]
    public async Task TrackAsync_AttachesUserIfNeeded_Async()
    {
        // Arrange
        var client = new FeatureClient(_remoteConfiguration, _httpClient, _mockLogger);
        var user = new User(_userId) { Name = "Test User" };
        var company = new Company(_companyId) { Name = "Test Company" };
        var @event = new Event(_trackEventName, user) { Company = company, ["custom-attribute"] = "custom-value" };

        // Act
        await client.TrackAsync(@event);

        // Assert
        await VerifyOutputAsync(new TrackEventMessage
        {
            Name = _trackEventName,
            UserId = _userId,
            CompanyId = _companyId,
            Attributes = new Dictionary<string, object?> { ["custom-attribute"] = "custom-value" },
        });
    }

    #endregion

    #region GetFeaturesAsync Tests

    [Fact(Timeout = _timeout)]
    public async Task GetFeaturesAsync_InOfflineMode_WithoutAnyFeatures_ReturnsNothing_Async()
    {
        // Arrange
        var client = new FeatureClient(_offlineConfiguration, _httpClient, _mockLogger);
        var context = new Context { User = new User(_userId) };

        // Act
        var features = await client.GetFeaturesAsync(context);

        // Assert
        Assert.Empty(features);
    }

    [Fact(Timeout = _timeout)]
    public async Task GetFeaturesAsync_InOfflineMode_WithLocalFeaturesResolversThatReturnNothing_ReturnsNothing_Async()
    {
        // Arrange
        var client = new FeatureClient(_offlineConfiguration, _httpClient, _mockLogger, [
            _mockResolveFeatureDelegate1.Object,
            _mockResolveFeatureDelegate2.Object,
        ]);

        var context = new Context { User = new User(_userId) };

        // Act
        var features = await client.GetFeaturesAsync(context);

        // Assert
        Assert.Empty(features);

        _mockResolveFeatureDelegate1.Verify(
            v => v.Invoke(context, It.IsAny<CancellationToken>()),
            Times.Once()
        );
        _mockResolveFeatureDelegate2.Verify(
            v => v.Invoke(context, It.IsAny<CancellationToken>()),
            Times.Once()
        );
    }

    [Fact(Timeout = _timeout)]
    public async Task GetFeaturesAsync_InOfflineMode_WithLocalFeaturesResolvers_ReturnsLocalFeatures_Async()
    {
        // Arrange
        _ = _mockResolveFeatureDelegate1.Setup(m => m.Invoke(It.IsAny<Context>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync([
            new EvaluatedFeature("feature-1", true),
        ]);
        _ = _mockResolveFeatureDelegate2.Setup(m => m.Invoke(It.IsAny<Context>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync([
            new EvaluatedFeature("feature-2", false),
        ]);

        var client = new FeatureClient(_offlineConfiguration, _httpClient, _mockLogger, [
            _mockResolveFeatureDelegate1.Object,
            _mockResolveFeatureDelegate2.Object,
        ]);

        var context = new Context { User = new User(_userId) };

        // Act
        var features = await client.GetFeaturesAsync(context);

        // Assert
        Assert.Equal(2, features.Count);

        Assert.True(features["feature-1"].Enabled);
        Assert.False(features["feature-2"].Enabled);
    }

    [Fact(Timeout = _timeout)]
    public async Task GetFeaturesAsync_InOfflineMode_WithLocalFeaturesResolvers_RespectsOverrides_Async()
    {
        // Arrange
        _ = _mockResolveFeatureDelegate1.Setup(m => m.Invoke(It.IsAny<Context>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync([
            new EvaluatedFeature("feature-1", true),
            new EvaluatedFeature("feature-2", false),
        ]);
        _ = _mockResolveFeatureDelegate2.Setup(m => m.Invoke(It.IsAny<Context>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync([
            new EvaluatedFeature("feature-2", true),
            new EvaluatedFeature("feature-1", false, true),
        ]);

        var client = new FeatureClient(_offlineConfiguration, _httpClient, _mockLogger, [
            _mockResolveFeatureDelegate1.Object,
            _mockResolveFeatureDelegate2.Object,
        ]);

        var context = new Context { User = new User(_userId) };

        // Act
        var features = await client.GetFeaturesAsync(context);

        // Assert
        Assert.Equal(2, features.Count);

        Assert.False(features["feature-1"].Enabled);
        Assert.False(features["feature-2"].Enabled);
    }

    [Fact(Timeout = _timeout)]
    public async Task GetFeaturesAsync_LetsExceptionsThrough_FromLocalFeaturesResolvers_Async()
    {
        // Arrange
        _ = _mockResolveFeatureDelegate1.Setup(m => m.Invoke(It.IsAny<Context>(), It.IsAny<CancellationToken>())
        ).ThrowsAsync(new InvalidCastException());

        var client = new FeatureClient(_offlineConfiguration, _httpClient, _mockLogger, [
            _mockResolveFeatureDelegate1.Object,
        ]);

        var context = new Context { User = new User(_userId) };

        // Act & Assert
        _ = await Assert.ThrowsAsync<InvalidCastException>(() => client.GetFeaturesAsync(context));
    }

    [Fact(Timeout = _timeout)]
    public async Task GetFeaturesAsync_InRemoteMode_ContactsTheServer_AndReturnsEvaluatedFeatures_Async()
    {
        // Arrange
        var client = new FeatureClient(_remoteConfiguration, _httpClient, _mockLogger);

        var context = new Context { User = new User(_userId) };

        SetupFeaturesEvaluatedEndpoint("response.features-evaluated.full");

        // Act
        var features = await client.GetFeaturesAsync(context);

        // Assert
        Assert.Equal(3, features.Count);

        AssertEvaluatedFeaturesEqual(
            new EvaluatedFeature(
                "feature-1",
                false,
                ("gpt-4.5-mini", new
                {
                    maxTokens = 2000
                }.AsJsonElement())
                )
            {
                EvaluationContext = new Dictionary<string, object?> { ["user.id"] = _userId },
                FlagEvaluationDebugData = new EvaluationDebugData
                {
                    Version = 19,
                    EvaluatedRules = [true],
                    EvaluationIssues =
                    [
                        (EvaluationIssueType.MissingField, "user.name"),
                        (EvaluationIssueType.MissingField, "other.level"),
                    ],
                },
                ConfigEvaluationDebugData = new EvaluationDebugData
                {
                    Version = 2,
                    EvaluatedRules = [true],
                    EvaluationIssues =
                    [
                        (EvaluationIssueType.MissingField, "company.tier"),
                    ],
                },
            },
            features["feature-1"]
        );

        AssertEvaluatedFeaturesEqual(
            new EvaluatedFeature("feature-2", true)
            {
                EvaluationContext = new Dictionary<string, object?> { ["user.id"] = _userId },
                FlagEvaluationDebugData = new EvaluationDebugData
                {
                    Version = 11,
                    EvaluatedRules = [],
                    EvaluationIssues = [],
                },
                ConfigEvaluationDebugData = null,
            },
            features["feature-2"]
        );

        AssertEvaluatedFeaturesEqual(
            new EvaluatedFeature("feature-3", true, ("variant-b", default))
            {
                EvaluationContext = new Dictionary<string, object?> { ["user.id"] = _userId },
                FlagEvaluationDebugData =
                    new EvaluationDebugData { Version = 1, EvaluatedRules = [], EvaluationIssues = [] },
                ConfigEvaluationDebugData = new EvaluationDebugData
                {
                    Version = 2,
                    EvaluatedRules = [],
                    EvaluationIssues = [],
                },
            },
            features["feature-3"]
        );
    }

    [Fact(Timeout = _timeout)]
    public async Task GetFeaturesAsync_InRemoteMode_IncludesLocalFeatures_AndRespectsOverrides_Async()
    {
        // Arrange
        var client = new FeatureClient(_remoteConfiguration, _httpClient, _mockLogger, [
            _mockResolveFeatureDelegate1.Object,
        ]);

        _ = _mockResolveFeatureDelegate1
            .Setup(m => m.Invoke(It.IsAny<Context>(), It.IsAny<CancellationToken>())
            ).ReturnsAsync([
                new EvaluatedFeature("feature-1", true, true),
                new EvaluatedFeature("feature-2", true),
                new EvaluatedFeature("feature-4", true),
            ]);

        var context = new Context { User = new User(_userId) };

        SetupFeaturesEvaluatedEndpoint("response.features-evaluated.full");

        // Act
        var features = await client.GetFeaturesAsync(context);

        // Assert
        Assert.Equal(4, features.Count);

        AssertEvaluatedFeaturesEqual(
            new EvaluatedFeature("feature-1", true, true)
            {
                EvaluationContext = new Dictionary<string, object?> { ["user.id"] = _userId },
                FlagEvaluationDebugData = new EvaluationDebugData
                {
                    Version = 19,
                    EvaluatedRules = [true],
                    EvaluationIssues =
                    [
                        (EvaluationIssueType.MissingField, "user.name"),
                        (EvaluationIssueType.MissingField, "other.level"),
                    ],
                },
                ConfigEvaluationDebugData = new EvaluationDebugData
                {
                    Version = 2,
                    EvaluatedRules = [true],
                    EvaluationIssues =
                    [
                        (EvaluationIssueType.MissingField, "company.tier"),
                    ],
                },
            },
            features["feature-1"]
        );

        AssertEvaluatedFeaturesEqual(
            new EvaluatedFeature("feature-2", true)
            {
                EvaluationContext = new Dictionary<string, object?> { ["user.id"] = _userId },
                FlagEvaluationDebugData = new EvaluationDebugData
                {
                    Version = 11,
                    EvaluatedRules = [],
                    EvaluationIssues = [],
                },
                ConfigEvaluationDebugData = null,
            },
            features["feature-2"]
        );

        AssertEvaluatedFeaturesEqual(
            new EvaluatedFeature("feature-3", true, ("variant-b", default))
            {
                EvaluationContext = new Dictionary<string, object?> { ["user.id"] = "test-user-id" },
                FlagEvaluationDebugData =
                    new EvaluationDebugData { Version = 1, EvaluatedRules = [], EvaluationIssues = [] },
                ConfigEvaluationDebugData = new EvaluationDebugData
                {
                    Version = 2,
                    EvaluatedRules = [],
                    EvaluationIssues = [],
                },
            },
            features["feature-3"]
        );

        AssertEvaluatedFeaturesEqual(
            new EvaluatedFeature("feature-4", true)
            {
                EvaluationContext = new Dictionary<string, object?> { ["user.id"] = "test-user-id" },
                FlagEvaluationDebugData = null,
                ConfigEvaluationDebugData = null,
            },
            features["feature-4"]
        );
    }

    [Fact(Timeout = _timeout)]
    public async Task GetFeaturesAsync_InLocalMode_EvaluatesFilters_WhenFiltersMatch_Async()
    {
        // Arrange
        var client = new FeatureClient(_localConfiguration, _httpClient, _mockLogger);

        var context = new Context
        {
            User = new User(_userId) { Name = "alex" },
            Company = new Company(_companyId) { Name = "bucket" },
        };

        SetupFeaturesDefinitionsEndpoint("response.features-definitions.full");

        // Act
        var features = await client.GetFeaturesAsync(context);

        // Assert
        Assert.Equal(2, features.Count);

        AssertEvaluatedFeaturesEqual(
            new EvaluatedFeature(
                "feature-1",
                true,
                ("variant-1", new
                {
                    some = "value"
                }.AsJsonElement())
                )
            {
                EvaluationContext = new Dictionary<string, object?>
                {
                    ["company.id"] = _companyId,
                    ["company.name"] = "bucket",
                    ["user.id"] = _userId,
                    ["user.name"] = "alex",
                },
                FlagEvaluationDebugData = new EvaluationDebugData
                {
                    Version = 2,
                    EvaluatedRules = [true],
                    EvaluationIssues =
                    [
                    ],
                },
                ConfigEvaluationDebugData = new EvaluationDebugData
                {
                    Version = 3,
                    EvaluatedRules = [true],
                    EvaluationIssues =
                    [
                    ],
                },
            },
            features["feature-1"]
        );

        AssertEvaluatedFeaturesEqual(
            new EvaluatedFeature("feature-2", false)
            {
                EvaluationContext = new Dictionary<string, object?>
                {
                    ["company.id"] = _companyId,
                    ["company.name"] = "bucket",
                    ["user.id"] = _userId,
                    ["user.name"] = "alex",
                },
                FlagEvaluationDebugData = new EvaluationDebugData
                {
                    Version = 3,
                    EvaluatedRules = [],
                    EvaluationIssues = [],
                },
                ConfigEvaluationDebugData = null,
            },
            features["feature-2"]
        );
    }

    [Fact(Timeout = _timeout)]
    public async Task GetFeaturesAsync_InLocalMode_EvaluatesFilters_WhenFiltersDoNotMatch_Async()
    {
        // Arrange
        var client = new FeatureClient(_localConfiguration, _httpClient, _mockLogger);

        var context = new Context { User = new User(_userId) };

        SetupFeaturesDefinitionsEndpoint("response.features-definitions.full");

        // Act
        var features = await client.GetFeaturesAsync(context);

        // Assert
        Assert.Equal(2, features.Count);

        AssertEvaluatedFeaturesEqual(
            new EvaluatedFeature(
                "feature-1",
                false)
            {
                EvaluationContext = new Dictionary<string, object?> { ["user.id"] = _userId },
                FlagEvaluationDebugData = new EvaluationDebugData
                {
                    Version = 2,
                    EvaluatedRules = [false],
                    EvaluationIssues =
                    [
                        (EvaluationIssueType.MissingField, "company.name"),
                    ],
                },
                ConfigEvaluationDebugData = new EvaluationDebugData
                {
                    Version = 3,
                    EvaluatedRules = [false],
                    EvaluationIssues =
                    [
                        (EvaluationIssueType.MissingField, "user.name"),
                    ],
                },
            },
            features["feature-1"]
        );

        AssertEvaluatedFeaturesEqual(
            new EvaluatedFeature("feature-2", false)
            {
                EvaluationContext = new Dictionary<string, object?> { ["user.id"] = _userId },
                FlagEvaluationDebugData = new EvaluationDebugData
                {
                    Version = 3,
                    EvaluatedRules = [],
                    EvaluationIssues = [],
                },
                ConfigEvaluationDebugData = null,
            },
            features["feature-2"]
        );
    }

    [Fact(Timeout = _timeout)]
    public async Task GetFeaturesAsync_InLocalMode_PullsDefinitionsOnce_Async()
    {
        // Arrange
        var client = new FeatureClient(_localConfiguration, _httpClient, _mockLogger);

        var context = new Context { User = new User(_userId) };

        SetupFeaturesDefinitionsEndpoint("response.features-definitions.full");

        // Act
        _ = await client.GetFeaturesAsync(context);
        _ = await client.GetFeaturesAsync(context);
    }

    #endregion

    #region GetFeatureAsync Tests

    [Fact(Timeout = _timeout)]
    public async Task GetFeatureAsync_ReturnsShim_WhenFeatureIsNotReal_Async()
    {
        // Arrange
        var client = SetupClientAndGetFeature(OperationMode.Offline);
        var context = new Context { User = new User(_userId) };

        // Act
        var feature = await client.GetFeatureAsync("feature-1", context);

        // Assert
        Assert.Equal("feature-1", feature.Key);
        Assert.False(feature.Enabled);
    }

    [Fact(Timeout = _timeout)]
    public async Task GetFeatureGenericAsync_ReturnsShim_WhenFeatureIsNotReal_Async()
    {
        // Arrange
        var client = SetupClientAndGetFeature(OperationMode.Offline);
        var context = new Context { User = new User(_userId) };

        // Act
        var feature = await client.GetFeatureAsync<object>("feature-1", context);

        // Assert
        Assert.Equal("feature-1", feature.Key);
        Assert.False(feature.Enabled);
        Assert.Null(feature.Config.Key);
        Assert.Null(feature.Config.Payload);
    }

    [Theory(Timeout = _timeout)]
    [InlineData(OperationMode.Offline, TrackingStrategy.Default, false)]
    [InlineData(OperationMode.Offline, TrackingStrategy.Active, false)]
    [InlineData(OperationMode.Offline, TrackingStrategy.Inactive, false)]
    [InlineData(OperationMode.Offline, TrackingStrategy.Disabled, false)]
    [InlineData(OperationMode.RemoteEvaluation, TrackingStrategy.Default, true)]
    [InlineData(OperationMode.RemoteEvaluation, TrackingStrategy.Active, true)]
    [InlineData(OperationMode.RemoteEvaluation, TrackingStrategy.Inactive, true)]
    [InlineData(OperationMode.RemoteEvaluation, TrackingStrategy.Disabled, false)]
    [InlineData(OperationMode.LocalEvaluation, TrackingStrategy.Default, true)]
    [InlineData(OperationMode.LocalEvaluation, TrackingStrategy.Active, true)]
    [InlineData(OperationMode.LocalEvaluation, TrackingStrategy.Inactive, true)]
    [InlineData(OperationMode.LocalEvaluation, TrackingStrategy.Disabled, false)]
    public async Task GetFeatureAsync_ShimFeature_Enabled_ConformsToSpecifications_Async(
        OperationMode mode,
        TrackingStrategy trackingStrategy,
        bool emitsFeatureEvent
    )
    {
        // Arrange
        var client = SetupClientAndGetFeature(mode);
        var context = new Context { User = new User(_userId) };

        // Act
        var feature = await client.GetFeatureAsync(_featureKey, context, trackingStrategy);
        Assert.False(feature.Enabled);

        await client.FlushAsync();

        // Assert
        if (emitsFeatureEvent)
        {
            await VerifyOutputAsync(new UserMessage
            {
                UserId = _userId,
                Attributes = new Dictionary<string, object?>(),
                Metadata = trackingStrategy switch
                {
                    TrackingStrategy.Active => new TrackingMetadata { Active = true },
                    TrackingStrategy.Inactive => new TrackingMetadata { Active = false },
                    TrackingStrategy.Default => null,
                    TrackingStrategy.Disabled => null,
                    _ => null,
                },
            },
            new FeatureEventMessage
            {
                SubType = FeatureEventType.CheckFlag,
                FeatureKey = _featureKey,
                EvaluationResult = false.AsJsonElement(),
            });
        }
    }

    [Theory(Timeout = _timeout)]
    [InlineData(OperationMode.Offline, TrackingStrategy.Default, false)]
    [InlineData(OperationMode.Offline, TrackingStrategy.Active, false)]
    [InlineData(OperationMode.Offline, TrackingStrategy.Inactive, false)]
    [InlineData(OperationMode.Offline, TrackingStrategy.Disabled, false)]
    [InlineData(OperationMode.RemoteEvaluation, TrackingStrategy.Default, true)]
    [InlineData(OperationMode.RemoteEvaluation, TrackingStrategy.Active, true)]
    [InlineData(OperationMode.RemoteEvaluation, TrackingStrategy.Inactive, true)]
    [InlineData(OperationMode.RemoteEvaluation, TrackingStrategy.Disabled, false)]
    [InlineData(OperationMode.LocalEvaluation, TrackingStrategy.Default, true)]
    [InlineData(OperationMode.LocalEvaluation, TrackingStrategy.Active, true)]
    [InlineData(OperationMode.LocalEvaluation, TrackingStrategy.Inactive, true)]
    [InlineData(OperationMode.LocalEvaluation, TrackingStrategy.Disabled, false)]
    public async Task GetFeatureGenericAsync_ShimFeature_Enabled_ConformsToSpecifications_Async(
        OperationMode mode,
        TrackingStrategy trackingStrategy,
        bool emitsFeatureEvent
    )
    {
        // Arrange
        var client = SetupClientAndGetFeature(mode);
        var context = new Context { User = new User(_userId) };

        // Act
        var feature = await client.GetFeatureAsync<object>(_featureKey, context, trackingStrategy);

        // Run pending tasks to ensure the message is sent
        await MockTime.AdvanceTimeAsync();

        // Flush bulk messages
        await client.FlushAsync();

        // Assert
        Assert.False(feature.Enabled);

        // Run pending tasks to ensure the message is sent
        await MockTime.AdvanceTimeAsync();

        // Flush bulk messages
        await client.FlushAsync();

        // Assert
        if (emitsFeatureEvent)
        {
            await VerifyOutputAsync(new UserMessage
            {
                UserId = _userId,
                Attributes = new Dictionary<string, object?>(),
                Metadata = trackingStrategy switch
                {
                    TrackingStrategy.Active => new TrackingMetadata { Active = true },
                    TrackingStrategy.Inactive => new TrackingMetadata { Active = false },
                    TrackingStrategy.Default => null,
                    TrackingStrategy.Disabled => null,
                    _ => null,
                },
            },
            new FeatureEventMessage
            {
                SubType = FeatureEventType.CheckFlag,
                FeatureKey = _featureKey,
                EvaluationResult = false.AsJsonElement(),
            });
        }
    }

    [Theory(Timeout = _timeout)]
    [InlineData(OperationMode.Offline, TrackingStrategy.Default, false)]
    [InlineData(OperationMode.Offline, TrackingStrategy.Active, false)]
    [InlineData(OperationMode.Offline, TrackingStrategy.Inactive, false)]
    [InlineData(OperationMode.Offline, TrackingStrategy.Disabled, false)]
    [InlineData(OperationMode.RemoteEvaluation, TrackingStrategy.Default, true)]
    [InlineData(OperationMode.RemoteEvaluation, TrackingStrategy.Active, true)]
    [InlineData(OperationMode.RemoteEvaluation, TrackingStrategy.Inactive, true)]
    [InlineData(OperationMode.RemoteEvaluation, TrackingStrategy.Disabled, false)]
    [InlineData(OperationMode.LocalEvaluation, TrackingStrategy.Default, true)]
    [InlineData(OperationMode.LocalEvaluation, TrackingStrategy.Active, true)]
    [InlineData(OperationMode.LocalEvaluation, TrackingStrategy.Inactive, true)]
    [InlineData(OperationMode.LocalEvaluation, TrackingStrategy.Disabled, false)]
    public async Task GetFeatureGenericAsync_ShimFeature_Config_ConformsToSpecifications_Async(
        OperationMode mode,
        TrackingStrategy trackingStrategy,
        bool emitsFeatureEvent
    )
    {
        // Arrange
        var client = SetupClientAndGetFeature(mode);
        var context = new Context { User = new User(_userId) };

        // Act
        var feature = await client.GetFeatureAsync<object>(_featureKey, context, trackingStrategy);
        Assert.Null(feature.Config.Key);

        // Run pending tasks to ensure the message is sent
        await MockTime.AdvanceTimeAsync();

        // Flush bulk messages
        await client.FlushAsync();

        // Assert
        if (emitsFeatureEvent)
        {
            await VerifyOutputAsync(
                new UserMessage
                {
                    UserId = _userId,
                    Attributes = new Dictionary<string, object?>(),
                    Metadata = TrackingStrategyTrackingMetadata(trackingStrategy)

                },
                new FeatureEventMessage
                {
                    SubType = FeatureEventType.CheckConfig,
                    FeatureKey = _featureKey,
                    EvaluationResult = new { }.AsJsonElement(),
                }
            );
        }
    }

    [Theory(Timeout = _timeout)]
    [InlineData(OperationMode.Offline, TrackingStrategy.Default, false)]
    [InlineData(OperationMode.Offline, TrackingStrategy.Active, false)]
    [InlineData(OperationMode.Offline, TrackingStrategy.Inactive, false)]
    [InlineData(OperationMode.Offline, TrackingStrategy.Disabled, false)]
    [InlineData(OperationMode.RemoteEvaluation, TrackingStrategy.Default, true)]
    [InlineData(OperationMode.RemoteEvaluation, TrackingStrategy.Active, true)]
    [InlineData(OperationMode.RemoteEvaluation, TrackingStrategy.Inactive, true)]
    [InlineData(OperationMode.RemoteEvaluation, TrackingStrategy.Disabled, false)]
    [InlineData(OperationMode.LocalEvaluation, TrackingStrategy.Default, true)]
    [InlineData(OperationMode.LocalEvaluation, TrackingStrategy.Active, true)]
    [InlineData(OperationMode.LocalEvaluation, TrackingStrategy.Inactive, true)]
    [InlineData(OperationMode.LocalEvaluation, TrackingStrategy.Disabled, false)]
    public async Task GetFeatureAsync_ShimFeature_Track_ConformsToSpecifications_Async(
        OperationMode mode,
        TrackingStrategy trackingStrategy,
        bool emitsEvent
    )
    {
        // Arrange
        var client = SetupClientAndGetFeature(mode);
        var context = new Context { User = new User(_userId) };

        // Act
        var feature = await client.GetFeatureAsync(_featureKey, context, trackingStrategy);

        // Run pending tasks to ensure the message is sent
        await MockTime.AdvanceTimeAsync();

        // Flush bulk messages
        await client.FlushAsync();

        // Track and flush
        feature.Track();

        // Run pending tasks to ensure the message is sent
        await MockTime.AdvanceTimeAsync();

        // Flush bulk messages
        await client.FlushAsync();

        // Assert
        if (emitsEvent)
        {
            await VerifyOutputAsync(
                new UserMessage
                {
                    UserId = _userId,
                    Attributes = new Dictionary<string, object?>(),
                    Metadata = TrackingStrategyTrackingMetadata(trackingStrategy)
                },
                new TrackEventMessage
                {
                    Name = _featureKey,
                    UserId = _userId,
                    Attributes = new Dictionary<string, object?>(),
                    Metadata = TrackingStrategyTrackingMetadata(trackingStrategy)
                }
            );
        }
    }

    [Theory(Timeout = _timeout)]
    [InlineData(OperationMode.Offline, TrackingStrategy.Default, false)]
    [InlineData(OperationMode.Offline, TrackingStrategy.Active, false)]
    [InlineData(OperationMode.Offline, TrackingStrategy.Inactive, false)]
    [InlineData(OperationMode.Offline, TrackingStrategy.Disabled, false)]
    [InlineData(OperationMode.RemoteEvaluation, TrackingStrategy.Default, true)]
    [InlineData(OperationMode.RemoteEvaluation, TrackingStrategy.Active, true)]
    [InlineData(OperationMode.RemoteEvaluation, TrackingStrategy.Inactive, true)]
    [InlineData(OperationMode.RemoteEvaluation, TrackingStrategy.Disabled, false)]
    [InlineData(OperationMode.LocalEvaluation, TrackingStrategy.Default, true)]
    [InlineData(OperationMode.LocalEvaluation, TrackingStrategy.Active, true)]
    [InlineData(OperationMode.LocalEvaluation, TrackingStrategy.Inactive, true)]
    [InlineData(OperationMode.LocalEvaluation, TrackingStrategy.Disabled, false)]
    public async Task GetFeatureGenericAsync_ShimFeature_Track_ConformsToSpecifications_Async(
        OperationMode mode,
        TrackingStrategy trackingStrategy,
        bool emitsEvent
    )
    {
        // Arrange
        var client = SetupClientAndGetFeature(mode);
        var context = new Context { User = new User(_userId) };

        // Act
        var feature = await client.GetFeatureAsync<object>(_featureKey, context, trackingStrategy);

        // Flush bulk messages
        await client.FlushAsync();

        // Track and flush
        feature.Track();

        // Run pending tasks to ensure the message is sent
        await MockTime.AdvanceTimeAsync();

        // Flush bulk messages
        await client.FlushAsync();

        // Assert
        if (emitsEvent)
        {
            await VerifyOutputAsync(
                new UserMessage
                {
                    UserId = _userId,
                    Attributes = new Dictionary<string, object?>(),
                    Metadata = TrackingStrategyTrackingMetadata(trackingStrategy)
                },
                new TrackEventMessage
                {
                    Name = _featureKey,
                    UserId = _userId,
                    Attributes = new Dictionary<string, object?>(),
                    Metadata = TrackingStrategyTrackingMetadata(trackingStrategy)
                }
            );
        }
    }

    [Fact(Timeout = _timeout)]
    public async Task GetFeatureAsync_InLocalMode_ConformsToSpecifications_Async()
    {
        // Arrange
        var client = new FeatureClient(_localConfiguration, _httpClient, _mockLogger);

        var context = new Context
        {
            User = new User(_userId) { Name = "alex" },
            Company = new Company(_companyId) { Name = "Acme" },
        };

        SetupFeaturesDefinitionsEndpoint("response.features-definitions.full");

        // Act
        var feature = await client.GetFeatureAsync("feature-1", context);

        // Flush bulk messages
        _ = feature.Enabled;

        // Run pending tasks to ensure the message is sent
        await MockTime.AdvanceTimeAsync();

        // Flush bulk messages
        await client.FlushAsync();

        // Track and flush
        feature.Track();

        // Run pending tasks to ensure the message is sent
        await MockTime.AdvanceTimeAsync();

        // Flush bulk messages
        await client.FlushAsync();

        var contextFields = new Dictionary<string, object?>
        {
            ["user.id"] = _userId,
            ["user.name"] = "alex",
            ["company.id"] = _companyId,
            ["company.name"] = "Acme",
        };

        // Assert
        await VerifyOutputAsync(
            new UserMessage
            {
                UserId = _userId,
                Attributes = new Dictionary<string, object?> { ["name"] = "alex" }
            },
            new CompanyMessage
            {
                CompanyId = _companyId,
                UserId = _userId,
                Attributes = new Dictionary<string, object?> { ["name"] = "Acme" },
            },
            new FeatureEventMessage
            {
                SubType = FeatureEventType.EvaluateFlag,
                FeatureKey = "feature-1",
                EvaluationResult = false.AsJsonElement(),
                TargetingVersion = 2,
                Context = contextFields,
                EvaluatedRules = [false],
                MissingFields = [],
            },
            new FeatureEventMessage
            {
                SubType = FeatureEventType.EvaluateConfig,
                FeatureKey = "feature-1",
                EvaluationResult = new { key = "variant-1", payload = new { some = "value" } }.AsJsonElement(),
                TargetingVersion = 3,
                Context = contextFields,
                EvaluatedRules = [true],
                MissingFields = [],
            },
            new FeatureEventMessage
            {
                SubType = FeatureEventType.EvaluateFlag,
                FeatureKey = "feature-2",
                EvaluationResult = false,
                TargetingVersion = 3,
                Context = contextFields,
                EvaluatedRules = [],
                MissingFields = [],
            },
            new FeatureEventMessage
            {
                SubType = FeatureEventType.CheckFlag,
                FeatureKey = "feature-1",
                EvaluationResult = false,
                TargetingVersion = 2,
                Context = contextFields,
                EvaluatedRules = [false],
                MissingFields = [],
            },
            new TrackEventMessage
            {
                Name = "feature-1",
                UserId = _userId,
                CompanyId = _companyId,
                Attributes = new Dictionary<string, object?>(),
            }
        );
    }

    [Fact(Timeout = _timeout)]
    public async Task GetFeatureGenericAsync_InLocalMode_ConformsToSpecifications_Async()
    {
        // Arrange
        var client = new FeatureClient(_localConfiguration, _httpClient, _mockLogger);

        var context = new Context
        {
            User = new User(_userId) { Name = "alex" },
            Company = new Company(_companyId) { Name = "Acme" },
        };

        SetupFeaturesDefinitionsEndpoint("response.features-definitions.full");

        // Act
        var feature = await client.GetFeatureAsync<JsonElement>("feature-1", context);
        var isEnabled = feature.Enabled;
        var (key, payload) = feature.Config;

        // Run pending tasks to ensure the message is sent
        await MockTime.AdvanceTimeAsync();

        // Flush bulk messages
        await client.FlushAsync();

        // Track and flush
        feature.Track();

        // Run pending tasks to ensure the message is sent
        await MockTime.AdvanceTimeAsync();

        // Flush bulk messages
        await client.FlushAsync();

        // Assert
        Assert.False(isEnabled);
        Assert.Equal("variant-1", key);
        JsonAssert.Equivalent(payload, new
        {
            some = "value"
        });

        var contextFields = new Dictionary<string, object?>
        {
            ["user.id"] = _userId,
            ["user.name"] = "alex",
            ["company.id"] = _companyId,
            ["company.name"] = "Acme",
        };

        // Assert
        await VerifyOutputAsync(
            new UserMessage
            {
                UserId = _userId,
                Attributes = new Dictionary<string, object?> { ["name"] = "alex" }
            },
            new CompanyMessage
            {
                CompanyId = _companyId,
                UserId = _userId,
                Attributes = new Dictionary<string, object?> { ["name"] = "Acme" },
            },
            new FeatureEventMessage
            {
                SubType = FeatureEventType.EvaluateFlag,
                FeatureKey = "feature-1",
                EvaluationResult = false,
                TargetingVersion = 2,
                Context = contextFields,
                EvaluatedRules = [false],
                MissingFields = [],
            },
            new FeatureEventMessage
            {
                SubType = FeatureEventType.EvaluateConfig,
                FeatureKey = "feature-1",
                EvaluationResult = new { key = "variant-1", payload = new { some = "value" } }.AsJsonElement(),
                TargetingVersion = 3,
                Context = contextFields,
                EvaluatedRules = [true],
                MissingFields = [],
            },
            new FeatureEventMessage
            {
                SubType = FeatureEventType.EvaluateFlag,
                FeatureKey = "feature-2",
                EvaluationResult = false.AsJsonElement(),
                TargetingVersion = 3,
                Context = contextFields,
                EvaluatedRules = [],
                MissingFields = [],
            },
            new FeatureEventMessage
            {
                SubType = FeatureEventType.CheckFlag,
                FeatureKey = "feature-1",
                EvaluationResult = false.AsJsonElement(),
                TargetingVersion = 2,
                Context = contextFields,
                EvaluatedRules = [false],
                MissingFields = [],
            },
            new FeatureEventMessage
            {
                SubType = FeatureEventType.CheckConfig,
                FeatureKey = "feature-1",
                EvaluationResult = new { key = "variant-1", payload = new { some = "value" } }.AsJsonElement(),
                TargetingVersion = 3,
                Context = contextFields,
                EvaluatedRules = [true],
                MissingFields = [],
            },
            new TrackEventMessage
            {
                Name = "feature-1",
                UserId = _userId,
                CompanyId = _companyId,
                Attributes = new Dictionary<string, object?>(),
            }
        );
    }

    [Fact(Timeout = _timeout)]
    public async Task GetFeatureAsync_InRemoteMode_ConformsToSpecifications_Async()
    {
        // Arrange
        var client = new FeatureClient(_remoteConfiguration, _httpClient, _mockLogger);

        var context = new Context
        {
            User = new User(_userId) { Name = "alex" },
            Company = new Company(_companyId) { Name = "Acme" },
        };

        SetupFeaturesEvaluatedEndpoint("response.features-evaluated.full");

        // Act
        var feature = await client.GetFeatureAsync("feature-1", context);

        // Check the feature
        _ = feature.Enabled;

        // Run pending tasks to ensure the message is sent
        await MockTime.AdvanceTimeAsync();

        // Flush bulk messages
        await client.FlushAsync();

        // Track and flush
        feature.Track();

        // Run pending tasks to ensure the message is sent
        await MockTime.AdvanceTimeAsync();

        // Flush bulk messages
        await client.FlushAsync();

        var contextFields = new Dictionary<string, object?>
        {
            ["user.id"] = _userId,
            ["user.name"] = "alex",
            ["company.id"] = _companyId,
            ["company.name"] = "Acme",
        };

        // Assert
        await VerifyOutputAsync(
            new UserMessage { UserId = _userId, Attributes = new Dictionary<string, object?> { ["name"] = "alex" } },
            new CompanyMessage
            {
                CompanyId = _companyId,
                UserId = _userId,
                Attributes = new Dictionary<string, object?> { ["name"] = "Acme" },
            },
            new FeatureEventMessage
            {
                SubType = FeatureEventType.CheckFlag,
                FeatureKey = "feature-1",
                EvaluationResult = false.AsJsonElement(),
                TargetingVersion = 19,
                Context = contextFields,
                EvaluatedRules = [true, false, true],
                MissingFields = ["user.name", "other.level"],
            },
            new TrackEventMessage
            {
                Name = "feature-1",
                UserId = _userId,
                CompanyId = _companyId,
                Attributes = new Dictionary<string, object?>(),
            }
        );
    }

    [Fact(Timeout = _timeout)]
    public async Task GetFeatureGenericAsync_InRemoteMode_ConformsToSpecifications_Async()
    {
        // Arrange
        var client = new FeatureClient(_remoteConfiguration, _httpClient, _mockLogger);

        var context = new Context
        {
            User = new User(_userId) { Name = "alex" },
            Company = new Company(_companyId) { Name = "Acme" },
        };

        SetupFeaturesEvaluatedEndpoint("response.features-evaluated.full");

        // Act
        var feature = await client.GetFeatureAsync<JsonElement>("feature-1", context);
        var isEnabled = feature.Enabled;
        var (key, payload) = feature.Config;

        // Run pending tasks to ensure the message is sent
        await MockTime.AdvanceTimeAsync();

        // Flush bulk messages
        await client.FlushAsync();

        // Track and flush
        feature.Track();

        // Run pending tasks to ensure the message is sent
        await MockTime.AdvanceTimeAsync();

        // Flush bulk messages
        await client.FlushAsync();

        // Assert
        Assert.False(isEnabled);
        Assert.Equal("gpt-4.5-mini", key);
        JsonAssert.Equivalent(payload, new
        {
            maxTokens = 2000
        });

        var contextFields = new Dictionary<string, object?>
        {
            ["user.id"] = _userId,
            ["user.name"] = "alex",
            ["company.id"] = _companyId,
            ["company.name"] = "Acme",
        };

        // Assert
        await VerifyOutputAsync(
            new UserMessage { UserId = _userId, Attributes = new Dictionary<string, object?> { ["name"] = "alex" } },
            new CompanyMessage
            {
                CompanyId = _companyId,
                UserId = _userId,
                Attributes = new Dictionary<string, object?> { ["name"] = "Acme" },
            },
            new FeatureEventMessage
            {
                SubType = FeatureEventType.CheckFlag,
                FeatureKey = "feature-1",
                EvaluationResult = false.AsJsonElement(),
                TargetingVersion = 19,
                Context = contextFields,
                EvaluatedRules = [true, false, true],
                MissingFields = ["user.name", "other.level"],
            },
            new FeatureEventMessage
            {
                SubType = FeatureEventType.CheckConfig,
                FeatureKey = "feature-1",
                EvaluationResult = new { key = "gpt-4.5-mini", payload = new { maxTokens = 2000 } }.AsJsonElement(),
                TargetingVersion = 2,
                Context = contextFields,
                EvaluatedRules = [true],
                MissingFields = ["user.name", "other.level"],
            },
            new TrackEventMessage
            {
                Name = "feature-1",
                UserId = _userId,
                CompanyId = _companyId,
                Attributes = new Dictionary<string, object?>(),
            }
        );
    }

    #endregion

    #region FlushAsync Tests

    [Fact(Timeout = _timeout)]
    public async Task FlushAsync_InOfflineMode_DoesNothing_Async()
    {
        // Arrange
        var client = new FeatureClient(_offlineConfiguration, _httpClient, _mockLogger);

        // Act
        await client.UpdateUserAsync(new User(_userId));

        // Run pending tasks to ensure the message is sent
        await MockTime.AdvanceTimeAsync();

        // Flush bulk messages
        await client.FlushAsync();
    }

    [Fact(Timeout = _timeout)]
    public async Task FlushAsync_FlushesOutputMessages_Async()
    {
        // Arrange
        var client = new FeatureClient(_localConfiguration, _httpClient, _mockLogger);

        // Act
        await client.UpdateUserAsync(new User("1"));
        await client.UpdateUserAsync(new User("2"));

        // Run pending tasks to ensure the message is sent
        await MockTime.AdvanceTimeAsync();

        // Flush bulk messages
        await client.FlushAsync();

        // Assert
        await VerifyOutputAsync(
            new UserMessage { UserId = "1", Attributes = new Dictionary<string, object?>() },
            new UserMessage { UserId = "2", Attributes = new Dictionary<string, object?>() });
    }

    #endregion

    #region FlushAsync Tests

    [Fact(Timeout = _timeout)]
    public async Task RefreshAsync_InOfflineMode_DoesNothing_Async()
    {
        // Arrange
        var client = new FeatureClient(_offlineConfiguration, _httpClient, _mockLogger);

        // Act
        await client.RefreshAsync();
    }

    [Fact(Timeout = _timeout)]
    public async Task RefreshAsync_InRemoteMode_DoesNothing_Async()
    {
        // Arrange
        var client = new FeatureClient(_remoteConfiguration, _httpClient, _mockLogger);

        // Act
        await client.RefreshAsync();
    }

    [Fact(Timeout = _timeout)]
    public async Task RefreshAsync_InLocalMode_RequestsFeaturesDefinitions_AndHandlesRemoteErrorGracefully_Async()
    {
        // Arrange
        var client = new FeatureClient(_localConfiguration, _httpClient, _mockLogger);

        // Act
        await client.RefreshAsync();
    }

    [Fact(Timeout = _timeout)]
    public async Task RefreshAsync_InLocalMode_RequestsFeaturesDefinitions_Async()
    {
        // Arrange
        SetupFeaturesDefinitionsEndpoint("response.features-definitions.full");
        var client = new FeatureClient(_localConfiguration, _httpClient, _mockLogger);

        // Act
        await client.RefreshAsync();
    }

    #endregion

    #region Rate Limiting Tests

    [Fact(Timeout = _timeout)]
    public async Task UpdateUserAsync_AppliesRateLimits_Async()
    {
        // Arrange
        var client = new FeatureClient(_remoteConfiguration, _httpClient, _mockLogger);

        var user1 = new User(_userId);
        var user2 = new User(_userId);

        // Act - Send the same user multiple times
        await client.UpdateUserAsync(user1);
        await client.UpdateUserAsync(user2);

        // Run pending tasks to ensure the message is sent
        await MockTime.AdvanceTimeAsync();

        // Flush bulk messages
        await client.FlushAsync();

        // Assert - Only one message should be sent
        var expectedOutput = new UserMessage
        {
            UserId = _userId,
            Attributes = new Dictionary<string, object?>()
        };
        await VerifyOutputAsync(expectedOutput);

        // Advance time past the rate limit window
        await MockTime.AdvanceTimeAsync(_remoteConfiguration.Output.RollingWindow);
        await MockTime.AdvanceTimeAsync(TimeSpan.FromSeconds(1));

        // Act - Send the same user again after rate limit window
        await client.UpdateUserAsync(user1);

        // Run pending tasks to ensure the message is sent
        await MockTime.AdvanceTimeAsync();

        // Flush bulk messages
        await client.FlushAsync();

        // Assert - Now we should have a second message
        await VerifyOutputAsync(expectedOutput);
    }

    [Fact(Timeout = _timeout)]
    public async Task UpdateCompanyAsync_AppliesRateLimits_Async()
    {
        // Arrange
        var client = new FeatureClient(_remoteConfiguration, _httpClient, _mockLogger);

        var company1 = new Company(_companyId);
        var company2 = new Company(_companyId);

        // Act - Send the same user multiple times
        await client.UpdateCompanyAsync(company1);
        await client.UpdateCompanyAsync(company2);

        // Run pending tasks to ensure the message is sent
        await MockTime.AdvanceTimeAsync();

        // Flush bulk messages
        await client.FlushAsync();

        // Assert - Only one message should be sent
        var expectedOutput = new CompanyMessage
        {
            CompanyId = _companyId,
            Attributes = new Dictionary<string, object?>()
        };
        await VerifyOutputAsync(expectedOutput);

        // Advance time past the rate limit window
        await MockTime.AdvanceTimeAsync(_remoteConfiguration.Output.RollingWindow);
        await MockTime.AdvanceTimeAsync(TimeSpan.FromSeconds(1));

        // Act - Send the same user again after rate limit window
        await client.UpdateCompanyAsync(company1);

        // Run pending tasks to ensure the message is sent
        await MockTime.AdvanceTimeAsync();

        // Flush bulk messages
        await client.FlushAsync();

        // Assert - Now we should have a second message
        await VerifyOutputAsync(expectedOutput);
    }

    [Fact(Timeout = _timeout)]
    public async Task GetFeaturesAsync_InLocalMode_AppliesRateLimits_Async()
    {
        // Arrange
        SetupFeaturesDefinitionsEndpoint("response.features-definitions.full");

        var client = new FeatureClient(_localConfiguration, _httpClient, _mockLogger);

        var context1 = new Context
        {
            User = new User(_userId),
        };

        var context2 = new Context
        {
            User = new User(_userId),
        };

        _ = await client.GetFeaturesAsync(context1);
        _ = await client.GetFeaturesAsync(context2);

        // Run pending tasks to ensure the message is sent
        await MockTime.AdvanceTimeAsync();

        // Flush bulk messages
        await client.FlushAsync();

        var fields = context1.ToFields();

        // Assert - Only one message should be sent
        var expectedOutputs = new[]
        {
            new FeatureEventMessage
            {
                SubType = FeatureEventType.EvaluateFlag,
                FeatureKey = "feature-1",
                EvaluationResult = false.AsJsonElement(),
                TargetingVersion = 2,
                MissingFields = ["company.name"],
                EvaluatedRules = [false],
                Context = fields,
            },
            new FeatureEventMessage
            {
                SubType = FeatureEventType.EvaluateConfig,
                FeatureKey = "feature-1",
                EvaluationResult = new {}.AsJsonElement(),
                EvaluatedRules = [false],
                MissingFields = ["user.name"],
                TargetingVersion = 3,
                Context = fields
            },
            new FeatureEventMessage
            {
                SubType = FeatureEventType.EvaluateFlag,
                FeatureKey = "feature-2",
                EvaluationResult = false.AsJsonElement(),
                EvaluatedRules = [],
                MissingFields = [],
                TargetingVersion = 3,
                Context = fields
            },
        };

        await VerifyOutputAsync(expectedOutputs[0], expectedOutputs[1], expectedOutputs[2]);

        // Advance time past the rate limit window
        await MockTime.AdvanceTimeAsync(_remoteConfiguration.Output.RollingWindow);
        await MockTime.AdvanceTimeAsync(TimeSpan.FromSeconds(1));

        // Act - Send the same user again after rate limit window
        _ = await client.GetFeaturesAsync(context1);

        // Run pending tasks to ensure the message is sent
        await MockTime.AdvanceTimeAsync();

        // Flush bulk messages
        await client.FlushAsync();

        // Assert - Now we should have a second message
        await VerifyOutputAsync(expectedOutputs[0], expectedOutputs[1], expectedOutputs[2]);
    }

    #endregion
}
