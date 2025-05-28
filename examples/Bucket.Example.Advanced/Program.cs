// This example demonstrates how to use the Bucket SDK for feature flag management in a .NET application.
// It includes the following features:
//   - Configuration from appsettings.json,
//   - Console logging,
//   - Local feature evaluation,
//   - Feature flag checking,

using Bucket.Sdk;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

// Build service provider
await using var serviceProvider = services.BuildServiceProvider();

// Get required services
var featureClient = serviceProvider.GetRequiredService<IFeatureClient>();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

// Create a context for feature evaluation
var context = new Context
{
    // User information
    User = new("test-user"),

    //Company information
    Company = new("test-company"),

    // Add context attributes
    ["plan"] = "premium",
    ["country"] = "US",
    ["company_size"] = "enterprise",
    ["industry"] = "tech",
};


// Example 1: Check a feature flag
var betaFeature = await featureClient.GetFeatureAsync("beta-feature", context);
logger.LogInformation("Beta feature is {Enabled}", betaFeature.Enabled);

// Track feature usage
betaFeature.Track();

// Example 2: Get a feature with configuration
var configFeature = await featureClient.GetFeatureAsync<Dictionary<string, object>>("config-feature", context);
if (configFeature.Enabled)
{
    var config = configFeature.Config.Payload;
    logger.LogInformation("Config feature is enabled with config: {Config}", config);
}

// Example 3: Get all features
var allFeatures = await featureClient.GetFeaturesAsync(context);
logger.LogInformation("Available features: {Features}",
    string.Join(", ", allFeatures.Select(f => $"{f.Key}: {f.Value.Enabled}")));

// Example 4: Track an event
await featureClient.TrackAsync(new("feature_used", context.User) { Company = context.Company });

// Flush any pending events
await featureClient.FlushAsync();

// Wait for user input before exiting
Console.WriteLine("Press any key to exit...");
Console.ReadKey();
