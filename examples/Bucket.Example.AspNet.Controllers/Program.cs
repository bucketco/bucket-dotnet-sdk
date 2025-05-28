using Bucket.Sdk;

using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(static options => { options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0; });

// Configure Bucket provider
builder.Services.AddBucketFeatures()
       .UseRestrictedFeatureHandler((_, _) => ValueTask.FromResult<IActionResult>(new UnauthorizedResult()))
       .AddLocalFeatures(new EvaluatedFeature("list-todos", true), new EvaluatedFeature("get-todo", true),
           new EvaluatedFeature("create-todo", true), new EvaluatedFeature("delete-todo", false));

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Default endpoint to redirect to Scalar documentation
app.MapGet("/", () => Results.Redirect("/scalar"));

app.Run();
