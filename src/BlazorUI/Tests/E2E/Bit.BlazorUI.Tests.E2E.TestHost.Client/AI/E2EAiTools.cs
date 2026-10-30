using System.Text;
using System.Text.Json;

namespace Bit.BlazorUI.Tests.E2E.TestHost.AI;

/// <summary>
/// The tools the E2E host registers, which the scripts of <see cref="E2EAiScripts"/> call: each answers at once and the
/// same way every time. Their arguments and results are read and written as JSON elements, with nothing reflected.
/// </summary>
public static class E2EAiTools
{
    /// <summary>Read-only, runs without asking: {"city"} to {"city","degrees","condition"}.</summary>
    public const string GetWeather = "get_weather";

    /// <summary>Destructive, so the default approval policy asks the user first: {"filter"} to {"filter","deleted"}.</summary>
    public const string DeleteRecords = "delete_records";

    /// <summary>Read-only, reports its progress half way and returns two passages with a citation.</summary>
    public const string SearchDocs = "search_docs";

    /// <summary>Read-only, and always fails (a retryable "The service is unavailable.").</summary>
    public const string FlakyService = "flaky_service";

    /// <summary>The tools, in the order a page lists them.</summary>
    public static IReadOnlyList<IBitAiTool> Create() =>
    [
        BitAiTool.Create(new BitAiToolDefinition(GetWeather)
        {
            Title = "Weather",
            Description = "Look up the current weather for a city.",
            Icon = "Sunny",
            ParametersSchema = E2EAiScriptRun.Json("""{"type":"object","properties":{"city":{"type":"string","description":"The city, for example Oslo."}},"required":["city"]}"""),
        }, (invocation, _) =>
        {
            var city = Text(invocation.Arguments, "city") ?? "Nowhere";
            var (degrees, condition) = city.ToLowerInvariant() switch
            {
                "oslo" => (-3, "Snow"),
                "lisbon" => (17, "Clear"),
                _ => (14, "Cloudy"),
            };

            var result = Object(w =>
            {
                w.WriteString("city", city);
                w.WriteNumber("degrees", degrees);
                w.WriteString("condition", condition);
            });

            return ValueTask.FromResult(BitAiToolOutcome.Success(result, $"{city}: {degrees} degrees, {condition}"));
        }),

        BitAiTool.Create(new BitAiToolDefinition(DeleteRecords)
        {
            Title = "Delete records",
            Description = "Permanently delete the records matching a filter.",
            Icon = "Delete",
            Risk = BitAiToolRisk.Destructive,
            ParametersSchema = E2EAiScriptRun.Json("""{"type":"object","properties":{"filter":{"type":"string"}},"required":["filter"]}"""),
        }, (invocation, _) =>
        {
            var filter = Text(invocation.Arguments, "filter") ?? string.Empty;
            var result = Object(w =>
            {
                w.WriteString("filter", filter);
                w.WriteNumber("deleted", 3);
            });

            return ValueTask.FromResult(BitAiToolOutcome.Success(result, $"Deleted 3 records matching \"{filter}\"."));
        }),

        BitAiTool.Create(new BitAiToolDefinition(SearchDocs)
        {
            Title = "Search docs",
            Description = "Search the documentation and return the matching passages.",
            Icon = "Search",
            ParametersSchema = E2EAiScriptRun.Json("""{"type":"object","properties":{"query":{"type":"string"}},"required":["query"]}"""),
        }, (invocation, _) =>
        {
            invocation.Progress.Report(new BitAiToolProgress(0.5, "Ranking results"));

            var query = Text(invocation.Arguments, "query") ?? string.Empty;
            var result = Object(w =>
            {
                w.WriteString("query", query);
                w.WriteStartArray("hits");
                w.WriteStartObject(); w.WriteString("title", "Render modes"); w.WriteNumber("score", 0.94); w.WriteEndObject();
                w.WriteStartObject(); w.WriteString("title", "Circuit lifetime"); w.WriteNumber("score", 0.81); w.WriteEndObject();
                w.WriteEndArray();
            });

            var source = new BitAiCitationSource("docs-render-modes", "Render modes")
            {
                Kind = BitAiSourceKind.Document,
                SiteName = "Documentation",
                Url = "https://bitplatform.dev/components",
            };

            return ValueTask.FromResult(BitAiToolOutcome.Success(result, $"2 passages matched \"{query}\".") with
            {
                Citations = [new BitAiCitation("docs-cit-1", source) { Quote = "Every component runs on the server or in the browser." }],
            });
        }),

        BitAiTool.Create(new BitAiToolDefinition(FlakyService)
        {
            Title = "Flaky service",
            Description = "A service that is always down.",
            ParametersSchema = E2EAiScriptRun.Json("""{"type":"object","properties":{}}"""),
        }, (_, _) => ValueTask.FromResult(BitAiToolOutcome.Failure(new BitAiError("The service is unavailable.")
        {
            Code = "unavailable",
            Kind = BitAiErrorKind.Tool,
            IsRetryable = true,
        }))),
    ];

    private static string? Text(JsonElement arguments, string name) =>
        arguments.ValueKind is JsonValueKind.Object && arguments.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.String
            ? value.GetString()
            : null;

    // A JSON object written member by member, so a string from the arguments is escaped as JSON escapes it.
    private static JsonElement Object(Action<Utf8JsonWriter> members)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            members(writer);
            writer.WriteEndObject();
        }

        return E2EAiScriptRun.Json(Encoding.UTF8.GetString(stream.ToArray()));
    }
}

/// <summary>The models the host's model catalog offers (a BitAiModelSelector with no Models of its own lists these).</summary>
public static class E2EAiModels
{
    /// <summary>The model selected by default.</summary>
    public const string DefaultModelId = "e2e-large";

    /// <summary>The providers.</summary>
    public static IReadOnlyList<BitAiProvider> Providers { get; } =
    [
        new BitAiProvider("e2e", "E2E Provider"),
        new BitAiProvider("local", "Local Runtime"),
    ];

    /// <summary>Three models: a large one, a fast one without reasoning, and a local one that only streams.</summary>
    public static IReadOnlyList<BitAiModel> Models { get; } =
    [
        new BitAiModel(DefaultModelId, "E2E Large")
        {
            ProviderId = "e2e",
            Family = "E2E Provider",
            Description = "The general model.",
            Capabilities = BitAiModelCapabilities.Streaming | BitAiModelCapabilities.Tools | BitAiModelCapabilities.Vision
                | BitAiModelCapabilities.Reasoning | BitAiModelCapabilities.StructuredOutput | BitAiModelCapabilities.Citations,
            ContextWindow = 200_000,
            MaxOutputTokens = 16_000,
            InputPricePerMillion = 3m,
            OutputPricePerMillion = 15m,
        },
        new BitAiModel("e2e-fast", "E2E Fast")
        {
            ProviderId = "e2e",
            Family = "E2E Provider",
            Description = "Cheaper and quicker.",
            Capabilities = BitAiModelCapabilities.Streaming | BitAiModelCapabilities.Tools,
            ContextWindow = 128_000,
            InputPricePerMillion = 0.5m,
            OutputPricePerMillion = 1.5m,
        },
        new BitAiModel("e2e-local", "E2E Local")
        {
            ProviderId = "local",
            Family = "Local Runtime",
            Capabilities = BitAiModelCapabilities.Streaming,
            ContextWindow = 32_000,
        },
    ];
}
