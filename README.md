# BenchGecko .NET SDK

Official .NET client for the [BenchGecko](https://benchgecko.ai) API. Query AI model data, benchmark scores, and run side-by-side comparisons from C# and .NET applications.

BenchGecko tracks every major AI model, benchmark, and provider. This package wraps the public REST API with strongly typed C# classes, async/await patterns, and full CancellationToken support.

## Installation

```bash
dotnet add package BenchGecko
```

Targets .NET 6.0 and .NET 8.0.

## Quick Start

```csharp
using BenchGecko;

var client = new BenchGeckoClient();

// List all tracked AI models
var models = await client.ModelsAsync();
Console.WriteLine($"Tracking {models.Count} models");

// List all benchmarks
var benchmarks = await client.BenchmarksAsync();
foreach (var b in benchmarks.Take(5))
    Console.WriteLine(b.Name);

// Compare two models head-to-head
var comparison = await client.CompareAsync(new[] { "gpt-4o", "claude-opus-4" });
Console.WriteLine($"Compared {comparison.Models?.Count} models");
```

## API Reference

### `new BenchGeckoClient()`

Create a client with default settings (production URL).

### `new BenchGeckoClient(string baseUrl, HttpClient? httpClient)`

Create a client with a custom base URL or injected HttpClient for testing and dependency injection scenarios.

### `ModelsAsync(CancellationToken ct)`

Fetch all AI models. Returns `List<Model>` with name, provider, slug, and extensible metadata via the `Extra` dictionary.

### `BenchmarksAsync(CancellationToken ct)`

Fetch all benchmarks. Returns `List<Benchmark>` with name, category, slug, and extra metadata.

### `CompareAsync(string[] models, CancellationToken ct)`

Compare two or more models. Pass an array of model slugs (minimum 2). Returns a `ComparisonResult` containing per-model data.

## Error Handling

API errors throw `BenchGeckoException` with a message and optional HTTP status code:

```csharp
try
{
    var models = await client.ModelsAsync();
}
catch (BenchGeckoException ex)
{
    Console.WriteLine($"API error ({ex.StatusCode}): {ex.Message}");
}
```

## Dependency Injection

The client accepts an `HttpClient` parameter, making it compatible with `IHttpClientFactory`:

```csharp
services.AddHttpClient<BenchGeckoClient>(client =>
{
    client.BaseAddress = new Uri("https://benchgecko.ai");
});
```

## Data Attribution

Data provided by [BenchGecko](https://benchgecko.ai). Model benchmark scores are sourced from official evaluation suites. Pricing data is updated daily from provider APIs.

## Links

- [BenchGecko](https://benchgecko.ai) - AI model benchmarks, pricing, and rankings
- [API Documentation](https://benchgecko.ai/api-docs)
- [GitHub Repository](https://github.com/BenchGecko/benchgecko-dotnet)

## License

MIT
