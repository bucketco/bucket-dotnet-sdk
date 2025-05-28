# Bucket SDK for OpenFeature

[![Build & Test](https://github.com/bucketco/bucket-dotnet-sdk/actions/workflows/ci.yml/badge.svg)](https://github.com/bucketco/bucket-dotnet-sdk/actions/workflows/ci.yml) ![Bucket.Sdk.OpenFeature NuGet Version](https://img.shields.io/nuget/v/Bucket.Sdk.OpenFeature?label=Bucket.Sdk.OpenFeature)

## Overview

**Bucket.Sdk.OpenFeature** is an integration library that enables you to use Bucket's feature management capabilities with the [OpenFeature](https://openfeature.dev/) framework. This integration lets you leverage the industry-standard OpenFeature API with Bucket's powerful feature management capabilities.

## Installation

Install the package via NuGet:

```shell
dotnet add package Bucket.Sdk.OpenFeature
```

## Prerequisites

- .NET 9.0 or later
- OpenFeature packages
- A secret key for your app/environment in your [Bucket account](https://app.bucket.co/envs/current/settings/app-environments)

## Getting Started

### Register the Bucket Provider with OpenFeature

In your application startup, configure OpenFeature to use the Bucket provider:

```csharp
using Bucket.Sdk;
using Microsoft.Extensions.DependencyInjection;
using OpenFeature;

// Register Bucket services
services.AddBucketFeatures()
    .Configure(options =>
    {
        options.SecretKey = "<your-bucket-secret-key>";
        // Additional configuration options as needed
    });

// Register OpenFeature with Bucket provider
services.AddOpenFeature(builder =>
    builder
        .AddHostedFeatureLifecycle() // Optional: for service lifecycle support
        .AddContext((contextBuilder, serviceProvider) =>
            // Optional: set default context values for all evaluations
            contextBuilder.Set("default_key", "default_value")
        )
        .AddBucketFeaturesProvider()
);
```

### Using the Feature Client

After registering the Bucket provider, you can inject and use the OpenFeature client:

```csharp
using OpenFeature;
using OpenFeature.Model;

public class MyService
{
    private readonly FeatureClient _featureClient;

    public MyService(FeatureClient featureClient)
    {
        _featureClient = featureClient;
    }

    public async Task<string> GetWelcomeMessage(string userId, string email)
    {
        // Create a context with user information
        var context = EvaluationContext.Builder()
            .Set("userId", userId)
            .Set("email", email)
            .Build();

        // Check a boolean feature flag
        var isNewWelcomeEnabled = await _featureClient.GetBooleanValueAsync(
            "new-welcome-message", 
            false, 
            context
        );

        if (isNewWelcomeEnabled)
        {
            // Get a structured configuration
            var welcomeConfig = await _featureClient.GetObjectValueAsync(
                "welcome-config", 
                new Value(), 
                context
            );
            
            return welcomeConfig.AsStructure["message"].AsString;
        }

        return "Welcome to our application!";
    }
}
```

### Tracking Feature Usage

Use OpenFeature's tracking capabilities to record feature usage:

```csharp
// Basic tracking
_featureClient.Track("feature_used", context);

// Tracking with additional details
var details = TrackingEventDetails.Builder()
    .Set("detail-key", "detail-value")
    .Build();

_featureClient.Track("feature_used_with_details", context, details);
```

## Advanced Usage

### Custom Evaluation Context Translation

By default, the Bucket provider maps standard OpenFeature context fields to Bucket's Context model. You can provide a custom translator function to customize this mapping:

```csharp
// Custom translator function
Context CustomTranslator(EvaluationContext? evaluationContext)
{
    var context = new Context();
    if (evaluationContext != null)
    {
        // Extract user information
        var userId = evaluationContext.GetValue("custom_user_id")?.AsString;
        if (!string.IsNullOrEmpty(userId))
        {
            context.User = new User(userId)
            {
                Email = evaluationContext.GetValue("custom_email")?.AsString
            };
        }

        // Extract company information
        var companyId = evaluationContext.GetValue("custom_company_id")?.AsString;
        if (!string.IsNullOrEmpty(companyId))
        {
            context.Company = new Company(companyId)
            {
                Name = evaluationContext.GetValue("custom_company_name")?.AsString
            };
        }

        // Add custom attributes
        foreach (var (key, value) in evaluationContext)
        {
            if (!key.StartsWith("custom_"))
            {
                context.Add(key, value.AsObject);
            }
        }
    }
    
    return context;
}

// Register the custom translator
services.AddOpenFeature(builder =>
    builder.AddBucketFeaturesProvider(CustomTranslator)
);
```

## Examples

- [OpenFeature integration](../../examples/Bucket.Example.OpenFeature/README.md)

## Support

For support, questions, or feedback, please visit [Bucket's documentation](https://docs.bucket.co) or create an issue in the [GitHub repository](https://github.com/bucketco/bucket-dotnet-sdk).

## License

MIT License Copyright (c) 2025 Bucket ApS
