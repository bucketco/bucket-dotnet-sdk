// This example demonstrates how to use the Bucket SDK to update company details and associated users
// in a background job.
// Usually, there are company attributes that are not updated in real-time, such as: `revenue`, `plan`, etc.
// Using a background job, you can update these attributes periodically.

using Bucket.Sdk;

// Example method that simulates enumerating client companies and their associated user IDs.
static IEnumerable<(Company company, string[] userIds)> enumerateClients()
{
    {
        yield return (new("company1")
        {
            Name = "Company 1"
        }, ["user1", "user2"]);
        yield return (new("company2")
        {
            Name = "Company 2"
        }, ["user3", "user4"]);
        yield return (new("company3")
        {
            Name = "Company 3"
        }, ["user5", "user6"]);
    }
}

// Create a new `FeatureClient` instance with the provided secret key. No other configuration is needed.
await using var client = new FeatureClient(new() { SecretKey = "<bucket-secret-key>" });

// Enumerate all companies that are clients of this company and all the users that are part of them.
// This job can be run nightly to update company details and all the associated users.
foreach (var (company, userIds) in enumerateClients())
{
    foreach (var userId in userIds)
    {
        await client.UpdateCompanyAsync(company, new(userId), UpdateStrategy.Inactive);
    }
}

await client.FlushAsync();
