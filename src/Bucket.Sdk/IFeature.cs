namespace Bucket.Sdk;

/// <summary>
///     Describes a feature exposed by the SDK.
/// </summary>
/// <remarks>
///     Reading the value of <see cref="Enabled" /> may trigger side effects such as sending
///     events to Bucket service.
/// </remarks>
[PublicAPI]
public interface IFeature
{
    /// <summary>
    ///     The key of the feature.
    /// </summary>
    string Key
    {
        get;
    }

    /// <summary>
    ///     Specifies whether the feature is enabled.
    /// </summary>
    bool Enabled
    {
        get;
    }

    /// <summary>
    ///     Tracks the feature usage.
    ///     Consumers should call this method whenever the feature is used.
    /// </summary>
    void Track();
}

/// <summary>
///     Describes an evaluated feature exposed by the SDK, including feature config.
/// </summary>
/// <typeparam name="TPayload">The type of the payload used in remote configuration.</typeparam>
/// <remarks>
///     Reading the value of <see cref="Config" /> or <see cref="IFeature.Enabled" /> may trigger side effects such as
///     sending
///     events to Bucket service.
/// </remarks>
[PublicAPI]
public interface IFeature<TPayload>: IFeature
{
    /// <summary>
    ///     The configuration of the feature.
    /// </summary>
    (string? Key, TPayload? Payload) Config
    {
        get;
    }
}
