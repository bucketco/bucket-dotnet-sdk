namespace Bucket.Sdk;

/// <summary>
///     Specifies the strategy used by Bucket when updating users or companies.
/// </summary>
[PublicAPI]
public enum UpdateStrategy
{
    /// <summary>
    ///     The default strategy, left to the discretion of the Bucket service.
    /// </summary>
    Default,

    /// <summary>
    ///     The updated entity is considered "active" and it's activity updated the "seen" times.
    /// </summary>
    Active,

    /// <summary>
    ///     The updated entity is considered "inactive" and it's activity does not update the "seen" times.
    /// </summary>
    Inactive,
}
