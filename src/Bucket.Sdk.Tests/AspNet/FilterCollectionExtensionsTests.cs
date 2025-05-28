namespace Bucket.Sdk.Tests.AspNet;

using Microsoft.AspNetCore.Mvc.Filters;

public sealed class FilterCollectionExtensionsTests
{
    private const string _featureKey = "test-feature";
    private readonly FilterCollection _filters;

    public FilterCollectionExtensionsTests() => _filters = [];

    [Fact]
    public void AddFeatureRestricted_AddsFilterToCollection()
    {
        // Arrange
        var result = _filters.AddFeatureRestricted<MockFilter>(_featureKey);

        // Assert
        Assert.NotNull(result);
        _ = Assert.IsType<IFilterMetadata>(result, false);
        _ = Assert.Single(_filters);
    }

    [Fact]
    public void AddFeatureRestricted_ThrowsWhenArgumentsAreInvalid()
    {
        _ = Assert.Throws<ArgumentNullException>(() =>
            ((FilterCollection) null!).AddFeatureRestricted<MockFilter>(_featureKey));
        _ = Assert.Throws<ArgumentNullException>(() =>
            _filters.AddFeatureRestricted<MockFilter>(null!));
        _ = Assert.Throws<ArgumentException>(() =>
            _filters.AddFeatureRestricted<MockFilter>(string.Empty));
    }

    [UsedImplicitly]
    private sealed class MockFilter: IAsyncActionFilter
    {
        public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) => next();
    }
}
