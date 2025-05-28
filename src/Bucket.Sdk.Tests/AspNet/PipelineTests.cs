namespace Bucket.Sdk.Tests.AspNet;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

[CollectionDefinition(DisableParallelization = true)]
public sealed class PipelineTests: IAsyncDisposable
{
    private const int _timeout = 10000;

    private readonly IHost _host;
    private readonly Mock<IFeature> _mockFeature1;
    private readonly Mock<IFeature> _mockFeature2;

    public PipelineTests()
    {
        var mockFeatureClient = new Mock<IFeatureClient>(MockBehavior.Strict);
        var mockFeatureServiceBuilder = new Mock<IFeatureServiceBuilder>(MockBehavior.Strict);

        _mockFeature1 = new Mock<IFeature>(MockBehavior.Strict);
        _mockFeature2 = new Mock<IFeature>(MockBehavior.Strict);

        _ = mockFeatureClient.Setup(m => m.GetFeatureAsync(
                "feature-1", It.IsAny<Context>(), It.IsAny<TrackingStrategy>()
            )
            ).ReturnsAsync(_mockFeature1.Object);

        _ = mockFeatureClient.Setup(m => m.GetFeatureAsync(
                "feature-2", It.IsAny<Context>(), It.IsAny<TrackingStrategy>()
            )
        ).ReturnsAsync(_mockFeature2.Object);

        _host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        _ = services.AddRouting();

                        // Prepare the feature service builder
                        _ = mockFeatureServiceBuilder.Setup(m => m.Services)
                            .Returns(services);

                        // Register the mocked feature client
                        _ = services.AddSingleton(mockFeatureClient.Object);
                        _ = services.AddSingleton(new BucketFeatureServiceGuard());

                        _ = mockFeatureServiceBuilder.Object.UseContextResolver(httpContext => ValueTask.FromResult((
                            new Context { ["query"] = httpContext.Request.Query["test"].ToString() },
                            TrackingStrategy.Active)));
                    })
                    .Configure(app =>
                    {
                        _ = app.UseRouting();
                        _ = app.Use((context, next) =>
                        {
                            var mockHttpActivityFeature = new Mock<IHttpActivityFeature>(MockBehavior.Strict);
                            _ = mockHttpActivityFeature.Setup(m => m.Activity).Returns(
                                new Activity("test")
                            );

                            context.Features.Set(mockHttpActivityFeature.Object);
                            return next();
                        });

                        _ = app.UseWhenFeature("feature-1", branch =>
                            branch.Use(async (context, next) =>
                            {
                                context.Items.Add("UseWhenFeature", true);
                                await next();
                            })
                            );

                        _ = app.UseWhenNotFeature("feature-2", branch =>
                            branch.Use(async (context, next) =>
                            {
                                context.Items.Add("UseWhenNotFeature", true);
                                await next();
                            })
                        );

                        _ = app.UseEndpoints(endpoints =>
                        {
                            _ = endpoints.MapGet("/enabled", () => Results.Ok()).WithFeatureRestriction(
                                "feature-1"
                            );
                            _ = endpoints.MapGet("/disabled", () => Results.Ok()).WithFeatureRestriction(
                                "feature-2", false
                            );
                        });
                    })
            ).Build();

        _ = _host.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _host.StopAsync();
        _host.Dispose();
    }

    private async Task<HttpContext> InvokeAsync(string path, bool feature1 = true, bool feature2 = false)
    {
        _ = _mockFeature1.Setup(m => m.Enabled).Returns(feature1);
        _ = _mockFeature2.Setup(m => m.Enabled).Returns(feature2);

        var server = _host.GetTestServer();
        return await server.SendAsync(c =>
        {
            c.Request.Method = HttpMethods.Get;
            c.Request.Path = path;
            c.Request.QueryString = new QueryString("?test=test");
        });
    }

    [Fact(Timeout = _timeout)]
    public async Task Pipeline_IncludesBothAppBranches_Async()
    {
        var httpContext = await InvokeAsync("/enabled");

        Assert.True(httpContext.Items.ContainsKey("UseWhenFeature"));
        Assert.True(httpContext.Items.ContainsKey("UseWhenNotFeature"));
    }

    [Fact(Timeout = _timeout)]
    public async Task Pipeline_DoesNotIncludesBothAppBranches_Async()
    {
        var httpContext = await InvokeAsync("/enabled", false, true);

        Assert.False(httpContext.Items.ContainsKey("UseWhenFeature"));
        Assert.False(httpContext.Items.ContainsKey("UseWhenNotFeature"));
    }

    [Fact(Timeout = _timeout)]
    public async Task Pipeline_ContextIsPopulated_Async()
    {
        var httpContext = await InvokeAsync("/enabled");

        var (context, trackingStrategy) = await httpContext.GetEvaluationContextAsync();

        Assert.Equal("test", context["query"]);
        Assert.Equal(TrackingStrategy.Active, trackingStrategy);
    }

    [Fact(Timeout = _timeout)]
    public async Task Pipeline_HandlesEnabledRoutes_Async()
    {
        var httpContext = await InvokeAsync("/enabled");
        Assert.Equal((int) HttpStatusCode.OK, httpContext.Response.StatusCode);

        httpContext = await InvokeAsync("/disabled");
        Assert.Equal((int) HttpStatusCode.OK, httpContext.Response.StatusCode);
    }

    [Fact(Timeout = _timeout)]
    public async Task Pipeline_HandlesDisabledRoutes_Async()
    {
        var httpContext = await InvokeAsync("/enabled", false, true);
        Assert.Equal((int) HttpStatusCode.NotFound, httpContext.Response.StatusCode);

        httpContext = await InvokeAsync("/disabled", false, true);
        Assert.Equal((int) HttpStatusCode.NotFound, httpContext.Response.StatusCode);
    }
}
