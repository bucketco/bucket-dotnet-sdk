using Bucket.Sdk;

using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add HttpContextAccessor for tag helpers
builder.Services.AddHttpContextAccessor();

// Configure Bucket provider
builder.Services.AddBucketFeatures()
    .UseRestrictedFeatureHandler((feature, context) =>
    {
        // Return a redirect to the Unauthorized page
        return ValueTask.FromResult<IActionResult>(new RedirectToActionResult("Unauthorized", "Todo", null));
    })
    .AddLocalFeatures(new EvaluatedFeature("list-todos", true), new EvaluatedFeature("get-todo", true),
        new EvaluatedFeature("create-todo", true), new EvaluatedFeature("delete-todo", false));

var app = builder.Build();

app.UseExceptionHandler("/Todo/Error");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    "default",
    "{controller=Todo}/{action=Index}/{id?}");

app.Run();
