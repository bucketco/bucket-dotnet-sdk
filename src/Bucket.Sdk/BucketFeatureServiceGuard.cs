namespace Bucket.Sdk;

/// <summary>
///     Specialized guard class to ensure that the Bucket feature service is registered in the service container.
/// </summary>
/// <remarks>
///     This guard is used to ensure that the Bucket feature service is registered in the service container.
///     It is used to avoid a common mistake where the feature service is not registered, which can lead to runtime errors.
/// </remarks>
public sealed class BucketFeatureServiceGuard
{
    /// <summary>
    ///     Ensures that the Bucket feature service is registered in the service container.
    /// </summary>
    /// <param name="serviceProvider">The service provider to check.</param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if the <paramref name="serviceProvider"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if the Bucket feature service is not registered in the service container.
    /// </exception>
    public static void EnsureRegistered(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        _ = serviceProvider.GetService(typeof(BucketFeatureServiceGuard)) ??
            throw new InvalidOperationException(
                $"Bucket feature service not found. Add the required service by calling '{nameof(IServiceCollection)}.{nameof(ServiceCollectionExtensions.AddBucketFeatures)}' in the application startup code.");
    }
}
