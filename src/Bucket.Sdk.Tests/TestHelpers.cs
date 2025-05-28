
namespace Bucket.Sdk.Tests;

using System.Runtime.CompilerServices;

internal static class JsonAssert
{
    private static readonly EqualityComparer<JsonElement> _jsonElementEqualityComparer =
        EqualityComparer<JsonElement>.Create(
            JsonElement.DeepEquals,
            e => e.GetRawText().GetHashCode()
        );

    private static void Equivalent(JsonElement expected, JsonElement actual, Stack<string> path)
    {
        Debug.Assert(path != null);

        string strPath() => $"`{string.Join(string.Empty, path.Reverse())}`";

        var expectedValueKind = expected.ValueKind;
        if (expectedValueKind != actual.ValueKind)
        {
            Assert.Fail($"Expected value of type `{expectedValueKind.ToString().ToLowerInvariant()}` but got `{actual.ValueKind.ToString().ToLowerInvariant()}` at {strPath()}");
        }

        switch (expectedValueKind)
        {
            case JsonValueKind.Undefined or JsonValueKind.Null or JsonValueKind.False or
                JsonValueKind.True or JsonValueKind.Number or JsonValueKind.String:
                if (!_jsonElementEqualityComparer.Equals(expected, actual))
                {
                    Assert.Fail($"Expected value `{expected.ToString().ToLowerInvariant()}` but got `{actual.ToString().ToLowerInvariant()}` at {strPath()}");
                }

                break;
            case JsonValueKind.Array:
                var expectedElements = expected.EnumerateArray().ToImmutableArray();
                var actualElements = actual.EnumerateArray().ToImmutableArray();

                for (var i = 0; i < Math.Max(expectedElements.Length, actualElements.Length); i++)
                {
                    path.Push($"[{i}]");

                    Equivalent(
                        i < expectedElements.Length ? expectedElements[i] : default,
                        i < actualElements.Length ? actualElements[i] : default,
                        path
                    );

                    _ = path.Pop();
                }

                break;
            case JsonValueKind.Object:
            default:
                Debug.Assert(expectedValueKind is JsonValueKind.Object);

                var expectedPairs = expected.EnumerateObject().ToImmutableDictionary(
                    prop => prop.Name,
                    prop => prop.Value,
                    EqualityComparer<string>.Default,
                    _jsonElementEqualityComparer
                );
                var actualPairs = actual.EnumerateObject().ToImmutableDictionary(
                    prop => prop.Name,
                    prop => prop.Value,
                    EqualityComparer<string>.Default,
                    _jsonElementEqualityComparer
                );

                foreach (var key in expectedPairs.Keys.Concat(actualPairs.Keys).Distinct())
                {
                    path.Push($".{key}");

                    Equivalent(
                        expectedPairs.GetValueOrDefault(key),
                        actualPairs.GetValueOrDefault(key),
                        path
                    );

                    _ = path.Pop();
                }

                break;
        }
    }

    public static void Equivalent<T>(JsonElement expected, T? actual)
    {
        var path = new Stack<string>();
        Equivalent(expected, actual.AsJsonElement(), path);

        Debug.Assert(path.Count == 0);
    }

    public static void Equivalent(JsonElement expected, JsonElement actual, string key)
    {
        var path = new Stack<string>();
        path.Push(key);

        Equivalent(expected, actual, path);

        Debug.Assert(path.Count == 1);
    }

    public static T GetFixture<T>(string fixture)
    {
        Debug.Assert(!string.IsNullOrEmpty(fixture));

        var json = File.ReadAllText($"fixtures/{fixture}.json");
        var result = JsonSerializer.Deserialize<T>(json, JsonContext.TransferOptions);

        Debug.Assert(result != null);
        return result;
    }

    public static void EquivalentToFixture<T>(string fixture, T? @object) =>
        Equivalent(GetFixture<T>(fixture).AsJsonElement(JsonContext.TransferOptions), @object);

    public static JsonElement AsJsonElement<T>(this T? @object, JsonSerializerOptions? options = null) =>
        JsonSerializer.SerializeToElement(@object, options ?? JsonContext.PayloadOptions);
}

internal static class MockTime
{
    private static readonly SemaphoreSlim _occupied = new(1);
    private static readonly Dictionary<int, (string method, int line)> _taskMetadata = [];

    private static long _currentTick;

    private static readonly List<Task> _taskQueue = [];
    private static readonly PriorityQueue<TaskCompletionSource, long> _delayQueue = new();
    private static readonly Lock _lock = new();

    private static void AssertAllTasksFinished(IReadOnlyList<Task> tasks)
    {
        tasks = tasks.Where(t =>
            t.Status is not TaskStatus.RanToCompletion and
            not TaskStatus.Canceled
        ).ToImmutableArray();

        if (tasks.Count > 0)
        {
            var description = string.Join("\n", tasks.Select(t =>
            {
                var (method, line) = _taskMetadata.GetValueOrDefault(t.Id);
                var status = t.Status.ToString();
                if (t.Exception != null)
                {
                    status = $"{status} thrown {t.Exception.Message}";
                }

                return $"Task {t.Id} started at `{method}:{line}` with status {status})";
            }));

            Assert.Fail($"One or more tasks were not asserted:\n{description}");
        }
    }

    private static async Task RunPendingTasksAsync(int iterations = 10)
    {
        while (!TestContext.Current.CancellationToken.IsCancellationRequested)
        {
            Task[] assertTasks;
            lock (_lock)
            {
                assertTasks = [.. _taskQueue];
                _taskQueue.Clear();
            }

            if (assertTasks.Length == 0)
            {
                break;
            }

            if (--iterations == 0)
            {
                Assert.Fail("Too many iterations, possible deadlock");
            }

            try
            {
                await Task.WhenAll(assertTasks).WaitAsync(TestContext.Current.CancellationToken);
            }
            finally
            {
                AssertAllTasksFinished(assertTasks);
            }
        }
    }

    private static async Task CancelPendingDelaysAsync()
    {
        try
        {
            await RunPendingTasksAsync();

            TaskCompletionSource[] completionSources;
            lock (_lock)
            {
                completionSources = [.. _delayQueue.UnorderedItems.Select(item => item.Element)];
            }

            foreach (var completionSource in completionSources)
            {
                _ = completionSource.TrySetCanceled();
            }

            AssertAllTasksFinished(completionSources.Select(c => c.Task).ToArray());
        }
        finally
        {
            _taskMetadata.Clear();
        }
    }

    private static Task MockedDelay(
        TimeSpan wait, string callerMethod, int callerLine, CancellationToken cancellationToken)
    {
        var delayCompletionSource = new TaskCompletionSource();

        _ = cancellationToken.Register(() =>
         {
             lock (_lock)
             {
                 _ = delayCompletionSource.TrySetCanceled(TestContext.Current.CancellationToken);
             }
         });

        lock (_lock)
        {
            _delayQueue.Enqueue(delayCompletionSource, _currentTick + wait.Ticks);
            _taskMetadata[delayCompletionSource.Task.Id] = (callerMethod, callerLine);
        }

        return delayCompletionSource.Task;
    }

    private static long MockedCurrentTick() => _currentTick;

    private static void MockedTrackTask(Task task, string callerMethod, int callerLine)
    {
        lock (_lock)
        {
            _taskQueue.Add(task);
            _taskMetadata[task.Id] = (callerMethod, callerLine);
        }
    }

    public static async Task MockSystemTimeAsync()
    {
        await _occupied.WaitAsync(TestContext.Current.CancellationToken);

        await CancelPendingDelaysAsync();

        lock (_lock)
        {
            _currentTick = 0;

            Timing.Mock(
                MockedDelay,
                MockedCurrentTick,
                MockedTrackTask
            );
        }
    }

    public static async Task RestoreSystemTimeAsync()
    {
        _ = _occupied.Release();

        lock (_lock)
        {
            Timing.RestoreSystem();
        }

        await CancelPendingDelaysAsync();
    }

    public static async Task AdvanceTimeAsync(TimeSpan? delta = null)
    {
        await RunPendingTasksAsync();

        var completionSources = new List<TaskCompletionSource>();
        lock (_lock)
        {
            _currentTick += delta?.Ticks ?? 0;
            while (_delayQueue.TryPeek(out var completion, out var tick) && tick <= _currentTick)
            {
                completionSources.Add(completion);
                _ = _delayQueue.Dequeue();
            }
        }

        foreach (var completionSource in completionSources)
        {
            _ = completionSource.TrySetResult();
        }
    }

    public static void AssertForgottenException<TException>(Action forgetful)
        where TException : Exception
    {
        forgetful();

        lock (_lock)
        {
            if (_taskQueue.Count == 0)
            {
                Assert.Fail("No forgotten tasks to assert");
            }

            Exception? caughtException = null;
            try
            {
                _taskQueue[0].Wait(TestContext.Current.CancellationToken);
            }
            catch (Exception ex)
            {
                caughtException = ex is AggregateException { InnerExceptions.Count: 1 } aggregateException
                    ? aggregateException.InnerExceptions.FirstOrDefault()
                    : ex;
            }

            _ = Assert.IsType<TException>(caughtException);
            _taskQueue.RemoveAt(0);
        }
    }

    public static void AssertForgottenException<TException>(Func<object?> forgetful) where TException : Exception =>
        AssertForgottenException<TException>(() => { _ = forgetful(); });
}

internal sealed class ControlledAsyncCallback<T>: IDisposable
{
    private readonly TaskCompletionSource<T> _callbackCanComplete = new();
    private readonly TaskCompletionSource _callbackStarted = new();
    private int _completedCount;
    private int _startedCount;
    private bool _wasAsserted;

    public void Dispose()
    {
        Assert.True(_wasAsserted, "The callback was not asserted!");
        _ = _callbackCanComplete.TrySetCanceled(TestContext.Current.CancellationToken);
    }

    public async ValueTask<T> FunctionAsync()
    {
        _ = Interlocked.Increment(ref _startedCount);

        // Signal that the callback has started.
        _ = _callbackStarted.TrySetResult();

        // Wait for the test to allow the callback to complete.
        var result = await _callbackCanComplete.Task.WaitAsync(TestContext.Current.CancellationToken);

        // Mark the callback as executed.
        _ = Interlocked.Increment(ref _completedCount);
        return result;
    }

    public void AssertCompleted(int? times = null)
    {
        _wasAsserted = true;
        if (times.HasValue)
        {
            Assert.True(_completedCount == times.Value, $"Expected the callback to be complete exactly {times} times");
        }
        else
        {
            Assert.True(_completedCount > 0, "Expected the callback to be complete");
        }
    }

    public void AssertNotCompleted()
    {
        _wasAsserted = true;
        Assert.True(_completedCount == 0, "Expected the callback not to be complete");
    }

    public void AssertNotStarted()
    {
        _wasAsserted = true;
        Assert.True(_startedCount == 0, "Expected the callback not to be called");
    }

    public Task WaitForStartAsync() =>
        _callbackStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

    public void Complete(T result) => _callbackCanComplete.SetResult(result);
}

internal static class FakeLoggerExtensions
{
    public static void Verify(this FakeLogger logger, LogLevel level, [RegexPattern] string pattern)
    {
        Debug.Assert(logger != null);

        var log = logger.Collector.LatestRecord;

        Assert.Equal(level, log.Level);
        Assert.Matches(pattern, log.Message);
    }
}

internal static class SnapshotExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
    };

    [SuppressMessage("ReSharper", "ExplicitCallerInfoArgument")]
    public static async Task VerifySnapshotAsync<T>(this T @object, [CallerFilePath] string testFile = "")
    {
        var json = JsonSerializer.Serialize(@object, _jsonOptions);
        _ = await Verify(json, sourceFile: testFile)
            .UseDirectory("snapshots")
            .AddScrubber(builder => builder.Replace("\r\n", "\n").Replace("\\r\\n", "\\n"));
    }
}
