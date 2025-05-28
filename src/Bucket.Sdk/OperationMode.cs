namespace Bucket.Sdk;

/// <summary>
///     Specifies the operation mode of the SDK.
/// </summary>
[PublicAPI]
public enum OperationMode
{
    /// <summary>
    ///     The SDK is offline. It does not communicate with the Bucket service.
    /// </summary>
    Offline = 0,

    /// <summary>
    ///     The SDK is online. Features definitions are fetched from the Bucket service and evaluated locally.
    /// </summary>
    LocalEvaluation = 1,

    /// <summary>
    ///     The SDK is online. Features definitions are not fetched from the Bucket service and evaluated remotely.
    /// </summary>
    RemoteEvaluation = 2,
}
