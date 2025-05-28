# Bucket Community .NET SDK

[![Build & Test](https://github.com/bucketco/bucket-dotnet-sdk/actions/workflows/ci.yml/badge.svg)](https://github.com/bucketco/bucket-dotnet-sdk/actions/workflows/ci.yml) [Bucket.Sdk](https://img.shields.io/nuget/v/Bucket.Sdk)

.NET client for [Bucket.co](https://bucket.co).

Bucket supports feature toggling, tracking feature usage, collecting feedback on features, and remotely
configuring features.

> [!Note]
> This is a community-led SDK for Bucket.co. It is developed and maintained by the community, not directly
> by Bucket.co. While we strive to maintain compatibility with the Bucket.co API and provide a high-quality
> SDK, please note that Bucket.co is not directly responsible for its development, maintenance, or support.

We welcome contributions from the community to help improve this SDK. If you encounter any issues or have
suggestions for improvements, please open an issue or submit a pull request on GitHub.

## Installation

To use the [core Bucket SDK](src/Bucket.Sdk/README.md), install:

```shell
dotnet add package Bucket.Sdk
```

For [ASP.NET](src/Bucket.Sdk.AspNet/README.md), install:

```shell
dotnet add package Bucket.Sdk.AspNet
```

When using [OpenFeature](src/Bucket.Sdk.OpenFeature/README.md), install:

```shell
dotnet add package Bucket.Sdk.OpenFeature
```

## Code examples

- [Basic usage](examples/Bucket.Example.Basic/README.md)
- [Advanced usage](examples/Bucket.Example.Advanced/README.md)
- [OpenFeature integration](examples/Bucket.Example.OpenFeature/README.md)
- [Nightly sync job](examples/Bucket.Example.SyncJob/README.md)
- [Basic API](examples/Bucket.Example.AspNet.Api/README.md)
- [Controllers-based API](examples/Bucket.Example.AspNet.Controllers/README.md)
- [ASP.NET MVC](examples/Bucket.Example.AspNet.Mvc/README.md)

## Other languages

You can find documentation on other supported languages in the [Supported languages](https://docs.bucket.co/quickstart/supported-languages) documentation pages.

You can also [use the HTTP API directly](https://docs.bucket.co/api/http-api)

## Future plans

- Potential support for Bucket CLI as a command line tool for `dotnet`.
- Support for MCP (Model Context Protocol) in the future.
- Source generator for strongly-typed feature flags.

## License

> MIT License Copyright (c) 2025 Bucket ApS
