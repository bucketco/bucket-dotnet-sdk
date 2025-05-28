namespace Bucket.Sdk;

/// <summary>
///     Extension methods for <see cref="IApplicationBuilder" /> to add feature management
///     integration for ASP.NET application building.
/// </summary>
[PublicAPI]
public static class ApplicationBuilderExtensions
{
    // This is a private helper method that powers the `UseWhenFeature` / `UseWhenNotFeature` functionality
    // It creates a conditional branch in the middleware pipeline based on a feature flag
    private static IApplicationBuilder UseWhenFeature(
        IApplicationBuilder app,
        string featureKey,
        bool enabled,
        Action<IApplicationBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentException.ThrowIfNullOrEmpty(featureKey);
        ArgumentNullException.ThrowIfNull(configure);

        // Ensure the Bucket feature service is registered
        BucketFeatureServiceGuard.EnsureRegistered(app.ApplicationServices);

        // Create branch pipeline and configure it
        var branchBuilder = app.New();
        configure(branchBuilder);

        // Return middleware that conditionally executes the branch or continues with main pipeline
        return app.Use(next =>
        {
            // Set up the branch to run the main pipeline after its own middleware
            branchBuilder.Run(next);
            var branchPipeline = branchBuilder.Build();

            // Create the actual middleware that does the feature check
            return async context =>
            {
                // Get the feature flag status for the current request
                var feature = await context.GetFeatureAsync(featureKey);

                if (feature.Enabled == enabled)
                {
                    // Feature condition matches - execute branch pipeline
                    await branchPipeline(context);
                }
                else
                {
                    // Feature condition doesn't match - continue with main pipeline
                    await next(context);
                }
            };
        });
    }

    /// <summary>
    ///     Creates a branch in the request pipeline that is activated based on a feature being enabled.
    ///     This method joins the pipeline with the main pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="featureKey">The feature key that needs to be enabled for the request to take the branch.</param>
    /// <param name="configure">Action that configures the branch.</param>
    /// <returns>The current <see cref="IApplicationBuilder" /> for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when either <paramref name="app" /> or <paramref name="configure" /> are
    ///     <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="featureKey" /> is <see langword="null"/> or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the Bucket feature service is not registered.</exception>
    public static IApplicationBuilder UseWhenFeature(
        this IApplicationBuilder app,
        string featureKey,
        Action<IApplicationBuilder> configure) => UseWhenFeature(app, featureKey, true, configure);

    /// <summary>
    ///     Creates a branch in the request pipeline that is activated based on a feature being disabled.
    ///     This method joins the pipeline with the main pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="featureKey">The feature key that needs to be disabled for the request to take the branch.</param>
    /// <param name="configure">Action that configures the branch.</param>
    /// <returns>The current <see cref="IApplicationBuilder" /> for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when either <paramref name="app" /> or <paramref name="configure" /> are
    ///     <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="featureKey" /> is <see langword="null"/> or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the Bucket feature service is not registered.</exception>
    public static IApplicationBuilder UseWhenNotFeature(
        this IApplicationBuilder app,
        string featureKey,
        Action<IApplicationBuilder> configure) => UseWhenFeature(app, featureKey, false, configure);

    /// <summary>
    ///     Creates a branch with custom middleware in the request pipeline that is activated based on
    ///     a feature being enabled. This method joins the pipeline with the main pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="featureKey">The feature key that needs to be enabled for the request to take the branch.</param>
    /// <returns>The current <see cref="IApplicationBuilder" /> for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="app" /> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="featureKey" /> is <see langword="null"/> or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the Bucket feature service is not registered.</exception>
    public static IApplicationBuilder UseMiddlewareWhenFeature<T>(this IApplicationBuilder app, string featureKey)
    {
        ArgumentNullException.ThrowIfNull(app);

        return UseWhenFeature(app, featureKey, true, branch => branch.UseMiddleware<T>());
    }

    /// <summary>
    ///     Creates a branch with custom middleware in the request pipeline that is activated based on a
    ///     feature being disabled. This method joins the pipeline with the main pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="featureKey">The feature key that needs to be disabled for the request to take the branch.</param>
    /// <returns>The current <see cref="IApplicationBuilder" /> for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="app" /> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="featureKey" /> is <see langword="null"/> or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the Bucket feature service is not registered.</exception>
    public static IApplicationBuilder UseMiddlewareWhenNotFeature<T>(this IApplicationBuilder app, string featureKey)
    {
        ArgumentNullException.ThrowIfNull(app);

        return UseWhenFeature(app, featureKey, false, branch => branch.UseMiddleware<T>());
    }
}
