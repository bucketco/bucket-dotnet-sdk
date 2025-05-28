# Bucket .NET SDK

## Overview

**Bucket.Sdk** is a library for the Bucket feature management SDK. It provides
seamless integration between .NET applications and Bucket's feature management
capabilities, allowing developers to easily consume feature flags and remote config
in their applications.

## Installation

Install the package via NuGet:

```shell
dotnet add package Bucket.Sdk
```

## Basic usage

To get started you need to obtain your secret key from the [environment settings](https://app.bucket.co/envs/current/settings/app-environments)
in Bucket.

> [!CAUTION]
> Secret keys are meant for use in server side SDKs only. Secret keys offer the
> users the ability to obtain information that is often sensitive and thus should
> not be used in client-side applications.

```csharp
using Bucket.Sdk;

// Create a new instance of the client with default configuration.
// Additional options are available, such as supplying a logger and
// other custom properties.
//
// We recommend that only one global instance of FeatureClient should be created
// to avoid multiple round-trips to Bucket servers.
var featureClient = new FeatureClient(new Configuration
{
    SecretKey = Environment.GetEnvironmentVariable("BUCKET_SECRET_KEY")
});

// The call to this method is optional (features definitions will be downloaded
// when requesting features, but it is recommended to pre-fetch them to avoid 
// unnecessary delays. `RefreshAsync()` is also **not** necessary when running
// in `offline` or `remote` evaluation modes.
await featureClient.RefreshAsync();
```

Once the client is initialized, you can obtain features along with the `Enabled` status that
indicates whether the feature is targeted for a user/company.

```csharp
// Create context for evaluation
var context = new Context
{
    User = new User("john_doe")
    {
        Name = "John Doe",
        Email = "john@acme.com",
        Avatar = "https://example.com/users/jdoe"
    },
    Company = new Company("acme_inc")
    {
        Name = "Acme, Inc.",
        Avatar = "https://example.com/companies/acme"
    }
};

// Get the huddle feature using company, user and context to
// evaluate the targeting. The `ZoomFeaturePayload` is the type deserialized from
// the payload that was registered in Bucket.
var feature = await featureClient.GetFeatureAsync<ZoomFeaturePayload>("huddle", context);

if (feature.Enabled)
{
    // This is your feature gated code...
    // Send an event when the feature is used:
    feature.Track();

    if (feature.Config?.Key == "zoom")
    {
        // This code will run if a given remote configuration is set up
    }

    // CAUTION: if you plan to use the event for automated feedback surveys
    // call `FlushAsync()` immediately after Track. It can be awaited
    // to guarantee the send happened.
    await featureClient.FlushAsync();
}
```

You can also use the `GetFeaturesAsync()` method which returns a dictionary of all features:

```csharp
// Get the current features (uses company, user and custom context to
// evaluate the features)
var features = await featureClient.GetFeaturesAsync(context);
var bothEnabled = 
    features.TryGetValue("huddle", out var huddle) && huddle.Enabled && 
    features.TryGetValue("voiceHuddle", out var voiceHuddle) && voiceHuddle.Enabled;
```

`GetFeaturesAsync()` returns a dictionary of `EvaluatedFeature` objects. The `GetFeatureAsync()`
method, on the other hand, returns `IFeature` or `IFeature<TPayload>` instances. The difference
is that the latter ones will emit events to Bucket whenever `Enabled` or `Config` properties
are read. Additionally, `EvaluatedFeature` contains more properties which are useful for
debugging purposes.

## Operation modes

Bucket SDK supports three operation modes:

- Offline mode, useful during development and debugging.
- Remote evaluation mode that contacts Bucket each time a feature needs to be
  evaluated. **This mode is not recommended for high performance or reliability workloads.**
- Local evaluation mode is the default and the recommended mode to use Bucket. It uses
  features definitions that are periodically downloaded from the server to evaluate features
  locally.

You can select the desired operation mode during the configuration of the client:

```csharp
using Bucket.Sdk;

var featureClient = new FeatureClient(new Configuration
{
    SecretKey = Environment.GetEnvironmentVariable("BUCKET_SECRET_KEY"),
    Mode = OperationMode.Offline,
});

// This call will be ignored in `offline` or `remote`.
await featureClient.RefreshAsync();

// This call will be ignored in `offline` mode.
await featureClient.FlushAsync();
```

## High performance feature targeting

When Bucket SDK is running in `local` mode, it contacts the Bucket servers when
you call `RefreshAsync()` and downloads the features definitions with their targeting
rules. These rules are then matched against the user/company information you provide
to `GetFeatureAsync()` or `GetFeaturesAsync()` methods.

This means these calls do not need to contact the Bucket servers once `RefreshAsync()`
has completed. `FeatureClient` will continue to periodically download the targeting rules
from the Bucket servers in the background if configured to do so.

### Batch operations

The SDK automatically batches operations like user/company updates and feature events
to minimize API calls. The output buffer is configurable through the client options:

```csharp
var client = new FeatureClient(new Configuration
{
    Output = new OutputOptions
    {
        MaxMessages = 100,  // Maximum number of events to batch
        FlushInterval = TimeSpan.FromSeconds(1)  // Flush interval
    }
});
```

You can manually flush the output buffer at any time by calling `FlushAsync()`.

> [!TIP]
> It's recommended to call `FlushAsync()` before your application shuts down to
> ensure all events are sent.

### Feature definitions

Feature definitions include the rules needed to determine which features should
be enabled and which config values should be applied toa given user/company.
Feature definitions are automatically fetched when calling `RefreshAsync()`. They are
then cached and refreshed in the background.

## Error handling

The SDK is designed to fail gracefully and never throw exceptions to the caller
(unless the exception is caused by the caller). Instead, the SDK logs errors and
provides fallback behavior:

1. **Feature evaluation failures**: The SDK will always default to "default" states
   for features if they are not resolvable:

    ```csharp
    // If feature evaluation fails, `Enabled` will be `false`.
    var feature = await client.GetFeatureAsync("my-feature", context);
    ```

2. **Network Issues**: The SDK will retry operations when possible and fall back to
   safe defaults when necessary.

    ```csharp
    // This call will not fail if network issues are encountered, errors are
    // logged instead.
    await client.UpdateUserAsync(new User("userId"));
    ```

The SDK uses the standard .NET `Microsoft.Extensions.Logging`. You can configure
any logger that suits your needs and can configure the appropriate levels of
severity levels.

```csharp
// If you implement your own `CustomLogger` and pass it as a second argument
// to the constructor.
var client = new FeatureClient(configuration, new CustomLogger());
```

## Remote config (beta)

Remote config is a dynamic and flexible approach to configuring feature behavior
outside of your app – without needing to re-deploy it.

Similar to `Enabled`, each feature has a `Config` property. This configuration
is managed from within Bucket. It is managed similar to the way access to features
is managed, but instead of the binary `Enabled` you can have multiple configuration
values which are given to different user/companies.

```csharp
var features = await client.GetFeaturesAsync();
// Result will contain a dictionary that describe all features:
// {
//   "huddle" = {
//     Enabled: true,
//     Config: (Key = "gpt-3.5", Payload = { maxTokens = 10000, model = "gpt-3.5-beta1" })
//   }
//   ...
// }
```

`Key` is mandatory for a config, but if a feature has no config or no config
value was matched against the context, the `Config` will be `null`. Make sure
to check against this case when trying to use the configuration in your application.
`Payload` is an optional `Any` value for arbitrary configuration needs.

Just as `Enabled`, accessing `Config` on the `EvaluatedFeature` instances returned
by `GetFeaturesAsync` does not automatically generate a `check` event, contrary
to the `Config` property on the object returned by `GetFeatureAsync` methods.

## Configuration

The `FeatureClient` constructor accepts a `Configuration` object that allows you
to customize the SDK's behavior:

```csharp
var client = new FeatureClient(new Configuration
{
    SecretKey = "your-secret-key",
    ApiBaseUri = new Uri("https://api.bucket.co"),
    Mode = OperationMode.LocalEvaluation,
    Features = new FeaturesOptions
    {
        RefreshInterval = TimeSpan.FromMinutes(1),
        StaleAge = TimeSpan.FromHours(1)
    },
    Output = new OutputOptions
    {
        MaxMessages = 100,
        FlushInterval = TimeSpan.FromSeconds(1),
        RollingWindow = TimeSpan.FromMinutes(1)
    }
});
```

Bucket SDK can use the standard .NET `Microsoft.Extensions.Configuration` system
to load the configuration. This means that you can use any configuration provider
that meets your goals `appsettings.json`, environment or Azure AppConfiguration.

Check out [this example](../../examples/Bucket.Example.Advanced/README.md) for
details on how to configure the SDK from `appsettings.json`.

## Tracking custom events and setting custom attributes

Tracking allows events and updating user/company attributes in Bucket. For example,
if a customer changes their plan, you'll want Bucket to know about it, in order to
continue to provide up-to-date targeting information in the Bucket interface.

The following example shows how to register a new user, associate it with a company
and finally track an event:

```csharp
// Registers the user with Bucket using the provided unique ID, and
// providing a set of custom attributes:
await client.UpdateUserAsync(
    new User("user_id")
    {
        Name = "John Doe",
        ["longTimeUser"] = true,
        ["payingCustomer"] = false
    }
);

// Updates the company and associates the user to this company on Bucket side:
await client.UpdateCompanyAsync(
    new Company("company_id")
    {
        Name = "Acme Inc.",
        ["otherProperty"] = true,
    }, 
    new User("user_id")
);

// The user started a voice huddle:
await client.TrackAsync(
    new Event("huddle", new User("user_id"))
    {
        Company = new Company("company_id"),
        ["voice"] = true
    }
);
```

Some attributes are used by Bucket to improve the UI, and are recommended to provide
for easier navigation:

- `Name` — display name for `User`/`Company`.
- `Email` — the email of the user.
- `Avatar` — the URL for `User`/`Company` avatar image.

> [!IMPORTANT]
> The custom attributes you add to `Event`, `Company`, `User` or `Context` are passed
> to Bucket **as they are**. The key names are not transformed into `camelCase` and
> `object` values are not expanded. It is your responsibility
> to provide the data in the correct format.

## Remote Feature Evaluation

By default, the SDK performs feature evaluation locally after downloading feature
definitions. In some cases, you might want to evaluate features remotely on Bucket
servers. This is useful when:

- You want to ensure you're always using the latest feature definitions.
- You need to evaluate features without maintaining local feature definitions.
- You're working in a serverless environment where maintaining state is difficult.

To use remote evaluation, set the operation mode in your client configuration:

```csharp
var client = new FeatureClient(new Configuration
{
    SecretKey = "your-secret-key",
    Mode = OperationMode.RemoteEvaluation
});

// Feature evaluation will now contact Bucket servers for each request
var feature = await client.GetFeatureAsync("feature-key", context);
```

Note that remote evaluation requires a network request for each feature evaluation,
which may introduce latency and make your application dependent on Bucket's availability.

## Remote flag evaluation with stored context

If you don't want to provide context each time when evaluating feature flags but
rather you would like to utilize the attributes you sent to Bucket previously
(by calling `UpdateUserAsync` and `UpdateCompanyAsync`), you can use the remote
evaluation methods with just `UserId` and `CompanyId`.

These methods will call Bucket's servers and feature flags will be evaluated
remotely using the stored attributes:

```csharp
// Update user and company attributes
await client.UpdateUserAsync(
    new User("john_doe")
    {
        Name = "John O.",
        ["role"] = "admin",
    }
);

await client.UpdateCompanyAsync(
    new Company("acme_inc")
    {
        Name = "Acme, Inc",
        ["tier"] = "premium"
    }
);

// Later, evaluate features using just the IDs - Bucket will use the stored attributes
var features = await client.GetFeaturesAsync("acme_inc", "john_doe");

// Or for a single feature
var feature = await client.GetFeatureAsync("feature-key", "acme_inc", "john_doe");
```

## Opting out of tracking

There are use cases where you might not want to send `User`, `Company`, and
tracking events to Bucket.co. These are usually cases where you could be
impersonating another user in the system and do not want to interfere with
the data being collected by Bucket.

To disable tracking, call the `GetFeatureAsync()` with `TrackingStrategy.Disabled`:

```csharp
var context = new Context
{
    User = new User("john_doe"),
    Company = new Company("acme_inc"),
};

// Feature evaluation will happen normally, but no tracking events will be sent.
var feature = await client.GetFeatureAsync("feature-key", context, TrackingStrategy.Disabled);

// Even if the feature is enabled, calling Track() won't send any events to Bucket.
if (feature.Enabled)
{
    feature.Track(); // This won't actually send the event to Bucket
}
```

> [!IMPORTANT]
> Note that directly calling methods like `TrackAsync()`, `UpdateCompanyAsync()`,
> or `UpdateUserAsync()` on the `FeatureClient` will still send tracking data even
> if you've disabled tracking when asking for a feature.

## Local features and overrides

In some scenarios, you might want to define features locally or override existing
features from Bucket. This is particularly useful for:

- Development and testing.
- Feature gating during local development.
- Temporarily overriding feature states in production.
- Creating fallbacks when Bucket services are unreachable.

You can provide your own implementation of `ResolveLocalFeaturesAsyncDelegate` to the
`FeatureClient` constructor:

```csharp
// Define a delegate that resolves local features
async ValueTask<IEnumerable<EvaluatedFeature>> ResolveLocalFeatures(
    Context context, CancellationToken cancellationToken)
{
    return new []
    {
        // Simple feature with just enabled state
        new EvaluatedFeature("feature1", true),
        // Feature with configuration 
        new EvaluatedFeature(
            "feature2", true, (Key: "beta", Payload: new { MaxUsers = 100, Theme = "dark" })
        )
    };
};

// Create a feature client with local features (you can supply multiple resolvers):
var client = new FeatureClient(
    new Configuration
    {
        SecretKey = Environment.GetEnvironmentVariable("BUCKET_SECRET_KEY")
    },
    resolveLocalFeaturesAsync: new[] { ResolveLocalFeatures }
);
```

### Overriding remote features

By default, local features are merged with remote features from Bucket, with local
definitions taking precedence. If you want to completely override remote features,
set the `Override` property:

```csharp
// Define a local feature that overrides remote configuration
async ValueTask<IEnumerable<EvaluatedFeature>> ResolveLocalFeatures(
    Context context, CancellationToken cancellationToken)
{
    return new [] { new EvaluatedFeature("feature1", true, @override: true) };
};
```

You can also implement dynamic local features that change based on environment or
the value of the `context`. This approach gives you flexible control over feature
flags while still benefiting from Bucket's infrastructure when needed.

## Managing `Last seen`

By default `UpdateUserAsync`/`UpdateCompanyAsync` calls automatically update the
given user/company `Last seen` property on Bucket servers. You can control if
`Last seen` should be updated when the events are sent by setting `updateStrategy`:

```csharp
await client.UpdateUserAsync(new User("john_doe")
{
    Name = "John O."
}, UpdateStrategy.Active);

await client.UpdateCompanyAsync(new Company("acme_inc")
{
    Name = "Acme, Inc"
}, updateStrategy: UpdateStrategy.Inactive);
```

## Zero PII

The Bucket SDK doesn't collect any metadata and IP addresses are *not* being
stored. For tracking individual users, we recommend using something like database
ID as userId, as it's unique and does not include any PII (personal identifiable
information). If, however, you're using e.g. email address as userId, but prefer
not to send any PII to Bucket, you can hash the sensitive data before sending
it to Bucket:

```csharp
using System.Security.Cryptography;
using System.Text;

string HashString(string input)
{
    using var sha256 = SHA256.Create();
    var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
    return Convert.ToHexString(bytes).ToLowerInvariant();
}

await client.UpdateUserAsync(new User(HashString("john@example.com")));
```

## License

> MIT License Copyright (c) 2025 Bucket ApS
