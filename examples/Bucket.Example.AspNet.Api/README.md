# Bucket ASP.NET API Example

This example demonstrates how to integrate Bucket feature flags into an ASP.NET Core API application.
It showcases a simple TODO API with feature-flagged endpoints.

## Overview

The example implements a basic TODO API with four endpoints, each protected by different feature flags.
It uses Bucket's feature management capabilities to control access to these endpoints.

## Key components

### Feature configuration

- The application uses `AddBucketFeatures()` to set up Bucket's feature management system.
- Local features are configured using `AddLocalFeatures()` with predefined feature states.
- A custom unauthorized handler is set up using `UseRestrictedFeatureHandler()`.

### API endpoints

- GET `/todos` - Lists all TODO items (protected by "list-todos" feature).
- GET `/todos/{id}` - Retrieves a specific TODO item (protected by "get-todo" feature).
- POST `/todos` - Creates a new TODO item (protected by "create-todos" feature).
- DELETE `/todos/{id}` - Deletes a TODO item (protected by "delete-todos" feature).

### Feature flags

- `list-todos` - Enabled by default.
- `get-todo` - Enabled by default.
- `create-todos` - Enabled by default.
- `delete-todos` - Disabled by default.

## Technical details

### Feature restriction

. Each endpoint is protected using the `WithFeatureRestriction()` method.
. Unauthorized access attempts are handled by returning a 401 Unauthorized response.
. Feature flags are evaluated at runtime for each request.

## Running the example

1. Replace `<bucket-secret-key>` with your actual Bucket secret key in `appsettings.json`.
2. Ensure you have .NET SDK installed.
3. Navigate to the project directory.
4. Run `dotnet run`.
5. Access the API documentation at `/scalar` endpoint.

## Best practices demonstrated

. Separation of concerns between feature management and business logic.
. Clear feature flag naming conventions.
. Proper error handling for restricted features.
. API documentation integration.
. Clean endpoint organization.

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
