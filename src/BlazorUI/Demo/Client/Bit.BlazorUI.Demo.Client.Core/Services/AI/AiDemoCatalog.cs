using System.ComponentModel;

namespace Bit.BlazorUI.Demo.Client.Core.Services.AI;

/// <summary>
/// The models, providers, and tools the AI demos offer.
/// </summary>
/// <remarks>
/// The tools are created through the overloads of <see cref="BitAiTool"/> that take the JSON metadata of
/// <see cref="AiDemoJsonContext"/>, so their arguments and results are read and written without reflection - the demo
/// is a trimmed WebAssembly app, like the apps it is written for.
/// </remarks>
public static class AiDemoCatalog
{
    /// <summary>The model selected by default.</summary>
    public const string DefaultModelId = "demo-model-1";

    /// <summary>The providers shown in the model picker.</summary>
    public static IReadOnlyList<BitAiProvider> Providers { get; } =
    [
        new BitAiProvider("demo", "Demo Provider") { Website = "https://example.com" },
        new BitAiProvider("local", "Local Runtime"),
    ];

    /// <summary>The models shown in the model picker.</summary>
    public static IReadOnlyList<BitAiModel> Models { get; } =
    [
        new BitAiModel("demo-model-1", "Demo Large")
        {
            ProviderId = "demo",
            Family = "Demo Provider",
            Description = "The general model. Slower, better at long reasoning.",
            Capabilities = BitAiModelCapabilities.Streaming | BitAiModelCapabilities.Tools | BitAiModelCapabilities.Vision
                | BitAiModelCapabilities.Reasoning | BitAiModelCapabilities.StructuredOutput | BitAiModelCapabilities.Citations,
            ContextWindow = 200_000,
            MaxOutputTokens = 16_000,
            InputPricePerMillion = 3m,
            OutputPricePerMillion = 15m,
        },
        new BitAiModel("demo-model-1-fast", "Demo Fast")
        {
            ProviderId = "demo",
            Family = "Demo Provider",
            Description = "Cheaper and quicker. No reasoning output.",
            Capabilities = BitAiModelCapabilities.Streaming | BitAiModelCapabilities.Tools
                | BitAiModelCapabilities.StructuredOutput,
            ContextWindow = 128_000,
            MaxOutputTokens = 8_000,
            InputPricePerMillion = 0.5m,
            OutputPricePerMillion = 1.5m,
        },
        new BitAiModel("local-small", "Local Small")
        {
            ProviderId = "local",
            Family = "Local Runtime",
            Description = "Runs on the machine. No network, no cost.",
            Capabilities = BitAiModelCapabilities.Streaming,
            ContextWindow = 32_000,
            InputPricePerMillion = 0m,
            OutputPricePerMillion = 0m,
        },
    ];

    /// <summary>The tools the demo registers.</summary>
    /// <returns>The tools.</returns>
    public static IReadOnlyList<IBitAiTool> CreateTools() =>
    [
        BitAiTool.Create(
            "get_weather",
            "Look up the current weather for a city.",
            AiDemoJsonContext.Default.WeatherArgs,
            AiDemoJsonContext.Default.WeatherResult,
            async (args, token) =>
            {
                await Task.Delay(700, token);
                return Weather(args.City);
            },
            definition => definition with { Title = "Weather", Icon = BitIconName.Globe, Risk = BitAiToolRisk.ReadOnly }),

        BitAiTool.Create(
            "search_docs",
            "Search the product documentation and return matching passages.",
            AiDemoJsonContext.Default.SearchArgs,
            async (args, invocation, token) =>
            {
                invocation.Progress.Report(new BitAiToolProgress(0.2, "Querying the index"));
                await Task.Delay(500, token);
                invocation.Progress.Report(new BitAiToolProgress(0.7, "Ranking results"));
                await Task.Delay(500, token);

                var source = new BitAiCitationSource("docs-render-modes", "Render modes")
                {
                    Kind = BitAiSourceKind.Document,
                    SiteName = "Product documentation",
                    Url = "https://example.com/docs/render-modes",
                };

                return BitAiToolOutcome.Success(SearchHits(args.Query), AiDemoJsonContext.Default.SearchResult) with
                {
                    Text = $"2 passages matched “{args.Query}”.",
                    Citations = [new BitAiCitation(BitAiId.New("cit"), source)],
                };
            },
            definition => definition with { Title = "Search docs", Icon = BitIconName.Search }),

        BitAiTool.Create(
            "delete_records",
            "Permanently delete records matching a filter.",
            AiDemoJsonContext.Default.DeleteArgs,
            AiDemoJsonContext.Default.DeleteResult,
            async (args, token) =>
            {
                await Task.Delay(600, token);
                return new DeleteResult(args.Filter, 128);
            },
            definition => definition with
            {
                Title = "Delete records",
                Icon = BitIconName.Delete,
                Risk = BitAiToolRisk.Destructive,
            }),
    ];

    /// <summary>The two passages the documentation search always finds.</summary>
    /// <param name="query">What was searched for.</param>
    /// <returns>The result.</returns>
    public static SearchResult SearchHits(string query) => new(query,
    [
        new SearchHit("Render modes", 0.94),
        new SearchHit("Circuit lifetime", 0.81),
    ]);

    private static WeatherResult Weather(string city) => city.ToLowerInvariant() switch
    {
        "oslo" => new WeatherResult("Oslo", -3, "Snow", 81),
        "lisbon" => new WeatherResult("Lisbon", 17, "Clear", 54),
        "auckland" => new WeatherResult("Auckland", 22, "Showers", 72),
        _ => new WeatherResult(city, 14, "Cloudy", 66),
    };

    /// <summary>Arguments for the weather tool.</summary>
    /// <param name="City">The city to look up.</param>
    /// <param name="Days">How many days ahead to forecast.</param>
    public sealed record WeatherArgs(
        [property: Description("The city to look up, for example Oslo.")] string City,
        [property: Description("How many days ahead to forecast.")] int Days = 1);

    /// <summary>The weather for one city.</summary>
    /// <param name="City">The city.</param>
    /// <param name="Degrees">The temperature in Celsius.</param>
    /// <param name="Condition">A one-word summary.</param>
    /// <param name="Humidity">Relative humidity as a percentage.</param>
    public sealed record WeatherResult(string City, int Degrees, string Condition, int Humidity);

    /// <summary>Arguments for the documentation search tool.</summary>
    /// <param name="Query">What to search for.</param>
    public sealed record SearchArgs(string Query);

    /// <summary>What the documentation search found.</summary>
    /// <param name="Query">What was searched for.</param>
    /// <param name="Hits">The matching passages, the best first.</param>
    public sealed record SearchResult(string Query, IReadOnlyList<SearchHit> Hits);

    /// <summary>One passage the documentation search found.</summary>
    /// <param name="Title">The passage's title.</param>
    /// <param name="Score">How well it matches, from 0 to 1.</param>
    public sealed record SearchHit(string Title, double Score);

    /// <summary>Arguments for the destructive delete tool.</summary>
    /// <param name="Filter">Which records to delete.</param>
    public sealed record DeleteArgs(string Filter);

    /// <summary>The outcome of a delete.</summary>
    /// <param name="Filter">The filter that was applied.</param>
    /// <param name="Deleted">How many records were removed.</param>
    public sealed record DeleteResult(string Filter, int Deleted);
}
