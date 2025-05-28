global using System.Diagnostics;

global using JetBrains.Annotations;

global using Microsoft.AspNetCore.Builder;
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.AspNetCore.Mvc.Filters;
global using Microsoft.Extensions.DependencyInjection;

using System.Runtime.CompilerServices;

// Allow unit tests to access internal members of this assembly via `Bucket.Sdk.Tests`
[assembly: InternalsVisibleTo("Bucket.Sdk.Tests")]
