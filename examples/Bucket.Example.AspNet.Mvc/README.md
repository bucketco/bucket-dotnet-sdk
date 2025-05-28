# Bucket ASP.NET MVC example

This example demonstrates how to integrate Bucket feature flags into an ASP.NET Core MVC application.
It showcases a simple TODO application with feature-flagged controllers, actions, and UI components.

## Overview

The example implements a basic TODO application with CRUD operations, each protected by different feature flags.
It demonstrates how to use Bucket's feature management capabilities to control access to both backend functionality and UI elements.

## Key components

### Feature configuration

- The application uses `AddBucketFeatures()` to set up Bucket's feature management system.
- Local features are configured using `AddLocalFeatures()` with predefined feature states.
- A custom unauthorized handler is set up using `UseRestrictedFeatureHandler()` to redirect users to an Unauthorized page.

### MVC endpoints

- **Index** - Lists all TODO items (protected by "list-todos" feature).
- **Details** - Displays details of a specific TODO item (protected by "get-todo" feature).
- **Create** - Allows creation of new TODO items (protected by "create-todo" feature).
- **Delete** - Enables deletion of TODO items (protected by "delete-todo" feature).

### Feature flags

- `list-todos` - Enabled by default.
- `get-todo` - Enabled by default.
- `create-todo` - Enabled by default.
- `delete-todo` - Disabled by default (demonstrating restricted functionality).

## Technical details

### Feature restriction

The example demonstrates multiple ways to use feature flags:

1. **Controller actions restriction**: Each controller action is protected using the `[FeatureRestricted]` attribute.
2. **UI conditional rendering**: The views use various tag helpers to conditionally render UI elements based on feature flags:
   - `feature` attribute to conditionally show/hide elements
   - `show-when-feature-enabled` and `show-when-feature-disabled` to conditionally display content
   - `enable-when-feature-enabled` to control UI element state

3. **Custom unauthorized handling**: Redirects to a dedicated Unauthorized page when a user attempts to access a disabled feature.

## Running the example

1. Ensure you have .NET SDK installed.
2. Navigate to the project directory.
3. Run `dotnet run`.
4. Access the application at the default URL.

## Best practices demonstrated

- Proper separation of concerns between feature management and application logic.
- Clear feature flag naming conventions that match functionality.
- Graceful handling of unauthorized access attempts.
- Consistent UI feedback based on feature availability.
- Multiple methods of feature restriction in both backend and frontend.

## Other examples

- [Basic usage](../Bucket.Example.Basic/README.md)
- [Advanced usage](../Bucket.Example.Advanced/README.md)
- [OpenFeature integration](../Bucket.Example.OpenFeature/README.md)
- [Nightly sync job](../Bucket.Example.SyncJob/README.md)
- [Basic API](../Bucket.Example.AspNet.Api/README.md)
- [Controllers-based API](../Bucket.Example.AspNet.Controllers/README.md)
- [ASP.NET MVC](../Bucket.Example.AspNet.Mvc/README.md)

## License

> MIT License Copyright (c) 2025 Bucket ApS
