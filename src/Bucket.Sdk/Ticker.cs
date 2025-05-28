namespace Bucket.Sdk;

/// <summary>
///     A delegate that represents a callback function for the Ticker class.
/// </summary>
/// <typeparam name="T">The type of value produced by the callback</typeparam>
/// <returns>A tuple containing a boolean indicating success and the value produced by the callback</returns>
internal delegate ValueTask<(bool, T?)> TickerCallback<T>();

/// <summary>
///     A struct that represents the result of a ticker callback.
/// </summary>
/// <typeparam name="T">The type of value produced by the callback</typeparam>
internal readonly struct TickerResult<T>
{
    /// <summary>
    ///     The age of the value.
    /// </summary>
    public required TimeSpan Age
    {
        get; init;
    }

    /// <summary>
    ///     The value produced by the callback.
    /// </summary>
    /// <remarks>
    ///     The value is <see langword="null"/> if <see cref="HasValue" /> is <c>false</c>.
    /// </remarks>
    public T? Value
    {
        get; init;
    }

    /// <summary>
    ///     Indicates whether the value is valid.
    /// </summary>
    public bool HasValue => Age != TimeSpan.MaxValue;
}

/// <summary>
///     A class that periodically executes a callback function at a specified interval.
///     It also caches the last successful value and its age.
/// </summary>
/// <typeparam name="T">The type of value produced by the callback</typeparam>
internal class Ticker<T>: IDisposable, IAsyncDisposable
{
    private readonly TickerCallback<T> _callback;
    private readonly TimeSpan _tickInterval;
    private CancellationTokenSource _cts;
    private int _isDisposed;
    private long _lastSuccessTick = -1;
    private T? _lastValue;
    private Task<bool> _tickTask;
    private Task? _tickTaskInner;
    private readonly Lock _tickLock = new();

    /// <summary>
    ///     Creates a new instance of the Ticker class.
    /// </summary>
    /// <param name="callback">The async callback function to execute at regular intervals.</param>
    /// <param name="tickInterval">The interval between executions.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="tickInterval" /> is out of range.</exception>
    /// \
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="callback" /> is <see langword="null"/>.</exception>
    public Ticker(TickerCallback<T> callback, TimeSpan tickInterval)
    {
        Debug.Assert(tickInterval >= TimeSpan.Zero);
        Debug.Assert(callback != null);

        _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        _tickInterval = tickInterval;

        _cts = new();
        _tickTask = RunTickerLoopAsync();
    }

    /// <summary>
    ///     Asynchronously disposes the ticker.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 1)
        {
            return;
        }

        await _cts.CancelAsync();
        if (_tickTask != null)
        {
            _ = await _tickTask;
        }

        _cts.Dispose();

        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Disposes the ticker, releasing all resources.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 1)
        {
            return;
        }

        _cts.Cancel();
        _tickTask?.Wait();
        _cts.Dispose();

        GC.SuppressFinalize(this);
    }

    private void AssertNotDisposed() => ObjectDisposedException.ThrowIf(_isDisposed == 1, this);

    private async Task<bool> RunTickerLoopAsync()
    {
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                await Timing.Delay(_tickInterval, _cts.Token);
                await ExecuteCallbackAsync();
            }
        }
        catch (Exception e)
        {
            if (e is OperationCanceledException or TaskCanceledException)
            {
                // Expected when cancellation is requested
                return false;
            }

            throw;
        }

        return true;
    }

    private async Task ExecuteCallbackAsync()
    {
        var (success, value) = await _callback();
        if (success)
        {
            _lastSuccessTick = Timing.TickCount;
            _lastValue = value;
        }
    }

    private async Task TickAsyncInnerAsync()
    {
        await _cts.CancelAsync();

        var loopEndedNaturally = await _tickTask;
        if (!loopEndedNaturally)
        {
            // Execute callback immediately.
            await ExecuteCallbackAsync();
        }

        // Restart the loop.
        if (_isDisposed == 0)
        {
            _cts = new();
            _tickTask = RunTickerLoopAsync();
        }

        lock (_tickLock)
        {
            _tickTaskInner = null;
        }
    }

    /// <summary>
    ///     Immediately executes the callback and restarts the timer.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when this instance is disposed.</exception>
    public Task TickAsync()
    {
        lock (_tickLock)
        {
            AssertNotDisposed();

            _tickTaskInner ??= TickAsyncInnerAsync();
            return _tickTaskInner;
        }
    }

    /// <summary>
    ///     Gets the last successful value and its age.
    /// </summary>
    /// <returns>A tuple with the age of the value and the value itself.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when this instance is disposed.</exception>
    public TickerResult<T> GetValue()
    {
        AssertNotDisposed();

        if (_lastSuccessTick < 0)
        {
            return new()
            {
                Age = TimeSpan.MaxValue,
                Value = default
            };
        }

        var age = Timing.TickCount - _lastSuccessTick;
        return new()
        {
            Age = TimeSpan.FromTicks(age),
            Value = _lastValue
        };
    }

    /// <summary>
    ///     Gets the last successful value and its age asynchronously.
    ///     If no value is available, it will execute the callback and return the result.
    /// </summary>
    /// <returns>A task representing the asynchronous operation that returns a tuple with age and value.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when this instance is disposed.</exception>
    public async Task<TickerResult<T>> GetValueAsync()
    {
        AssertNotDisposed();

        if (_lastSuccessTick < 0)
        {
            await TickAsync();
        }

        return GetValue();
    }

    /// <summary>
    ///     Finalizes the ticker instance.
    /// </summary>
    ~Ticker() => Dispose();
}
