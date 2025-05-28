# Bucket ASP.NET controller example

This example demonstrates how to use Bucket feature flags in an ASP.NET Core application
using the traditional `Controller` pattern.

## Features

- Feature flag protection using `[FeatureRestricted]` attribute.
- Scalar API documentation.
- RESTful API endpoints for "TODO" management.

## Walkthrough

The example uses local feature flags configured in `Program.cs`:

```csharp
   AddLocalFeatures(
       new EvaluatedFeature("list-todos", true),
       new EvaluatedFeature("get-todo", true),
       new EvaluatedFeature("create-todo", true),
       new EvaluatedFeature("delete-todo", false)
   );
```

Feature flag protection is implemented using the `[FeatureRestricted]` attribute, which can be applied at both
controller and action levels:

1. **Controller-level protection**: The `TodoController` is decorated with `[FeatureRestricted("list-todos")]`, meaning
   that all actions in the controller require the `list-todos` feature to be enabled.
2. **Action-level protection**: Individual actions can override the controller-level feature requirement by
   specifying their own `[FeatureRestricted]` attribute. For example:
    - `GetById` requires `get-todo`.
    - `Create` requires `create-todo`.
    - `Delete` requires `delete-todo`.

When a feature flag is disabled, the `UseRestrictedFeatureHandler` middleware intercepts the request and returns a
`401 Unauthorized` response:

```csharp
    UseRestrictedFeatureHandler((_, _) => ValueTask.FromResult<IActionResult>(new UnauthorizedResult()))
```

This handler can be customized to return different responses or perform additional actions when a feature is restricted.

## API endpoints

All endpoints are protected by feature flags:

- `GET /todo` - Lists all todos (requires `list-todos` feature).
- `GET /todo/{id}` - Gets a specific todo (requires `get-todo` feature).
- `POST /todo` - Creates a new todo (requires `create-todo` feature).
- `DELETE /todo/{id}` - Deletes a todo (requires `delete-todo` feature).

## Running the example

1. Replace `<bucket-secret-key>` with your actual Bucket secret key in `appsettings.json`.
2. Ensure you have .NET SDK installed.
3. Navigate to the project directory.
4. Run `dotnet run`.
5. Access the API documentation at `/scalar` endpoint.

## Feature behavior

- If a feature flag is enabled, the endpoint will work normally.
- If a feature flag is disabled, the endpoint will return `401 Unauthorized`.
- The `delete-todos` feature is disabled by default in this example.

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
