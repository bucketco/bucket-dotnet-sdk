namespace Bucket.Sdk;

/// <summary>
///     Describes the Bucket feature client API.
/// </summary>
[PublicAPI]
public interface IFeatureClient
{
    /// <summary>
    ///     Refreshes the features from the Bucket servers.
    /// </summary>
    /// <returns>A task that completes when the features are refreshed.</returns>
    Task RefreshAsync();

    /// <summary>
    ///     Flushes the output buffer.
    /// </summary>
    /// <returns>A task that completes when the buffer is flushed.</returns>
    Task FlushAsync();

    /// <summary>
    ///     Updates the user details.
    /// </summary>
    /// <param name="user">The user to update.</param>
    /// <param name="updateStrategy">The update strategy to use.</param>
    /// <returns>A task that completes when the user is updated.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the user is null.</exception>
    Task UpdateUserAsync(
        User user, UpdateStrategy updateStrategy = UpdateStrategy.Default);

    /// <summary>
    ///     Updates the company details.
    /// </summary>
    /// <param name="company">The company to update.</param>
    /// <param name="user">The user to associate with the company (optional).</param>
    /// <param name="updateStrategy">The update strategy to use.</param>
    /// <returns>A task that completes when the company is updated.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the company is null.</exception>
    Task UpdateCompanyAsync(
        Company company, User? user = null, UpdateStrategy updateStrategy = UpdateStrategy.Default);

    /// <summary>
    ///     Tracks an event.
    /// </summary>
    /// <param name="event">The event to track.</param>
    /// <param name="updateStrategy">The update strategy to use.</param>
    /// <returns>A task that completes when the event is tracked.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the event is null.</exception>
    Task TrackAsync(
        Event @event, UpdateStrategy updateStrategy = UpdateStrategy.Default);

    /// <summary>
    ///     Get all the feature known to <see cref="FeatureClient" />.
    /// </summary>
    /// <param name="context">The context used to resolve features.</param>
    /// <returns>A task that completes with the features.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="context" /> is <see langword="null"/>.</exception>
    Task<IReadOnlyDictionary<string, EvaluatedFeature>> GetFeaturesAsync(Context context);

    /// <summary>
    ///     Gets a feature.
    /// </summary>
    /// <param name="key">The key of the feature.</param>
    /// <param name="context">The context.</param>
    /// <param name="trackingStrategy">The tracking strategy.</param>
    /// <returns>A task that completes with the feature.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="key" /> is <see langword="null"/> or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="context" /> is <see langword="null"/>.</exception>
    Task<IFeature> GetFeatureAsync(
        string key, Context context, TrackingStrategy trackingStrategy = TrackingStrategy.Default);

    /// <summary>
    ///     Gets a feature with a config.
    /// </summary>
    /// <typeparam name="TPayload">The type of the config payload.</typeparam>
    /// <param name="key">The key of the feature.</param>
    /// <param name="context">The context.</param>
    /// <param name="trackingStrategy">The tracking strategy.</param>
    /// <returns>A task that completes with the feature with the config.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="key" /> is <see langword="null"/> or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="context" /> is <see langword="null"/>.</exception>
    Task<IFeature<TPayload>> GetFeatureAsync<TPayload>(
        string key, Context context, TrackingStrategy trackingStrategy = TrackingStrategy.Default);
}
