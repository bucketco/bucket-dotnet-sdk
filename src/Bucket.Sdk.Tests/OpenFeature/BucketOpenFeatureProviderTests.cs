namespace Bucket.Sdk.Tests.OpenFeature;

using global::OpenFeature.Constant;
using global::OpenFeature.Model;

public sealed class BucketOpenFeatureProviderTests
{
    private const string _featureKey = "test-feature";
    private const string _configKey = "variant-a";
    private const string _eventName = "test-event";
    private static readonly JsonElement _emptyJsonPayload = new { }.AsJsonElement();

    private readonly Mock<IFeatureClient> _mockFeatureClient;
    private readonly FakeLogger<BucketOpenFeatureProvider> _fakeLogger;
    private readonly BucketOpenFeatureProvider _provider;
    private readonly Dictionary<string, EvaluatedFeature> _featuresResponse;

    public BucketOpenFeatureProviderTests()
    {
        _mockFeatureClient = new Mock<IFeatureClient>(MockBehavior.Strict);
        _fakeLogger = new FakeLogger<BucketOpenFeatureProvider>();

        _featuresResponse = [];

        _ = _mockFeatureClient
            .Setup(m => m.GetFeaturesAsync(It.IsAny<Context>()))
            .ReturnsAsync(_featuresResponse);
        _ = _mockFeatureClient
            .Setup(m => m.FlushAsync()).Returns(Task.CompletedTask);
        _ = _mockFeatureClient
            .Setup(m => m.RefreshAsync()).Returns(Task.CompletedTask);
        _ = _mockFeatureClient
            .Setup(m => m.TrackAsync(It.IsAny<Event>(), It.IsAny<UpdateStrategy>()))
            .Returns(Task.CompletedTask);

        _provider = new BucketOpenFeatureProvider(_mockFeatureClient.Object, _fakeLogger);
    }

    [Fact]
    public void GetMetadata_ReturnsCorrectMetadata()
    {
        // Act
        var metadata = _provider.GetMetadata();

        // Assert
        Assert.Equal("Bucket Feature Provider", metadata.Name);
    }

    private async Task AssertCancellationAsync(Func<CancellationToken, Task> func)
    {
        _ = _mockFeatureClient
            .Setup(m => m.GetFeaturesAsync(It.IsAny<Context>()))
            .Returns(async (Context _) =>
            {
                await Task.Delay(100);
                return _featuresResponse;
            });
        _ = _mockFeatureClient
            .Setup(m => m.FlushAsync()).Returns(Task.Delay(100));
        _ = _mockFeatureClient
            .Setup(m => m.RefreshAsync()).Returns(Task.Delay(100));
        _ = _mockFeatureClient
            .Setup(m => m.TrackAsync(It.IsAny<Event>(), It.IsAny<UpdateStrategy>()))
            .Returns(Task.Delay(100));

        var source = new CancellationTokenSource();
        var task = func(source.Token);
        await source.CancelAsync();

        _ = await Assert.ThrowsAsync<TaskCanceledException>(() => task);
    }

    [Fact]
    public async Task ResolveBooleanValueAsync_CancelsIfRequested_Async()
    {
        await AssertCancellationAsync(token => _provider.ResolveBooleanValueAsync(_featureKey, false,
            cancellationToken: token));
    }

    [Fact]
    public async Task ResolveBooleanValueAsync_WhenFeatureExists_Async()
    {
        // Arrange
        _featuresResponse[_featureKey] = new EvaluatedFeature(_featureKey, true);

        // Act
        var result = await _provider.ResolveBooleanValueAsync(_featureKey, false,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.Value);
        Assert.Equal(Reason.TargetingMatch, result.Reason);
        Assert.Equal(_featureKey, result.FlagKey);
        Assert.Null(result.Variant);
    }

    [Fact]
    public async Task ResolveBooleanValueAsync_WhenFeatureExistsWithConfig_Async()
    {
        // Arrange
        _featuresResponse[_featureKey] = new EvaluatedFeature(
            _featureKey,
            true,
            (_configKey, _emptyJsonPayload)
        );

        // Act
        var result = await _provider.ResolveBooleanValueAsync(_featureKey, false, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.Value);
        Assert.Equal(Reason.TargetingMatch, result.Reason);
        Assert.Equal(_featureKey, result.FlagKey);
        Assert.Equal(_configKey, result.Variant);
    }

    [Fact]
    public async Task ResolveBooleanValueAsync_WhenFeatureIsOverridden_Async()
    {
        // Arrange
        _featuresResponse[_featureKey] = new EvaluatedFeature(
            _featureKey,
            true,
            @override: true
        );

        // Act
        var result = await _provider.ResolveBooleanValueAsync(_featureKey, false,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.Value);
        Assert.Equal(Reason.Static, result.Reason);
    }

    [Fact]
    public async Task ResolveBooleanValueAsync_WhenFeatureDoesNotExist_Async()
    {
        // Act
        var result = await _provider.ResolveBooleanValueAsync(_featureKey, true,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.Value);
        Assert.Equal($"Feature {_featureKey} not found", result.Reason);
        Assert.Equal(ErrorType.FlagNotFound, result.ErrorType);

        _fakeLogger.Verify(LogLevel.Warning, $"evaluation failed for feature {_featureKey} with error {ErrorType.FlagNotFound}: Feature {_featureKey} not found");
    }

    [Fact]
    public async Task ResolveStringValueAsync_CancelsIfRequested_Async()
    {
        await AssertCancellationAsync(token => _provider.ResolveStringValueAsync(_featureKey, "default",
            cancellationToken: token));
    }

    [Fact]
    public async Task ResolveStringValueAsync_WhenFeatureExistsWithConfig_Async()
    {
        // Arrange
        _featuresResponse[_featureKey] = new EvaluatedFeature(
            _featureKey,
            true,
            (_configKey, _emptyJsonPayload)
        );

        // Act
        var result = await _provider.ResolveStringValueAsync(_featureKey, "default",
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(_configKey, result.Value);
        Assert.Equal(Reason.TargetingMatch, result.Reason);
        Assert.Equal(_configKey, result.Variant);
    }

    [Fact]
    public async Task ResolveStringValueAsync_WhenFeatureHasNoConfig_Async()
    {
        // Arrange
        _featuresResponse[_featureKey] = new EvaluatedFeature(_featureKey, true);

        // Act
        var result = await _provider.ResolveStringValueAsync(_featureKey, "default",
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("default", result.Value);
        Assert.Equal(Reason.Error, result.Reason);
        Assert.Equal(ErrorType.TypeMismatch, result.ErrorType);
        Assert.Equal("Feature has no config", result.ErrorMessage);
    }

    [Fact]
    public async Task ResolveStringValueAsync_WhenFeatureIsOverridden_Async()
    {
        // Arrange
        _featuresResponse[_featureKey] = new EvaluatedFeature(
            _featureKey, true, (_configKey, _emptyJsonPayload), @override: true);

        // Act
        var result = await _provider.ResolveStringValueAsync(_featureKey, "default",
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(_configKey, result.Value);
        Assert.Equal(Reason.Static, result.Reason);
    }

    [Fact]
    public async Task ResolveStringValueAsync_WhenFeatureDoesNotExist_Async()
    {
        // Act
        var result = await _provider.ResolveStringValueAsync(_featureKey, "default",
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("default", result.Value);
        Assert.Equal($"Feature {_featureKey} not found", result.Reason);
        Assert.Equal(ErrorType.FlagNotFound, result.ErrorType);

        _fakeLogger.Verify(LogLevel.Warning, $"evaluation failed for feature {_featureKey} with error {ErrorType.FlagNotFound}: Feature {_featureKey} not found");
    }

    [Fact]
    public async Task ResolveIntegerValueAsync_CancelsIfRequested_Async()
    {
        await AssertCancellationAsync(token => _provider.ResolveIntegerValueAsync(_featureKey, 0,
            cancellationToken: token));
    }

    [Fact]
    public async Task ResolveIntegerValueAsync_WhenFeatureExistsWithNumberConfig_Async()
    {
        // Arrange
        _featuresResponse[_featureKey] = new EvaluatedFeature(
            _featureKey,
            true,
            (_configKey, 1337.AsJsonElement())
        );

        // Act
        var result = await _provider.ResolveIntegerValueAsync(_featureKey, 999,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1337, result.Value);
        Assert.Equal(Reason.TargetingMatch, result.Reason);
        Assert.Equal(_configKey, result.Variant);
    }

    [Fact]
    public async Task ResolveIntegerValueAsync_WhenFeatureConfigNotNumber_Async()
    {
        // Arrange
        _featuresResponse[_featureKey] = new EvaluatedFeature(
            _featureKey,
            true,
            (_configKey, "not-a-number".AsJsonElement())
        );

        // Act
        var result = await _provider.ResolveIntegerValueAsync(_featureKey, 1337,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1337, result.Value);
        Assert.Equal(Reason.Error, result.Reason);
        Assert.Equal(ErrorType.TypeMismatch, result.ErrorType);
        Assert.Equal("Feature has no config or payload is not of type `int`", result.ErrorMessage);
        Assert.Equal(_configKey, result.Variant);
    }

    [Fact]
    public async Task ResolveIntegerValueAsync_WhenFeatureIsOverridden_Async()
    {
        // Arrange
        _featuresResponse[_featureKey] = new EvaluatedFeature(
            _featureKey, true, (_configKey, 1337.AsJsonElement()), @override: true);

        // Act
        var result = await _provider.ResolveIntegerValueAsync(_featureKey, 999,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1337, result.Value);
        Assert.Equal(Reason.Static, result.Reason);
    }

    [Fact]
    public async Task ResolveIntegerValueAsync_WhenFeatureDoesNotExist_Async()
    {
        // Act
        var result = await _provider.ResolveIntegerValueAsync(_featureKey, 999,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(999, result.Value);
        Assert.Equal($"Feature {_featureKey} not found", result.Reason);
        Assert.Equal(ErrorType.FlagNotFound, result.ErrorType);

        _fakeLogger.Verify(LogLevel.Warning, $"evaluation failed for feature {_featureKey} with error {ErrorType.FlagNotFound}: Feature {_featureKey} not found");
    }

    [Fact]
    public async Task ResolveDoubleValueAsync_CancelsIfRequested_Async()
    {
        await AssertCancellationAsync(token => _provider.ResolveDoubleValueAsync(_featureKey, 0,
            cancellationToken: token));
    }

    [Fact]
    public async Task ResolveDoubleValueAsync_WhenFeatureExistsWithNumberConfig_Async()
    {
        // Arrange
        _featuresResponse[_featureKey] = new EvaluatedFeature(
            _featureKey,
            true,
            (_configKey, 3.14.AsJsonElement())
        );

        // Act
        var result = await _provider.ResolveDoubleValueAsync(_featureKey, 999,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3.14, result.Value);
        Assert.Equal(Reason.TargetingMatch, result.Reason);
        Assert.Equal(_configKey, result.Variant);
    }

    [Fact]
    public async Task ResolveDoubleValueAsync_WhenFeatureConfigNotNumber_Async()
    {
        // Arrange
        _featuresResponse[_featureKey] = new EvaluatedFeature(
            _featureKey,
            true,
            (_configKey, "not-a-number".AsJsonElement())
        );

        // Act
        var result = await _provider.ResolveDoubleValueAsync(_featureKey, 3.14,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3.14, result.Value);
        Assert.Equal(Reason.Error, result.Reason);
        Assert.Equal(ErrorType.TypeMismatch, result.ErrorType);
        Assert.Equal("Feature has no config or payload is not of type `double`", result.ErrorMessage);
        Assert.Equal(_configKey, result.Variant);
    }

    [Fact]
    public async Task ResolveDoubleValueAsync_WhenFeatureIsOverridden_Async()
    {
        // Arrange
        _featuresResponse[_featureKey] = new EvaluatedFeature(
            _featureKey, true, (_configKey, 3.14.AsJsonElement()), @override: true);

        // Act
        var result = await _provider.ResolveDoubleValueAsync(_featureKey, 999,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3.14, result.Value);
        Assert.Equal(Reason.Static, result.Reason);
    }

    [Fact]
    public async Task ResolveDoubleValueAsync_WhenFeatureDoesNotExist_Async()
    {
        // Act
        var result = await _provider.ResolveDoubleValueAsync(_featureKey, 3.14,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3.14, result.Value);
        Assert.Equal($"Feature {_featureKey} not found", result.Reason);
        Assert.Equal(ErrorType.FlagNotFound, result.ErrorType);

        _fakeLogger.Verify(LogLevel.Warning, $"evaluation failed for feature {_featureKey} with error {ErrorType.FlagNotFound}: Feature {_featureKey} not found");
    }

    [Fact]
    public async Task ResolveStructureValueAsync_CancelsIfRequested_Async()
    {
        await AssertCancellationAsync(token => _provider.ResolveStructureValueAsync(_featureKey, new Value(),
            cancellationToken: token));
    }

    [Fact]
    public async Task ResolveStructureValueAsync_WhenFeatureExists_WithObjectConfig_Async()
    {
        // Arrange
        var payload = new
        {
            nestedString = "hello",
            nestedNumber = 42,
            nestedBool = true,
            nestedObject = new
            {
                key = "value"
            },
            nestedArray = new[] { 1, 2, 3 }
        }.AsJsonElement();

        _featuresResponse[_featureKey] = new EvaluatedFeature(
            _featureKey,
            true,
            (_configKey, payload)
        );

        // Act
        var result = await _provider.ResolveStructureValueAsync(_featureKey, new Value(),
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(Reason.TargetingMatch, result.Reason);
        Assert.Equal(_configKey, result.Variant);

        var structure = result.Value.AsStructure;

        Assert.NotNull(structure);
        Assert.Equal("hello", structure.GetValue("nestedString").AsString);
        Assert.Equal(42, structure.GetValue("nestedNumber").AsInteger);
        Assert.True(structure.GetValue("nestedBool").AsBoolean);

        var nestedObject = structure.GetValue("nestedObject").AsStructure;
        Assert.NotNull(nestedObject);
        Assert.Equal("value", nestedObject.GetValue("key").AsString);

        var array = structure.GetValue("nestedArray").AsList;
        Assert.NotNull(array);
        Assert.Equal(3, array.Count);
        Assert.Equal(1, array[0].AsInteger);
        Assert.Equal(2, array[1].AsInteger);
        Assert.Equal(3, array[2].AsInteger);
    }

    [Fact]
    public async Task ResolveStructureValueAsync_WhenFeatureExists_WithValueConfig_Async()
    {
        // Arrange
        _featuresResponse[_featureKey] = new EvaluatedFeature(
            _featureKey,
            true,
            (_configKey, "hello".AsJsonElement())
        );

        // Act
        var result = await _provider.ResolveStructureValueAsync(_featureKey, new Value(),
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(Reason.TargetingMatch, result.Reason);
        Assert.Equal(_configKey, result.Variant);
        Assert.Equal("hello", result.Value.AsString);
    }

    [Fact]
    public async Task ResolveStructureValueAsync_WhenFeatureIsOverridden_Async()
    {
        // Arrange
        _featuresResponse[_featureKey] = new EvaluatedFeature(
            _featureKey, true, (_configKey, 3.14.AsJsonElement()), @override: true);

        // Act
        var result = await _provider.ResolveStructureValueAsync(_featureKey, new Value(33),
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3.14, result.Value.AsDouble);
        Assert.Equal(Reason.Static, result.Reason);
    }

    [Fact]
    public async Task ResolveStructureValueAsync_WhenFeatureDoesNotExist_Async()
    {
        // Act
        var result = await _provider.ResolveStructureValueAsync(_featureKey, new Value(33),
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(33, result.Value.AsInteger);
        Assert.Equal($"Feature {_featureKey} not found", result.Reason);
        Assert.Equal(ErrorType.FlagNotFound, result.ErrorType);

        _fakeLogger.Verify(LogLevel.Warning, $"evaluation failed for feature {_featureKey} with error {ErrorType.FlagNotFound}: Feature {_featureKey} not found");
    }

    [Fact]
    public async Task InitializeAsync_CancelsIfRequested_Async() =>
        await AssertCancellationAsync((token) => _provider.InitializeAsync(EvaluationContext.Empty, token));

    [Fact]
    public async Task InitializeAsync_CallsRefreshAsyncAsync()
    {
        // Act
        await _provider.InitializeAsync(EvaluationContext.Empty, TestContext.Current.CancellationToken);

        // Assert
        _mockFeatureClient.Verify(x => x.RefreshAsync(), Times.Once);
    }

    [Fact]
    public async Task ShutdownAsync_CancelsIfRequested_Async() =>
        await AssertCancellationAsync(_provider.ShutdownAsync);

    [Fact]
    public async Task ShutdownAsync_CallsFlushAsyncAsync()
    {
        // Act
        await _provider.ShutdownAsync(TestContext.Current.CancellationToken);

        // Assert
        _mockFeatureClient.Verify(v => v.FlushAsync(), Times.Once);
    }

    [Fact]
    public void Track_WithValidContext_CallsTrackAsync()
    {
        // Arrange
        var evaluationContext = EvaluationContext.Builder()
            .SetTargetingKey("user-123")
            .Build();

        var trackingEventDetails = TrackingEventDetails.Builder()
            .Set("key", "value")
            .Build();

        // Act
        _provider.Track(_eventName, evaluationContext, trackingEventDetails);

        // Assert
        _mockFeatureClient.Verify(
            v => v.TrackAsync(
                It.Is<Event>(e =>
                    e.Name == _eventName &&
                    e.User.Id == "user-123" &&
                    Equals("value", e["key"])
                ),
                UpdateStrategy.Default
            ),
            Times.Once
        );
    }

    [Fact]
    public void Track_WithoutUser_LogsWarningAndDoesNotTrack()
    {
        // Arrange
        var evaluationContext = EvaluationContext.Empty;

        // Act
        _provider.Track(_eventName, evaluationContext);

        // Assert
        _mockFeatureClient.Verify(
            v => v.TrackAsync(It.IsAny<Event>(), It.IsAny<UpdateStrategy>()), Times.Never);

        _fakeLogger.Verify(LogLevel.Warning, "user is not set in context, discarded track event");
    }

    [Fact]
    public void Track_WithCompanyContext_IncludesCompanyInEvent()
    {
        // Arrange
        var evaluationContext = EvaluationContext.Builder()
            .SetTargetingKey("user-123")
            .Set("companyId", "company-456")
            .Build();

        // Act
        _provider.Track(_eventName, evaluationContext);

        // Assert
        _mockFeatureClient.Verify(
            v => v.TrackAsync(
                It.Is<Event>(e =>
                    e.Name == _eventName &&
                    e.User.Id == "user-123" &&
                    e.Company != null &&
                    e.Company.Id == "company-456"
                    ),
                UpdateStrategy.Default
            ),
            Times.Once
        );
    }
}
