# BenchGecko

Official .NET SDK for [BenchGecko](https://benchgecko.ai), the data layer of the AI economy. Every model. Every agent. Everything AI. Tracked.

## Overview

`BenchGecko` provides strongly-typed .NET primitives for working with LLM benchmark data. Build comparison tools, cost calculators, model selectors, and leaderboard UIs with clean C# idioms and full nullable reference type support.

The package includes:

- **Model** class with fluent builder pattern for constructing models with scores and pricing
- **BenchmarkCategory** enum covering 9 evaluation dimensions (Reasoning, Coding, Knowledge, Instruction, Multilingual, Safety, LongContext, Vision, Agentic)
- **ModelTier** classification (S through D) based on aggregate performance
- **ModelComparer** static class for head-to-head comparison, ranking, filtering, and value analysis
- **CostEstimator** static class for per-request, monthly, and multi-model cost estimation
- **BenchmarkScore** and **Pricing** records for structured data

## Installation

```bash
dotnet add package BenchGecko
```

Or via the NuGet Package Manager:

```
Install-Package BenchGecko
```

## Quick Start

```csharp
using BenchGecko;

// Define models with benchmark scores and pricing
var gpt4 = new Model("gpt-4o", "OpenAI")
    .WithContextWindow(128_000)
    .WithScore(BenchmarkCategory.Reasoning, 92.3)
    .WithScore(BenchmarkCategory.Coding, 89.1)
    .WithScore(BenchmarkCategory.Knowledge, 88.7)
    .WithPricing(inputPerMTok: 2.50, outputPerMTok: 10.00);

var claude = new Model("claude-sonnet-4", "Anthropic")
    .WithContextWindow(200_000)
    .WithScore(BenchmarkCategory.Reasoning, 94.1)
    .WithScore(BenchmarkCategory.Coding, 93.7)
    .WithScore(BenchmarkCategory.Knowledge, 91.2)
    .WithPricing(inputPerMTok: 3.00, outputPerMTok: 15.00);

// Compare across shared categories
var result = ModelComparer.Compare(gpt4, claude);
Console.WriteLine($"Winner: {result.Winner.Name}");
Console.WriteLine($"Categories compared: {result.CategoriesCompared}");
Console.WriteLine($"GPT-4o wins: {result.AWins.Count}, Claude wins: {result.BWins.Count}");
```

## Cost Estimation

Estimate inference costs for individual requests, monthly budgets, or compare across providers:

```csharp
using BenchGecko;

var model = new Model("gpt-4o", "OpenAI")
    .WithPricing(inputPerMTok: 2.50, outputPerMTok: 10.00);

// Single request cost
var cost = CostEstimator.EstimateRequest(model, inputTokens: 5_000, outputTokens: 2_000);
Console.WriteLine($"Request cost: ${cost:F4}");

// Monthly budget estimate
var monthly = CostEstimator.EstimateMonthly(
    model, dailyRequests: 1000, avgInputTokens: 3_000, avgOutputTokens: 1_000);
Console.WriteLine($"Monthly estimate: ${monthly:F2}");

// Compare costs across models
var models = new[] { gpt4, claude, gemini };
var ranked = CostEstimator.CompareCosts(models, inputTokens: 10_000, outputTokens: 5_000);
foreach (var (m, c) in ranked)
    Console.WriteLine($"  {m.Name}: ${c:F4}");
```

## Tier Classification

Models are automatically classified into performance tiers:

| Tier | Average Score | Description |
|------|--------------|-------------|
| S | 90+ | Elite frontier models |
| A | 80-89 | Strong general-purpose models |
| B | 70-79 | Capable mid-range models |
| C | 60-69 | Budget or older generation |
| D | <60 | Entry-level or legacy |

```csharp
// Filter by tier
var eliteModels = ModelComparer.FilterByTier(models, ModelTier.S);

// Rank by specific category
var codingLeaders = ModelComparer.RankByCategory(models, BenchmarkCategory.Coding);

// Find best value (performance per dollar)
var bestDeal = ModelComparer.BestValue(models);
Console.WriteLine($"Best value: {bestDeal?.Name} (score/dollar: {bestDeal?.ValueScore:F1})");
```

## Benchmark Categories

The `BenchmarkCategory` enum covers the major evaluation dimensions tracked by [BenchGecko](https://benchgecko.ai):

| Category | Typical Benchmarks |
|----------|-------------------|
| Reasoning | GSM8K, MATH, ARC-Challenge |
| Coding | HumanEval, MBPP, SWE-bench |
| Knowledge | MMLU, HellaSwag, TriviaQA |
| Instruction | MT-Bench, AlpacaEval |
| Multilingual | MGSM, XLSum |
| Safety | TruthfulQA, BBQ |
| LongContext | RULER, Needle-in-a-Haystack |
| Vision | MMMU, MathVista |
| Agentic | WebArena, SWE-bench |

## Data Source

Benchmark data, model metadata, and pricing information are maintained by [BenchGecko](https://benchgecko.ai), the data layer of the AI economy. Query thousands of AI models with cross-provider pricing and daily price history. Track company valuations, funding timelines, and revenue estimates. Pull benchmark scores, agent leaderboards, and a live changelog of every price drop, every launch, every deprecation. If it moved in AI today, it's already on BenchGecko.

## License

MIT
