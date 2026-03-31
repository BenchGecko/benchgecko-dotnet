using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace BenchGecko
{
    /// <summary>
    /// Exception thrown when the BenchGecko API returns an error.
    /// </summary>
    public class BenchGeckoException : Exception
    {
        /// <summary>HTTP status code, if available.</summary>
        public int? StatusCode { get; }

        public BenchGeckoException(string message, int? statusCode = null)
            : base(message)
        {
            StatusCode = statusCode;
        }
    }

    /// <summary>
    /// Represents an AI model tracked by BenchGecko.
    /// </summary>
    public class Model
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("slug")]
        public string? Slug { get; set; }

        [JsonPropertyName("provider")]
        public string? Provider { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extra { get; set; }
    }

    /// <summary>
    /// Represents a benchmark tracked by BenchGecko.
    /// </summary>
    public class Benchmark
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("slug")]
        public string? Slug { get; set; }

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extra { get; set; }
    }

    /// <summary>
    /// Result of comparing two or more AI models.
    /// </summary>
    public class ComparisonResult
    {
        [JsonPropertyName("models")]
        public List<JsonElement>? Models { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extra { get; set; }
    }

    /// <summary>
    /// Client for the BenchGecko API.
    /// Provides async methods to query AI models, benchmarks, and perform comparisons.
    /// </summary>
    public class BenchGeckoClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly bool _ownsHttpClient;

        /// <summary>
        /// Create a new BenchGecko client with default settings.
        /// </summary>
        public BenchGeckoClient() : this("https://benchgecko.ai", null) { }

        /// <summary>
        /// Create a new BenchGecko client with a custom base URL.
        /// </summary>
        public BenchGeckoClient(string baseUrl, HttpClient? httpClient = null)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _ownsHttpClient = httpClient == null;
            _httpClient = httpClient ?? new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("benchgecko-dotnet/0.1.0");
            _httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        }

        /// <summary>
        /// List all AI models tracked by BenchGecko.
        /// </summary>
        public async Task<List<Model>> ModelsAsync(CancellationToken ct = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/models", ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                throw new BenchGeckoException(
                    $"API error: {body}", (int)response.StatusCode);
            }
            return await response.Content.ReadFromJsonAsync<List<Model>>(cancellationToken: ct)
                ?? new List<Model>();
        }

        /// <summary>
        /// List all benchmarks tracked by BenchGecko.
        /// </summary>
        public async Task<List<Benchmark>> BenchmarksAsync(CancellationToken ct = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/benchmarks", ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                throw new BenchGeckoException(
                    $"API error: {body}", (int)response.StatusCode);
            }
            return await response.Content.ReadFromJsonAsync<List<Benchmark>>(cancellationToken: ct)
                ?? new List<Benchmark>();
        }

        /// <summary>
        /// Compare two or more AI models side by side.
        /// </summary>
        public async Task<ComparisonResult> CompareAsync(
            string[] models, CancellationToken ct = default)
        {
            if (models.Length < 2)
                throw new ArgumentException("At least 2 models are required for comparison.");

            var query = string.Join(",", models);
            var response = await _httpClient.GetAsync(
                $"{_baseUrl}/api/v1/compare?models={Uri.EscapeDataString(query)}", ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                throw new BenchGeckoException(
                    $"API error: {body}", (int)response.StatusCode);
            }
            return await response.Content.ReadFromJsonAsync<ComparisonResult>(cancellationToken: ct)
                ?? new ComparisonResult();
        }

        public void Dispose()
        {
            if (_ownsHttpClient)
                _httpClient.Dispose();
        }
    }
}
