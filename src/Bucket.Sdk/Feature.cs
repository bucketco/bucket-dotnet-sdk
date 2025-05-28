namespace Bucket.Sdk;

/// <summary>
///     The default implementation of the <see cref="IFeature" /> interface. Used
///     internally by the SDK.
/// </summary>
internal record Feature: IFeature
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly Action _checkEnabledStateFunc;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly bool _enabled;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly Action _trackFunc;

    /// <summary>
    ///     Creates a new instance of the <see cref="Feature" /> class.
    /// </summary>
    /// <param name="key">The key of teh feature.</param>
    /// <param name="enabled">Indicates if the feature is enabled.</param>
    /// <param name="checkEnabledStateFunc">The callback used to signal if <see cref="Enabled" /> was read.</param>
    /// <param name="trackFunc">The callback used by <see cref="Track" />.</param>
    [DebuggerStepThrough]
    public Feature(
        string key,
        bool enabled,
        Action checkEnabledStateFunc,
        Action trackFunc)
    {
        Debug.Assert(checkEnabledStateFunc != null);
        Debug.Assert(trackFunc != null);
        Debug.Assert(!string.IsNullOrEmpty(key));

        Key = key;
        _checkEnabledStateFunc = checkEnabledStateFunc;
        _trackFunc = trackFunc;
        _enabled = enabled;
    }

    /// <inheritdoc cref="IFeature.Key" />
    public string Key
    {
        get;
    }

    /// <inheritdoc cref="IFeature.Enabled" />
    public bool Enabled
    {
        [DebuggerStepThrough]
        get
        {
            _checkEnabledStateFunc();
            return _enabled;
        }
    }

    /// <inheritdoc cref="IFeature.Track" />
    [DebuggerStepThrough]
    public void Track() => _trackFunc();

    /// <summary>
    ///     Prints the members of the feature.
    /// </summary>
    /// <param name="builder">The builder to print the members to.</param>
    /// <returns>
    ///     <see langword="true" /> if the members were printed; otherwise, <see langword="false" />.
    /// </returns>
    [DebuggerStepThrough]
    protected virtual bool PrintMembers(StringBuilder builder)
    {
        Debug.Assert(builder != null);

        _ = builder.Append($"Key = {Key}, Enabled = {_enabled}");

        return true;
    }

    /// <inheritdoc />
    [DebuggerStepThrough]
    public virtual bool Equals(Feature? other) =>
        ReferenceEquals(this, other) ||
        other != null &&
        Key == other.Key &&
        _enabled == other._enabled;

    /// <inheritdoc />
    [DebuggerStepThrough]
    public override int GetHashCode() =>
        HashCode.Combine(Key, _enabled);
}

/// <summary>
///     The default implementation of the <see cref="IFeature{TPayload}" /> interface. Used
///     internally by the SDK.
/// </summary>
internal sealed record Feature<TPayload>: Feature, IFeature<TPayload>
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly Action _checkConfigFunc;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly (string? Key, TPayload? Payload) _config;

    /// <summary>
    ///     Creates a new instance of the <see cref="Feature{TPayload}" /> class.
    /// </summary>
    /// <param name="key">The key of the feature.</param>
    /// <param name="isEnabled">Indicates if the feature is enabled.</param>
    /// <param name="checkEnabledStateFunc">The callback used to signal if <see cref="Feature.Enabled" /> was read.</param>
    /// <param name="checkConfigFunc">The callback used to signal if <see cref="Config" /> was read.</param>
    /// <param name="trackFunc">The callback used by <see cref="Feature.Track" />.</param>
    /// <param name="config">The configuration of the feature.</param>
    [DebuggerStepThrough]
    internal Feature(
        string key,
        bool isEnabled,
        Action checkEnabledStateFunc,
        Action checkConfigFunc,
        Action trackFunc,
        (string? Key, TPayload? payload) config
    ) : base(key, isEnabled, checkEnabledStateFunc, trackFunc)
    {
        Debug.Assert(trackFunc != null);

        _config = config;
        _checkConfigFunc = checkConfigFunc;
    }

    /// <inheritdoc cref="IFeature{TPayload}.Config" />
    public (string? Key, TPayload? Payload) Config
    {
        [DebuggerStepThrough]
        get
        {
            _checkConfigFunc();
            return _config;
        }
    }

    /// <inheritdoc />
    [DebuggerStepThrough]
    protected override bool PrintMembers(StringBuilder builder)
    {
        Debug.Assert(builder != null);

        if (base.PrintMembers(builder))
        {
            _ = builder.Append(", ");
        }

        _ = builder.Append($"Config = {_config}");

        return true;
    }

    /// <inheritdoc />
    [DebuggerStepThrough]
    public bool Equals(Feature<TPayload>? other) =>
        base.Equals(other) &&
        Equals(_config, other._config);

    /// <inheritdoc />
    [DebuggerStepThrough]
    public override int GetHashCode() =>
        HashCode.Combine(base.GetHashCode(), _config);
}
