namespace Bucket.Example.AspNet.Mvc.TagHelpers;

using Microsoft.AspNetCore.Razor.TagHelpers;

using Sdk;

/// <summary>
///     Tag helper for conditionally disabling buttons based on feature flags
/// </summary>
[HtmlTargetElement("a", Attributes = "enable-when-feature-enabled")]
[HtmlTargetElement("button", Attributes = "enable-when-feature-enabled")]
public class FeatureButtonTagHelper: TagHelper
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public FeatureButtonTagHelper(IHttpContextAccessor httpContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    ///     The feature key to check. Button will be disabled if feature is disabled.
    /// </summary>
    [HtmlAttributeName("enable-when-feature-enabled")]
    public string? FeatureKey
    {
        get;
        set;
    }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        if (string.IsNullOrEmpty(FeatureKey) || _httpContextAccessor.HttpContext == null)
        {
            return;
        }

        // Check if the feature is enabled
        var feature = await _httpContextAccessor.HttpContext.GetFeatureAsync(FeatureKey);

        if (!feature.Enabled)
        {
            // For <a> elements
            output.Attributes.SetAttribute("class", $"{output.Attributes["class"]?.Value} disabled");
            output.Attributes.SetAttribute("aria-disabled", "true");

            // Prevent navigation for <a> tags by removing href or making it a dummy link
            if (output.TagName.Equals("a", StringComparison.OrdinalIgnoreCase))
            {
                output.Attributes.SetAttribute("href", "#");

                // Add `onclick` handler to prevent default action
                output.Attributes.SetAttribute("onclick", "event.preventDefault();");
            }

            // For <button> elements
            if (output.TagName.Equals("button", StringComparison.OrdinalIgnoreCase))
            {
                output.Attributes.SetAttribute("disabled", "disabled");
            }
        }
    }
}
