namespace Bucket.Sdk;

/// <summary>
///     Specifies the strategy used by Bucket when tracking feature usage.
/// </summary>
[PublicAPI]
public enum TrackingStrategy
{
    /// <summary>
    ///     The default strategy. The feature usage is tracked but user/company updates are left to the
    ///     discretion of the Bucket service.
    /// </summary>
    Default,

    /// <summary>
    ///     The feature usage is not tracked.
    /// </summary>
    Disabled,

    /// <summary>
    ///     The feature usage is tracked and user/company activity is updated. See <see cref="UpdateStrategy.Active" />.
    /// </summary>
    Active,

    /// <summary>
    ///     The feature usage is tracked and user/company activity is not updated. See <see cref="UpdateStrategy.Inactive" />.
    /// </summary>
    Inactive,
}
