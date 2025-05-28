namespace Bucket.Example.AspNet.Mvc.TagHelpers;

using Microsoft.AspNetCore.Razor.TagHelpers;

using Sdk;

/// <summary>
///     Tag helper for conditionally showing or hiding content based on feature flags
/// </summary>
[HtmlTargetElement(Attributes = "show-when-feature-enabled")]
[HtmlTargetElement(Attributes = "show-when-feature-disabled")]
public class FeatureTagHelper: TagHelper
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private string? _featureKey;
    private bool _requireEnabled;

    public FeatureTagHelper(IHttpContextAccessor httpContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);

        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    ///     The feature key to check if enabled
    /// </summary>
    [HtmlAttributeName("show-when-feature-enabled")]
    public string? EnabledFeatureKey
    {
        get => _requireEnabled ? _featureKey : null;
        set
        {
            if (!string.IsNullOrEmpty(value))
            {
                _requireEnabled = true;
                _featureKey = value;
            }
        }
    }

    /// <summary>
    ///     The feature key to check if disabled
    /// </summary>
    [HtmlAttributeName("show-when-feature-disabled")]
    public string? DisabledFeatureKey
    {
        get => _requireEnabled ? null : _featureKey;
        set
        {
            if (!string.IsNullOrEmpty(value))
            {
                _requireEnabled = false;
                _featureKey = value;
            }
        }
    }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);

        if (_httpContextAccessor.HttpContext == null)
        {
            // Can't check features without HttpContext
            output.SuppressOutput();
            return;
        }

        if (!string.IsNullOrEmpty(_featureKey))
        {
            var feature = await _httpContextAccessor.HttpContext.GetFeatureAsync(_featureKey);
            if (feature.Enabled != _requireEnabled)
            {
                output.SuppressOutput();
            }
        }
    }
}
