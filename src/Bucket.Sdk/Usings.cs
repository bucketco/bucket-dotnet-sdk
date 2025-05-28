global using System.Collections;
global using System.Collections.Immutable;
global using System.Diagnostics;
global using System.Globalization;
global using System.Text;
global using System.Text.Json;
global using System.Text.Json.Serialization;

global using JetBrains.Annotations;

global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Bucket.Sdk.Tests")]

// TODO: use a memory cache for the features
