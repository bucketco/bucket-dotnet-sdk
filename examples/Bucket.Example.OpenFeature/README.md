# Bucket SDK with OpenFeature example

This example demonstrates how to integrate the Bucket SDK with OpenFeature for feature flag management in a .NET application.
It showcases how to leverage OpenFeature's standardized API alongside Bucket's feature management capabilities.

## Code walkthrough

### 1. Service configuration

The example sets up a service collection with:

- Configuration binding from `appsettings.json`.
- Console logging configuration.
- Bucket SDK integration with local feature overrides.
- OpenFeature registration with Bucket as the provider.

#### Key Bucket methods

- `AddBucketFeatures()`: Registers Bucket SDK services with the dependency injection container, enabling feature flag functionality.
- `AddLocalFeatures()`: Adds local feature overrides for development and testing purposes, allowing you to define feature values that override remote configurations.
- `AddBucketFeaturesProvider()`: Registers Bucket as a provider for OpenFeature, connecting the Bucket feature management system to the OpenFeature API.

### 2. Context Creation

Demonstrates creating a rich OpenFeature evaluation context including:

- User information (userId, name, email).
- Company information (companyId, companyName).
- Custom attributes (plan, country, company_size, industry).

### 3. Feature flag consumption

Shows how to use feature flags through the OpenFeature API:

- Boolean feature flag evaluation.
- Feature configuration retrieval.
- Event tracking when features are used.
- Proper shutdown to flush pending events.

#### Feature Evaluation and Tracking

- The example uses OpenFeature's `GetBooleanValueAsync()` and `GetObjectValueAsync()` methods which internally leverage Bucket's evaluation capabilities.
- `Track()`: Records usage events when features are accessed, enabling analytics and insights about feature usage patterns.
- `Api.Instance.ShutdownAsync()`: Ensures all tracking events are properly flushed to Bucket's servers before the application exits.

## Getting Started

1. Replace `<bucket-secret-key>` with your actual Bucket secret key in `appsettings.json`.
2. Run the example: `dotnet run`.

## All examples

- [Basic usage](../Bucket.Example.Basic/README.md).
- [Advanced usage](../Bucket.Example.Advanced/README.md).
- [OpenFeature integration](../Bucket.Example.OpenFeature/README.md).
- [Nightly sync job](../Bucket.Example.SyncJob/README.md).
- [Basic API](../Bucket.Example.AspNet.Api/README.md).
- [Controllers-based API](../Bucket.Example.AspNet.Controllers/README.md).
- [ASP.NET MVC](../Bucket.Example.AspNet.Mvc/README.md).

## License

> MIT License Copyright (c) 2025 Bucket ApS
