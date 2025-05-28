# Bucket SDK basic example

This example demonstrates the fundamental usage of the Bucket SDK for feature flag management in a .NET application.

## Overview

The example showcases how to:

- Initialize the Bucket SDK client.
- Create a context for feature evaluation.
- Retrieve and display all available feature flags.

## Code Explanation

The example creates a `FeatureClient` instance with a secret key, which is the minimal configuration needed to connect
to Bucket's services.
It then demonstrates how to:

1. **Create a Context**: The context object represents the user and company context for feature evaluation. It includes:
    - User information (`ID`, `name`, email).
    - Company information (`ID`, `name`, and custom properties).
    - Application-specific context.

2. **Retrieve Features**: Shows how to fetch all available feature flags for the given context using `GetFeaturesAsync`.

## Usage

To run this example:

1. Replace `<bucket-secret-key>` with your actual Bucket secret key.
2. Run the application: `dotnet run`.

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
