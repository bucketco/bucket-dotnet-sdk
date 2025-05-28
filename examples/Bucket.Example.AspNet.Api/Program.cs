// This example illustrates the use of `AddBucketFeatures`, `AddLocalFeatures`, `UseRestrictedFeatureActionHandler`,
// and `WithFeatureRestriction` methods to configure a very simple API project.

using Bucket.Sdk;

using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;

using Scalar.AspNetCore;

// Create a wb application builder
var builder = WebApplication.CreateBuilder(args);

// Setup all required services
builder.Services.AddLogging()
       .AddOpenApi(static options => { options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0; });

// Configure Bucket provider
builder.Services.AddBucketFeatures()
       .UseRestrictedFeatureHandler((_, _) => ValueTask.FromResult<object?>(Results.Unauthorized()))
       .AddLocalFeatures(new EvaluatedFeature("list-todos", true), new EvaluatedFeature("get-todo", true),
           new EvaluatedFeature("create-todo", true), new EvaluatedFeature("delete-todo", false));

// Build the application
var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();
app.UseHttpsRedirection();

var todos = new Dictionary<Guid, ToDo>();

// Register the endpoints
app.MapGet("/todos", () => Results.Ok(todos.Values))
   .WithFeatureRestriction("list-todos")
   .WithDescription("Gets all existing TODO items");

app.MapGet("/todos/{id:guid}",
       (Guid id) => todos.TryGetValue(id, out var todo) ? Results.Ok((object?) todo) : Results.NotFound())
   .WithFeatureRestriction("get-todo")
   .WithDescription("Gets an existing TODO item");

app.MapPost("/todos", ([FromBody] ToDo todo) =>
   {
       todos.Add(todo.Id, todo);
       return Results.Ok(todo);
   })
   .WithFeatureRestriction("create-todo")
   .WithDescription("Creates a new TODO item");

app.MapDelete("/todos/{id:guid}",
       (Guid id) => todos.Remove(id, out var todo) ? Results.Ok((object?) todo) : Results.NotFound())
   .WithFeatureRestriction("delete-todo")
   .WithDescription("Deletes an existing TODO item");

// Default endpoint to redirect to Scalar documentation
app.MapGet("/", () => Results.Redirect("/scalar"));

app.Run();

// The type of the `TO-DO` item
internal sealed record ToDo(string Content)
{
    internal Guid Id { get; } = Guid.NewGuid();
}
