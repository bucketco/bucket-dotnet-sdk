# Bucket SDK for ASP.NET

## Overview

**Bucket.Sdk.AspNet** is an *ASP.NET Core* integration library for the Bucket feature management SDK. It provides
seamless integration between ASP.NET Core applications and Bucket's feature management capabilities, allowing developers
to easily consume feature flags and remote config in their web applications.

## Installation

Install the package via NuGet:

```shell
dotnet add package Bucket.Sdk.AspNet
```

## Prerequisites

- .NET 9.0 or later
- ASP.NET Core application
- A secret key for your app/environment in
  your [Bucket account](https://app.bucket.co/envs/current/settings/app-environments)
- And, Bucket.Sdk.AspNet library installed

## Getting started

### Register services

In your `Program.cs` or `Startup.cs` file, add Bucket feature management to your service collection:

```csharp
app.Services.AddBucketFeatures()
    .Configure(options =>
    {
        // Custom options you want to provide to the client
        options.Mode = OperationMode.RemoteEvaluation, 
    })
    .UseContextResolver(async httpContext =>
    {
        // Create a context from HTTP request data
        var context = new Context
        {
            User = httpContext.User.Identity != null ? new(httpContext.User.Identity.Name) {
                // Add other user-specific properties
            } : null,

            // Add other context properties based on your requirements
        };

        // Return the resolved evaluation context and the tracking strategy
        return (context, TrackingStrategy.Default);
    })
    .UseRestrictedFeatureHandler(async (feature, context) =>
    {
        // Custom handling for when a feature check fails for an MVC action/controller
        return new StatusCodeResult(StatusCodes.Status404NotFound);
    });
```

### 3. Configure middleware & application

You can conditionally use middleware based on feature flags:

```csharp
// Use middleware only when a feature is enabled
app.UseMiddlewareWhenFeature<CustomMiddleware>("new-authentication");

// Use middleware only when a feature is disabled
app.UseMiddlewareWhenNotFeature<LegacyMiddleware>("new-authentication");
```

### Use features in controllers

#### Using attributes

Add feature restrictions to controllers or actions:

```csharp
[FeatureRestricted("premium-features")]
public class PremiumController : Controller
{
    // This controller is only available if "premium-features" is enabled
}

public class ProductController : Controller
{
    [FeatureRestricted("beta-product-page", enabled: true)]
    public IActionResult BetaProduct()
    {
        // This action is only available if "beta-product-page" is enabled
        return View();
    }

    [FeatureRestricted("legacy-feature", enabled: false)]
    public IActionResult LegacyFeature()
    {
        // This action is only available if "legacy-feature" is disabled
        return View();
    }
}
```

#### Programmatically in actions

Check features within controller actions. The code below assumes that the `UseContextResolver` has been
used to register an evaluation context resolver.

The `GetFeatureAsync()` methods are provided for convenience as extensions for the `Controller` class.

```csharp
public class HomeController : Controller
{
    public async Task<IActionResult> Index()
    {
        var feature = await GetFeatureAsync("new-homepage");

        if (feature.Enabled)
        {
            return View("NewHome");
        }

        return View();
    }

    public async Task<IActionResult> Settings()
    {
        var feature = await GetFeatureAsync<SettingsConfig>(
            "enhanced-settings",
        );

        if (feature.Enabled)
        {
            ViewBag.Config = feature.Config;
            return View("EnhancedSettings");
        }

        return View();
    }
}
```

### Endpoint routing based on feature

Apply feature restrictions to endpoints:

```csharp
app.MapGet("/api/beta", () => "This is a beta endpoint")
    .WithFeatureRestriction("beta-api");

app.MapControllerRoute(
    name: "premium",
    pattern: "premium/{controller=Home}/{action=Index}/{id?}")
    .WithFeatureRestriction("premium-features", enabled: true);
```

### Filter integration

Add feature-restricted filters to MVC:

```csharp
// Add a filter that activates only if the feature is enabled
services.AddControllersWithViews(options =>
{
    options.Filters.AddFeatureRestricted<AuditLogFilter>("audit-logging");
});
```

## Advanced Usage

### Custom Context Resolution

Implement a custom evaluation context resolver to derive context from HTTP requests:

```csharp
app.Services.AddBucketFeatures()
    .UseContextResolver(async httpContext =>
    {
        var context = new Context();

        // Extract user information
        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            context.User = new(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)) {
                Email = httpContext.User.FindFirstValue(ClaimTypes.Email);
                ["role"] = httpContext.User.FindFirstValue(ClaimTypes.Role);
            }
        }

        // Add request-specific information
        context["ip"] = httpContext.Connection.RemoteIpAddress?.ToString();
        context["user-agent"] = httpContext.Request.Headers.UserAgent.ToString();

        // Get organization from header or cookie
        if (httpContext.Request.Headers.TryGetValue("X-Organization", out var org))
        {
            context.Company = new(org.ToString())
        }

        return (context, TrackingStrategy.Default);
    });
```

### Custom restricted feature handling

Customize how restricted features are handled:

```csharp
// For MVC actions and controllers
app.Services.AddBucketFeatures()
    .UseRestrictedFeatureHandler(async (feature, context) =>
    {
        // Log the feature flag check attempt
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogWarning("Attempted access to restricted feature flag: {Feature}", feature.Key);

        // For API requests, return a JSON response
        if (context.HttpContext.Request.Path.StartsWithSegments("/api"))
        {
            return new JsonResult(new
            {
                error = "Feature not available",
                featureKey = feature.Key,
                message = "This feature is not available for your account"
            })
        }
        // For web requests, redirect to an upgrade page
        else
        {
            return new RedirectToActionResult(
                "Upgrade",
                "Subscription",
                new { featureKey = feature.Key }
            );
        }
    });

// For minimal API endpoints
app.Services.AddBucketFeatures()
    .UseRestrictedFeatureHandler(async (feature, context) =>
    {
        // Log the feature flag check attempt  
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogWarning("Attempted access to restricted endpoint feature: {Feature}", feature.Key);

        // Return a custom response for endpoint restrictions
        return Results.Problem(
            title: "Feature Unavailable",
            detail: $"The feature '{feature.Key}' is not available for your account",
            statusCode: StatusCodes.Status403Forbidden
        );
    });
```

## Best Practices

- **Use meaningful feature keys** - Choose descriptive names and keys for your features that clearly indicate their
  purpose
- **Apply context-aware evaluations** - Leverage
  the [full power of context](https://docs.bucket.co/product-handbook/feature-rollouts) to target features to specific
  users, segments, or environments.
- **Handle graceful degradation** - Always ensure your application degrades gracefully when features are disabled
- **Separate routing concerns** - Use feature flags to control routes or controllers rather than individual
  UI elements
- **Monitor feature usage** - Take advantage
  of [Bucket's analytics](https://docs.bucket.co/product-handbook/feature-usage-configuration) to understand how
  features are being used
- **Test both states** - Always test your application with features both enabled and disabled to ensure proper
  functionality

## Examples

- [Basic API](../Bucket.Example.AspNet.Api/README.md)
- [Controllers-based API](../Bucket.Example.AspNet.Controllers/README.md)
- [ASP.NET MVC](../Bucket.Example.AspNet.Mvc/README.md)

## Support

For support, questions, or feedback, please visit [Bucket's documentation](https://docs.bucket.co) or create an issue in
the [GitHub repository](https://github.com/bucketco/bucket-dotnet-sdk).

## License

> MIT License Copyright (c) 2025 Bucket ApS
