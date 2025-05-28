# Bucket sync job example

This example demonstrates how to use the Bucket SDK to perform periodic updates of company
and user data in a background job.

## Overview

The example shows a common use case where company attributes that don't change frequently
(like `revenue`, `plan`, etc.) need to be synchronized periodically rather than in real-time.

## Key components

- The example uses `FeatureClient` to manage company and user updates.
- It demonstrates batch processing of company and user data.
- It uses the `UpdateStrategy.Inactive` strategy for updates to ensure that entities
  are not marked as active in Bucket.

## Code explanation

- The `EnumerateClients()` method simulates fetching company and user data from your system.
- Each company is represented with its name and associated user IDs.
- The main loop processes each company and its users sequentially.
- The `UpdateCompanyAsync` method is called for each user in the company.
- `FlushAsync()` is called at the end to ensure all updates are sent to Bucket.

## Why this approach?

- Background jobs are ideal for non-critical updates that don't need real-time processing.
- Using `UpdateStrategy.Inactive` is used to ensure that entities are not marked as active in Bucket.
- This pattern is commonly used for nightly syncs or periodic data updates.
- It helps reduce the load on your main application by moving non-urgent updates to a separate process.

## Usage

1. Replace `<bucket-secret-key>` with your actual Bucket secret key.
2. Modify the `enumerateClients()` method to fetch real company and user data from your system.
3. Schedule this job to run at your desired interval (e.g., nightly via a cron job).

## Best practices

- Always call `FlushAsync()` before exiting the application to ensure all data is sent to Bucket.
- Consider adding error handling and retry logic for production use.
- Monitor the job's performance and adjust the batch size if needed.
- Log the results of the sync for debugging and auditing purposes.

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
