// This example demonstrates how to use the Bucket SDK with OpenFeature for feature flag management in a .NET application.
// It includes the following features:
//   - Configuration from appsettings.json,
//   - Console logging,
//   - Local feature evaluation,
//   - Feature flag checking using OpenFeature client,

using Bucket.Sdk;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using OpenFeature;
using OpenFeature.Model;

// Create service collection and configure the Bucket SDK
var services = new ServiceCollection();

// Build configuration from appsettings.json
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", false)
    .Build();

// Configure Bucket SDK
services.AddOptions<Configuration>()
        .Bind(configuration.GetSection("Bucket"));

// Add console logging
services.AddLogging(builder =>
    builder.AddConsole().AddConfiguration(configuration.GetSection("Logging")));

// Configure Bucket SDK
services.AddBucketFeatures()
    // Add a local feature resolver for development/testing
    .AddLocalFeatures(new("beta-feature", true, true),   // Override remote feature
        new EvaluatedFeature("local-only-feature", true) // Local-only feature
    );

// Configure OpenFeature
services.AddOpenFeature(featureBuilder =>
    featureBuilder
        .AddHostedFeatureLifecycle() // From Hosting package
        .AddContext((contextBuilder, serviceProvider) =>
            // Optional: Add default context values that will be included in all evaluations
            contextBuilder.Set("default_context_key", "default_value")
        )
        .AddBucketFeaturesProvider()
);

// Build service provider
await using var serviceProvider = services.BuildServiceProvider();

// Get required services
var featureClient = serviceProvider.GetRequiredService<OpenFeature.FeatureClient>();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

// Create an OpenFeature evaluation context
var evaluationContext = EvaluationContext.Builder()
    .Set("userId", "test-user")
    .Set("name", "Test User")
    .Set("email", "test@example.com")
    .Set("companyId", "test-company")
    .Set("companyName", "Test Company")
    .Set("plan", "premium")
    .Set("country", "US")
    .Set("company_size", "enterprise")
    .Set("industry", "tech")
    .Build();

// Example 1: Check a feature flag using OpenFeature client
var betaFeatureResult = await featureClient.GetBooleanValueAsync("beta-feature", false, evaluationContext);
logger.LogInformation("Beta feature is {Enabled}", betaFeatureResult);

// Track an event when feature is used
if (betaFeatureResult)
{
    var details = TrackingEventDetails.Builder().Set("tracking-details", "example").Build();

    featureClient.Track("beta_feature_used", evaluationContext, details);
}

// Example 2: Get a feature with configuration
var configFeatureResult = await featureClient.GetObjectValueAsync("config-feature", new Value(), evaluationContext);
logger.LogInformation("Config feature is enabled with config: {Config}", configFeatureResult);
featureClient.Track("example_complete", evaluationContext);

// Shutdown OpenFeature (flushes any pending events)
await Api.Instance.ShutdownAsync();

// Wait for user input before exiting
Console.WriteLine("Press any key to exit...");
Console.ReadKey();
