namespace Bucket.Sdk;

/// <summary>
///     Extension methods for <see cref="Controller" /> to add feature management
///     integration for ASP.NET application building.
/// </summary>
public static class ControllerExtensions
{
    /// <summary>
    ///     Obtains a feature from the current controller's HTTP context.
    /// </summary>
    /// <param name="controller">The controller to use.</param>
    /// <param name="featureKey">The feature key to retrieve.</param>
    /// <returns>A task that completes with the feature.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="controller" /> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="featureKey" /> is <see langword="null"/> or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the Bucket feature service is not registered.</exception>
    [PublicAPI]
    public static async Task<IFeature> GetFeatureAsync(
        this Controller controller,
        string featureKey)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentException.ThrowIfNullOrEmpty(featureKey);

        return await controller.HttpContext.GetFeatureAsync(featureKey);
    }

    /// <summary>
    ///     Obtains a feature from the current controller's HTTP context.
    /// </summary>
    /// <param name="controller">The controller to use.</param>
    /// <param name="featureKey">The feature key to retrieve.</param>
    /// <typeparam name="TPayload">The type of the feature payload.</typeparam>
    /// <returns>A task that completes with the feature.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="controller" /> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="featureKey" /> is <see langword="null"/> or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the Bucket feature service is not registered.</exception>
    [PublicAPI]
    public static async Task<IFeature<TPayload>> GetFeatureAsync<TPayload>(
        this Controller controller,
        string featureKey)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentException.ThrowIfNullOrEmpty(featureKey);

        return await controller.HttpContext.GetFeatureAsync<TPayload>(featureKey);
    }
}
