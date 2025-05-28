# Bucket SDK advanced example

This example demonstrates advanced usage of the Bucket SDK for feature management in a .NET application.
It showcases various capabilities and best practices for consuming feature flags.

## Features Demonstrated

- **Configuration Management**: Using `appsettings.json` for SDK configuration.
- **Logging Integration**: Console logging with `Microsoft.Extensions.Logging`.
- **Local Feature Resolution**: Development/testing capabilities with local feature overrides.
- **Feature Flag Evaluation**: Context-aware feature flag checking.
- **Feature Configuration**: Retrieving feature-specific configurations.
- **Event Tracking**: Usage tracking and analytics capabilities.

## Code walkthrough

### 1. Service Configuration

The example sets up a service collection with:

- Configuration binding from `appsettings.json`.
- Console logging configuration.
- Bucket SDK integration.
- Local feature resolver for development/testing.

### 2. Context Creation

Demonstrates how to create a rich context for feature evaluation including:

- User information.
- Company information.
- Custom attributes (plan, country, company size, industry).

### 3. Feature flag consumption

Shows different ways to use feature flags:

- Simple boolean feature checks.
- Features with configuration payloads.
- Bulk feature retrieval.
- Usage tracking and analytics.

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
