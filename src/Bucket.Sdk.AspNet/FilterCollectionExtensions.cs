namespace Bucket.Sdk;

/// <summary>
///     Extension methods for <see cref="FilterCollection" /> to add feature management
///     integration for ASP.NET application building.
/// </summary>
[PublicAPI]
public static class FilterCollectionExtensions
{
    /// <summary>
    ///     Adds a filter to the pipeline that will only activate during a request if the specified feature
    ///     restriction is satisfied.
    /// </summary>
    /// <typeparam name="TFilter">The MVC filter to add and use if the feature is enabled.</typeparam>
    /// <param name="filters">The filter collection to add to.</param>
    /// <param name="featureKey">The feature key to check for.</param>
    /// <returns></returns>
    public static IFilterMetadata AddFeatureRestricted<TFilter>(
        this FilterCollection filters,
        string featureKey) where TFilter : IAsyncActionFilter
    {
        ArgumentNullException.ThrowIfNull(filters);
        ArgumentException.ThrowIfNullOrEmpty(featureKey);

        var filterMetadata = new FeatureRestrictedActionFilter<TFilter>(featureKey);
        filters.Add(filterMetadata);

        return filterMetadata;
    }
}
