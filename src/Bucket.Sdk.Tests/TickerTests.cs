namespace Bucket.Sdk.Tests;

public sealed class TickerTests: IAsyncDisposable
{
    private const int _timeout = 1000;

    private static readonly TimeSpan _defaultTickInterval = TimeSpan.FromSeconds(10);
    private static readonly (bool success, string? value) _validResult = (true, "valid result");
    private static readonly (bool success, string? value) _invalidResult = (false, null);
    private readonly ControlledAsyncCallback<(bool, string?)> _callback;

    private readonly Ticker<string> _ticker;

    public TickerTests()
    {
        // Mock the system time.
        MockTime.MockSystemTimeAsync().Wait(TestContext.Current.CancellationToken);

        // Set up the controlled async callback.
        _callback = new ControlledAsyncCallback<(bool, string?)>();

        // Create a new ticker.
        _ticker = new Ticker<string>(_callback.FunctionAsync, _defaultTickInterval);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            _ticker.Dispose();
            _callback.Dispose();
        }
        finally
        {
            await MockTime.RestoreSystemTimeAsync();
        }
    }

    [Fact(Timeout = _timeout)]
    public void GetValue_NoSuccessfulTick_ReturnsMaxAgeAndNoValue()
    {
        var result = _ticker.GetValue();

        _callback.AssertNotCompleted();

        Assert.False(result.HasValue);
        Assert.Equal(TimeSpan.MaxValue, result.Age);
        Assert.Null(result.Value);
    }

    [Fact(Timeout = _timeout)]
    [SuppressMessage("ReSharper", "MethodHasAsyncOverload")]
    public async Task GetValue_AfterSuccessfulTick_ReturnsValue_Async()
    {
        _callback.Complete(_validResult);
        await _ticker.TickAsync();

        _callback.AssertCompleted();

        var result = _ticker.GetValue();

        Assert.True(result.HasValue);
        Assert.Equal(_validResult.value, result.Value);
    }

    [Fact(Timeout = _timeout)]
    [SuppressMessage("ReSharper", "MethodHasAsyncOverload")]
    public async Task GetValue_AfterFailedTick_ReturnsNothing_Async()
    {
        _callback.Complete(_invalidResult);
        await _ticker.TickAsync();

        _callback.AssertCompleted();

        var result = _ticker.GetValue();

        Assert.False(result.HasValue);
    }

    [Fact(Timeout = _timeout)]
    [SuppressMessage("ReSharper", "MethodHasAsyncOverload")]
    public async Task GetValue_ReportsAge_Async()
    {
        // Arrange & Act
        _callback.Complete(_validResult);
        await _ticker.TickAsync();

        _callback.AssertCompleted();

        var interval = TimeSpan.FromSeconds(3);

        // Advance by 3 seconds.
        await MockTime.AdvanceTimeAsync(interval);

        var result = _ticker.GetValue();

        // Assert
        Assert.Equal(interval, result.Age);
    }

    [Fact(Timeout = _timeout)]
    public async Task GetValueAsync_NoSuccessfulTick_TriggersTickAndReturnsResult_Async()
    {
        // Arrange & Act
        _callback.Complete(_validResult);
        var result = await _ticker.GetValueAsync();

        // Assert
        _callback.AssertCompleted();

        Assert.True(result.HasValue);
        Assert.Equal(_validResult.value, result.Value);
    }

    [Fact(Timeout = _timeout)]
    public async Task GetValueAsync_AfterFailedTick_ReturnsNothing_Async()
    {
        // Arrange & Act
        _callback.Complete(_invalidResult);
        var result = await _ticker.GetValueAsync();

        // Assert
        _callback.AssertCompleted();

        Assert.False(result.HasValue);
    }

    [Fact(Timeout = _timeout)]
    public async Task GetValueAsync_CallsDuringActiveCallback_ReturnsLatestValue_Async()
    {
        // Arrange & Act
        await MockTime.AdvanceTimeAsync(_defaultTickInterval);

        await _callback.WaitForStartAsync();

        // Complete the tick asynchronously.
        var completionTask = Task.Run(() => _callback.Complete(_validResult), TestContext.Current.CancellationToken);

        // Wait for GetValueAsync to complete (this will wait for complete).
        var result = await _ticker.GetValueAsync();

        // Make sure the completion task finishes
        await completionTask;

        // Assert
        _callback.AssertCompleted();

        Assert.True(result.HasValue);
        Assert.Equal(_validResult.value, result.Value);
    }

    [Fact(Timeout = _timeout)]
    public async Task TickAsync_CancelsCurrentDelay_AndExecutesCallback_Async()
    {
        // Act
        _callback.Complete(_validResult);
        await _ticker.TickAsync();

        // Assert
        _callback.AssertCompleted();

        // Act
        var result = await _ticker.GetValueAsync();

        // Assert
        Assert.True(result.HasValue);
        Assert.Equal(_validResult.value, result.Value);
    }

    [Fact(Timeout = _timeout)]
    public async Task TickAsync_CancelsOnceAndWaitsForRest_Async()
    {
        // Act
        var t1 = _ticker.TickAsync();
        var t2 = _ticker.TickAsync();
        var t3 = _ticker.TickAsync();

        _callback.Complete(_validResult);

        await t3;

        Assert.True(t1.IsCompletedSuccessfully);
        Assert.True(t2.IsCompletedSuccessfully);

        // Assert
        _callback.AssertCompleted(1);
    }


    [Fact(Timeout = _timeout)]
    public async Task TimedTick_ExecutesCallbackAfterInterval_Async()
    {
        // Act
        // Let the automatic tick happen.
        await MockTime.AdvanceTimeAsync(_defaultTickInterval);

        // Validate the callback started.
        await _callback.WaitForStartAsync();

        // Complete it.
        _callback.Complete(_validResult);

        // Validate the value.
        var result = await _ticker.GetValueAsync();

        // Assert
        _callback.AssertCompleted();

        Assert.True(result.HasValue);
        Assert.Equal(_validResult.value, result.Value);
    }

    [Fact(Timeout = _timeout)]
    [SuppressMessage("ReSharper", "MethodHasAsyncOverload")]
    public async Task Dispose_CancelsDelayAndPreventsOperations_Async()
    {
        // Arrange & Act
        _ticker.Dispose(); // Dispose immediately

        // Advance time and expect nothing to happen.
        await MockTime.AdvanceTimeAsync(_defaultTickInterval);

        // Assert

        _ = Assert.Throws<ObjectDisposedException>(() => _ticker.GetValue());
        _ = await Assert.ThrowsAsync<ObjectDisposedException>(_ticker.GetValueAsync);
        _ = await Assert.ThrowsAsync<ObjectDisposedException>(_ticker.TickAsync);

        _callback.AssertNotStarted();
    }

    [Fact(Timeout = _timeout)]
    [SuppressMessage("ReSharper", "MethodHasAsyncOverload")]
    public async Task Dispose_CancelsCallback_Async()
    {
        // Arrange
        await MockTime.AdvanceTimeAsync(_defaultTickInterval);

        // Wait for the callback to start.
        await _callback.WaitForStartAsync();

        // Complete the callback.
        var completionTask = Task.Run(() => _callback.Complete(_validResult), TestContext.Current.CancellationToken);

        // Dispose the ticker while the callback is expected to run.
        _ticker.Dispose();

        // Make sure the completion task finishes
        await completionTask;

        // Assert
        _callback.AssertCompleted();
    }

    [Fact(Timeout = _timeout)]
    public void Dispose_MultipleTimes_OnlyDisposesOnce()
    {
        // Act
        _ticker.Dispose();
        _ticker.Dispose();

        // Assert
        _callback.AssertNotStarted();
    }

    [Fact(Timeout = _timeout)]
    public async Task DisposeAsync_CancelsDelayAndPreventsOperations_Async()
    {
        // Act

        // Dispose immediately.
        await _ticker.DisposeAsync();

        // Advance time and expect nothing to happen.
        await MockTime.AdvanceTimeAsync(_defaultTickInterval);

        // Assert
        _ = await Assert.ThrowsAsync<ObjectDisposedException>(_ticker.GetValueAsync);
        _ = await Assert.ThrowsAsync<ObjectDisposedException>(_ticker.TickAsync);

        _callback.AssertNotStarted();
    }

    [Fact(Timeout = _timeout)]
    public async Task DisposeAsync_CancelsCallback_Async()
    {
        // Arrange & Act
        await MockTime.AdvanceTimeAsync(_defaultTickInterval);

        // Wait for the callback to start.
        await _callback.WaitForStartAsync();

        // Complete the callback asynchronously.
        var completionTask = Task.Run(() => _callback.Complete(_validResult), TestContext.Current.CancellationToken);

        // Dispose the ticker while the callback is expected to run.
        await _ticker.DisposeAsync();

        // Make sure the completion task finishes
        await completionTask;

        // Assert
        _callback.AssertCompleted();
    }

    [Fact(Timeout = _timeout)]
    public async Task DisposeAsync_MultipleTimes_OnlyDisposesOnce_Async()
    {
        // Act
        await _ticker.DisposeAsync();
        await _ticker.DisposeAsync();

        // Assert
        _callback.AssertNotStarted();
    }
}
