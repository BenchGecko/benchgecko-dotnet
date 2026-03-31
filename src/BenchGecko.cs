namespace BenchGecko;

/// <summary>
/// Categories of AI benchmarks tracked by BenchGecko.
/// </summary>
public enum BenchmarkCategory
{
    Reasoning,
    Coding,
    Knowledge,
    Instruction,
    Multilingual,
    Safety,
    LongContext,
    Vision,
    Agentic
}

/// <summary>
/// Performance tier assigned to a model based on aggregate scores.
/// S = elite (90+), A = strong (80-89), B = capable (70-79), C = budget (60-69), D = legacy (&lt;60).
/// </summary>
public enum ModelTier { S, A, B, C, D }

/// <summary>
/// A benchmark score for a specific evaluation category.
/// </summary>
/// <param name="Category">The benchmark category.</param>
/// <param name="Score">Score value between 0.0 and 100.0.</param>
public record BenchmarkScore(BenchmarkCategory Category, double Score)
{
    /// <summary>Score clamped to valid range.</summary>
    public double ClampedScore => Math.Clamp(Score, 0.0, 100.0);
}

/// <summary>
/// Pricing information for a model in USD per million tokens.
/// </summary>
/// <param name="InputPerMTok">Input price per million tokens.</param>
/// <param name="OutputPerMTok">Output price per million tokens.</param>
public record Pricing(double InputPerMTok, double OutputPerMTok)
{
    /// <summary>Blended price (input + output) per million tokens.</summary>
    public double BlendedPerMTok => InputPerMTok + OutputPerMTok;
}

/// <summary>
/// An AI model with benchmark scores and optional pricing.
/// Use the builder methods to construct models fluently.
/// </summary>
public class Model
{
    public string Name { get; }
    public string Provider { get; }
    public long? ContextWindow { get; private set; }
    public Pricing? Pricing { get; private set; }

    private readonly Dictionary<BenchmarkCategory, double> _scores = new();

    /// <summary>All recorded benchmark scores.</summary>
    public IReadOnlyDictionary<BenchmarkCategory, double> Scores => _scores;

    public Model(string name, string provider)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    /// <summary>Set the context window size in tokens.</summary>
    public Model WithContextWindow(long tokens)
    {
        ContextWindow = tokens;
        return this;
    }

    /// <summary>Add a benchmark score (clamped to 0-100).</summary>
    public Model WithScore(BenchmarkCategory category, double score)
    {
        _scores[category] = Math.Clamp(score, 0.0, 100.0);
        return this;
    }

    /// <summary>Set pricing in USD per million tokens.</summary>
    public Model WithPricing(double inputPerMTok, double outputPerMTok)
    {
        Pricing = new Pricing(inputPerMTok, outputPerMTok);
        return this;
    }

    /// <summary>Average score across all benchmarked categories, or null if none.</summary>
    public double? AverageScore =>
        _scores.Count > 0 ? _scores.Values.Average() : null;

    /// <summary>Performance tier based on average score, or null if no scores.</summary>
    public ModelTier? Tier => AverageScore switch
    {
        >= 90.0 => ModelTier.S,
        >= 80.0 => ModelTier.A,
        >= 70.0 => ModelTier.B,
        >= 60.0 => ModelTier.C,
        < 60.0 => ModelTier.D,
        _ => null
    };

    /// <summary>
    /// Value score: average benchmark performance divided by blended token price.
    /// Higher is better. Returns null if pricing or scores are missing.
    /// </summary>
    public double? ValueScore
    {
        get
        {
            if (AverageScore is not { } avg || Pricing is not { } pricing)
                return null;
            var blended = pricing.BlendedPerMTok;
            return blended > 0 ? avg / blended : null;
        }
    }

    /// <summary>Get the score for a specific category, or null.</summary>
    public double? GetScore(BenchmarkCategory category) =>
        _scores.TryGetValue(category, out var score) ? score : null;

    public override string ToString()
    {
        var tier = Tier is { } t ? $" [{t}-Tier]" : "";
        return $"{Name} ({Provider}){tier}";
    }
}

/// <summary>
/// Result of comparing two models across shared benchmark categories.
/// </summary>
public class ComparisonResult
{
    public Model ModelA { get; }
    public Model ModelB { get; }

    /// <summary>Score deltas by category (positive = A leads).</summary>
    public IReadOnlyDictionary<BenchmarkCategory, double> Deltas { get; }

    /// <summary>Categories where Model A scores higher.</summary>
    public IReadOnlyList<BenchmarkCategory> AWins { get; }

    /// <summary>Categories where Model B scores higher.</summary>
    public IReadOnlyList<BenchmarkCategory> BWins { get; }

    /// <summary>Categories with identical scores.</summary>
    public IReadOnlyList<BenchmarkCategory> Ties { get; }

    internal ComparisonResult(
        Model a, Model b,
        Dictionary<BenchmarkCategory, double> deltas,
        List<BenchmarkCategory> aWins,
        List<BenchmarkCategory> bWins,
        List<BenchmarkCategory> ties)
    {
        ModelA = a;
        ModelB = b;
        Deltas = deltas;
        AWins = aWins;
        BWins = bWins;
        Ties = ties;
    }

    /// <summary>The model with the higher average score. Ties favor Model A.</summary>
    public Model Winner
    {
        get
        {
            var avgA = ModelA.AverageScore ?? 0;
            var avgB = ModelB.AverageScore ?? 0;
            return avgB > avgA ? ModelB : ModelA;
        }
    }

    /// <summary>Number of categories included in the comparison.</summary>
    public int CategoriesCompared => Deltas.Count;
}

/// <summary>
/// Static utilities for comparing models and estimating costs.
/// Core analysis functions for the BenchGecko SDK.
/// </summary>
public static class ModelComparer
{
    /// <summary>
    /// Compare two models across all mutually-scored categories.
    /// </summary>
    public static ComparisonResult Compare(Model a, Model b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        var deltas = new Dictionary<BenchmarkCategory, double>();
        var aWins = new List<BenchmarkCategory>();
        var bWins = new List<BenchmarkCategory>();
        var ties = new List<BenchmarkCategory>();

        foreach (var (category, scoreA) in a.Scores)
        {
            if (b.GetScore(category) is not { } scoreB)
                continue;

            var delta = scoreA - scoreB;
            deltas[category] = delta;

            if (Math.Abs(delta) < 0.001)
                ties.Add(category);
            else if (delta > 0)
                aWins.Add(category);
            else
                bWins.Add(category);
        }

        return new ComparisonResult(a, b, deltas, aWins, bWins, ties);
    }

    /// <summary>
    /// Rank a collection of models by score in a specific category (descending).
    /// Models without a score in that category are excluded.
    /// </summary>
    public static IReadOnlyList<(Model Model, double Score)> RankByCategory(
        IEnumerable<Model> models, BenchmarkCategory category)
    {
        return models
            .Select(m => (Model: m, Score: m.GetScore(category)))
            .Where(x => x.Score.HasValue)
            .OrderByDescending(x => x.Score!.Value)
            .Select(x => (x.Model, x.Score!.Value))
            .ToList();
    }

    /// <summary>
    /// Filter models by performance tier.
    /// </summary>
    public static IReadOnlyList<Model> FilterByTier(IEnumerable<Model> models, ModelTier tier)
    {
        return models.Where(m => m.Tier == tier).ToList();
    }

    /// <summary>
    /// Find the model with the best value score (performance per dollar).
    /// </summary>
    public static Model? BestValue(IEnumerable<Model> models)
    {
        return models
            .Where(m => m.ValueScore.HasValue)
            .OrderByDescending(m => m.ValueScore!.Value)
            .FirstOrDefault();
    }
}

/// <summary>
/// Inference cost estimation utilities.
/// </summary>
public static class CostEstimator
{
    /// <summary>
    /// Estimate the cost of a single request in USD.
    /// </summary>
    /// <param name="model">Model with pricing information.</param>
    /// <param name="inputTokens">Number of input tokens.</param>
    /// <param name="outputTokens">Number of output tokens.</param>
    /// <returns>Estimated cost in USD, or null if pricing is unavailable.</returns>
    public static double? EstimateRequest(Model model, long inputTokens, long outputTokens)
    {
        if (model.Pricing is not { } pricing)
            return null;

        var inputCost = (inputTokens / 1_000_000.0) * pricing.InputPerMTok;
        var outputCost = (outputTokens / 1_000_000.0) * pricing.OutputPerMTok;
        return inputCost + outputCost;
    }

    /// <summary>
    /// Estimate monthly cost assuming a daily request volume.
    /// </summary>
    /// <param name="model">Model with pricing information.</param>
    /// <param name="dailyRequests">Number of requests per day.</param>
    /// <param name="avgInputTokens">Average input tokens per request.</param>
    /// <param name="avgOutputTokens">Average output tokens per request.</param>
    /// <returns>Estimated monthly cost in USD, or null if pricing is unavailable.</returns>
    public static double? EstimateMonthly(
        Model model, int dailyRequests, long avgInputTokens, long avgOutputTokens)
    {
        var perRequest = EstimateRequest(model, avgInputTokens, avgOutputTokens);
        return perRequest * dailyRequests * 30;
    }

    /// <summary>
    /// Compare costs across multiple models for the same workload.
    /// Returns models sorted by cost (ascending).
    /// </summary>
    public static IReadOnlyList<(Model Model, double Cost)> CompareCosts(
        IEnumerable<Model> models, long inputTokens, long outputTokens)
    {
        return models
            .Select(m => (Model: m, Cost: EstimateRequest(m, inputTokens, outputTokens)))
            .Where(x => x.Cost.HasValue)
            .OrderBy(x => x.Cost!.Value)
            .Select(x => (x.Model, x.Cost!.Value))
            .ToList();
    }
}
