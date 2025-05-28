namespace Bucket.Sdk;

/// <summary>
///     Contains the result of evaluating a feature.
/// </summary>
[PublicAPI]
public sealed record EvaluatedFeature
{
    /// <summary>
    ///     Creates a new instance of the <see cref="EvaluatedFeature" /> class.
    /// </summary>
    /// <param name="key">The key of the feature.</param>
    /// <param name="enabled">Indicates if the feature is enabled.</param>
    /// <param name="override">Indicates if the enabled status overrides Bucket.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key" /> is <see langword="null"/> or empty.</exception>
    public EvaluatedFeature(
        string key,
        bool enabled,
        bool @override = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        Key = key;
        Enabled = enabled;
        Override = @override;
    }

    /// <summary>
    ///     Creates a new instance of the <see cref="EvaluatedFeature" /> class.
    /// </summary>
    /// <param name="key">The key of the feature.</param>
    /// <param name="enabled">Indicates if the feature is enabled.</param>
    /// <param name="config">The feature config.</param>
    /// <param name="override">Indicates if the enabled status overrides Bucket.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key" /> is <see langword="null"/> or empty.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="config" /> key is <see langword="null"/> or empty.</exception>
    public EvaluatedFeature(
        string key,
        bool enabled,
        (string key, Any payload)? config,
        bool @override = false
    ) :
        this(key, enabled, @override)
    {
        if (config != null)
        {
            ArgumentException.ThrowIfNullOrEmpty(config.Value.key);
        }

        Config = config;
    }

    /// <summary>
    ///     The key of the feature.
    /// </summary>
    public string Key
    {
        get;
    }

    /// <summary>
    ///     The value indicating whether the feature is enabled.
    /// </summary>
    public bool Enabled
    {
        get; init;
    }

    /// <summary>
    ///     Indicates whether the feature evaluation overrides the remote value.
    /// </summary>
    public bool Override
    {
        get; init;
    }

    /// <summary>
    ///     The feature config (optional).
    /// </summary>
    public (string Key, Any Payload)? Config
    {
        get; init;
    }

    /// <summary>
    ///     The evaluation context used for the feature flag and config evaluation.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
    internal IReadOnlyDictionary<string, object?>? EvaluationContext
    {
        get; init;
    }

    /// <summary>
    ///     The feature flag evaluated rules.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
    internal EvaluationDebugData? FlagEvaluationDebugData
    {
        get; init;
    }

    /// <summary>
    ///     The feature config evaluation debug data.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
    internal EvaluationDebugData? ConfigEvaluationDebugData
    {
        get; init;
    }

    [DebuggerStepThrough]
#pragma warning disable IDE0051 // Remove unused private members
    private bool PrintMembers(StringBuilder builder)
#pragma warning restore IDE0051 // Remove unused private members
    {
        Debug.Assert(builder != null);

        var evaluationContext = EvaluationContext?.ToStringElementWise();

        _ = builder
            .Append($"Key = {Key}, Enabled = {Enabled}, Override = {Override}, Config = {Config}, EvaluationContext = {evaluationContext}")
            .Append($", FlagEvaluationDebugData = {FlagEvaluationDebugData}, ConfigEvaluationDebugData = {ConfigEvaluationDebugData}");

        return true;
    }

    /// <inheritdoc />
    [DebuggerStepThrough]
    public bool Equals(EvaluatedFeature? other) =>
        ReferenceEquals(this, other) ||
        other != null &&
        Key == other.Key &&
        Enabled == other.Enabled &&
        Override == other.Override &&
        Equals(Config, other.Config) &&
        (
            EvaluationContext != null && other.EvaluationContext != null &&
            EvaluationContext.EqualsElementWise(other.EvaluationContext) ||
            EvaluationContext == null && other.EvaluationContext == null
        ) &&
        FlagEvaluationDebugData == other.FlagEvaluationDebugData &&
        ConfigEvaluationDebugData == other.ConfigEvaluationDebugData;

    /// <inheritdoc />
    [DebuggerStepThrough]
    public override int GetHashCode() =>
        HashCode.Combine(
            Key, Enabled, Config, FlagEvaluationDebugData, ConfigEvaluationDebugData,
            EvaluationContext?.GetHashCodeElementWise()
        );
}
