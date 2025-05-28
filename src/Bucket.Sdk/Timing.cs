namespace Bucket.Sdk;

using System.Runtime.CompilerServices;

/// <summary>
///     Delegate for delaying a task.
/// </summary>
/// <param name="delay">The delay to wait.</param>
/// <param name="callerMethod">The caller method.</param>
/// <param name="callerLine">The caller line.</param>
/// <param name="cancellationToken">The cancellation token.</param>
internal delegate Task DelayAsyncDelegate(
    TimeSpan delay,
    string callerMethod,
    int callerLine,
    CancellationToken cancellationToken);

/// <summary>
///     Delegate for tracking a task.
/// </summary>
/// <param name="task">The task to track.</param>
/// <param name="callerMethod">The caller method.</param>
/// <param name="callerLine">The caller line.</param>
internal delegate void TrackTaskAsyncDelegate(
    Task task,
    string callerMethod,
    int callerLine);

/// <summary>
///     Delegate for tracking a task.
/// </summary>
/// <param name="task">The task to track.</param>
/// <param name="callerMethod">The caller method.</param>
/// <param name="callerLine">The caller line.</param>
internal delegate void TrackValueTaskAsyncDelegate(
    ValueTask task,
    string callerMethod,
    int callerLine);

/// <summary>
///     Helper class for internal operations and to aid testing.
/// </summary>
internal static class Timing
{
    private static Func<long> _getTickCountFunc = () => Environment.TickCount;
    private static DelayAsyncDelegate _trackDelayFunc =
        (delay, _, _, cancellationToken) => Task.Delay(delay, cancellationToken);
    private static TrackTaskAsyncDelegate _trackTaskFunc = (_, _, _) =>
    {
    };

    /// <summary>
    ///     Restores the system time and delay to the default values.
    /// </summary>
    public static void RestoreSystem()
    {
        _trackDelayFunc = (delay, _, _, cancellationToken) => Task.Delay(delay, cancellationToken);
        _getTickCountFunc = () => Environment.TickCount;
        _trackTaskFunc = (_, _, _) =>
        {
        };
    }

    /// <summary>
    ///     Mocks the system time and delay to the specified values.
    /// </summary>
    /// <param name="delayRegistrar">The delegate to register the delay.</param>
    /// <param name="tickCountProvider">The delegate to provide the current time in ticks.</param>
    /// <param name="trackForgottenTask">The delegate to track forgotten tasks.</param>
    public static void Mock(
        DelayAsyncDelegate delayRegistrar,
        Func<long> tickCountProvider,
        TrackTaskAsyncDelegate trackForgottenTask
    )
    {
        Debug.Assert(delayRegistrar != null);
        Debug.Assert(tickCountProvider != null);
        Debug.Assert(trackForgottenTask != null);

        _getTickCountFunc = tickCountProvider;
        _trackDelayFunc = delayRegistrar;
        _trackTaskFunc = trackForgottenTask;
    }

    /// <summary>
    ///     The static constructor is used to set the default values for the static properties.
    /// </summary>
    static Timing() => RestoreSystem();

    /// <summary>
    ///     Gets the current time in ticks.
    /// </summary>
    public static long TickCount => _getTickCountFunc();

    /// <summary>
    ///     Creates a task that will complete after the specified delay.
    /// </summary>
    /// <param name="delay">The delay to wait.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="callerMethod">The caller method (automatically provided by the compiler).</param>
    /// <param name="callerLine">The caller line (automatically provided by the compiler).</param>
    public static Task Delay(
        TimeSpan delay,
        CancellationToken cancellationToken,
        [CallerMemberName] string callerMethod = "",
        [CallerLineNumber] int callerLine = 0
    ) => _trackDelayFunc(delay, callerMethod, callerLine, cancellationToken);

    /// <summary>
    ///     Forgets the result of a task.
    /// </summary>
    /// <param name="task">The task to forget.</param>
    /// <param name="callerMethod">The caller method (automatically provided by the compiler).</param>
    /// <param name="callerLine">The caller line (automatically provided by the compiler).</param>
    public static void Forget(
        this Task task,
        [CallerMemberName] string callerMethod = "",
        [CallerLineNumber] int callerLine = 0
    ) => _trackTaskFunc(task, callerMethod, callerLine);
}
