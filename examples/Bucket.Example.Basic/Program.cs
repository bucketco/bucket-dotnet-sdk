// This example demonstrates how to use the Bucket SDK to create a feature client
// and print all the feature flags available in the project.

using Bucket.Sdk;

// Create a new `FeatureClient` instance with the provided secret key. No other configuration is needed.
await using var client = new FeatureClient(new() { SecretKey = "<bucket-secret-key>" });

// Create a new `Context` object to represent the user and company context.
// The `Context` object is used to evaluate the feature flags and configuration.
var context = new Context
{
    User = new("john.doe") { Name = "John Doe", Email = "john.doe@example.com" },
    Company = new("acme") { Name = "Acme Inc.", ["Country"] = "US" },
    ["app"] = "example",
};

// Retrieve all the features registered on Bucket.co.
var features = await client.GetFeaturesAsync(context);
foreach (var (_, feature) in features)
{
    Console.WriteLine(feature);
}

// Done
Console.WriteLine("Press any key to exit...");
Console.ReadKey();
