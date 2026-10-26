namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.Tools;

public partial class AiToolsScenarioPage
{
    private readonly string example1RazorCode = """
@implements IAsyncDisposable

<div style="height: 560px">
    <BitAiChat Session="session"
               EmptyTitle="Ask about the weather"
               EmptyBody="The model will call get_weather, wait for the result, and answer from it."
               Suggestions="suggestions" />
</div>
""";

    private readonly string example1CsharpCode = """"
private BitAiChatSession? session;

private readonly string[] suggestions = ["What is the weather in Oslo?"];

// The tools registered with AddBitBlazorUIAIServices; the session runs the ones the model asks for.
[Inject] private IBitAiToolRegistry Tools { get; set; } = default!;

protected override void OnInitialized() => session = new BitAiChatSession(
    AiDemoToolTransports.CallsTool(
        "get_weather",
        """{"city":"Oslo","days":1}""",
        "It is -3 degrees in Oslo with snow and 81% humidity. Pack for it."),
    tools: Tools);

public ValueTask DisposeAsync() => session?.DisposeAsync() ?? ValueTask.CompletedTask;
"""";

    private const string example1ProgramCode = """
builder.Services.AddBitBlazorUIAIServices(ai =>
{
    ai.UseTransport(AiDemoTransports.Echo());

    // The JsonTypeInfo overload: the schema and the arguments are read without reflection, so it is safe to trim.
    ai.AddTool(BitAiTool.Create(
        "get_weather",
        "Look up the current weather for a city.",
        AiDemoJsonContext.Default.WeatherArgs,
        AiDemoJsonContext.Default.WeatherResult,
        async (args, token) =>
        {
            await Task.Delay(700, token);
            return Weather(args.City);
        },
        definition => definition with { Title = "Weather", Icon = BitIconName.Globe, Risk = BitAiToolRisk.ReadOnly }));
});

static WeatherResult Weather(string city) => city.ToLowerInvariant() switch
{
    "oslo" => new WeatherResult("Oslo", -3, "Snow", 81),
    "lisbon" => new WeatherResult("Lisbon", 17, "Clear", 54),
    "auckland" => new WeatherResult("Auckland", 22, "Showers", 72),
    _ => new WeatherResult(city, 14, "Cloudy", 66),
};
""";

    private const string example1JsonContextCode = """
public sealed record WeatherArgs(
    [property: Description("The city to look up, for example Oslo.")] string City,
    [property: Description("How many days ahead to forecast.")] int Days = 1);

public sealed record WeatherResult(string City, int Degrees, string Condition, int Humidity);

// The web defaults - camelCase, read case-insensitively - are what a model is told and what it sends back.
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(WeatherArgs))]
[JsonSerializable(typeof(WeatherResult))]
public partial class AiDemoJsonContext : JsonSerializerContext
{
}
""";

    private const string example1TransportCode = """
// AiScriptedTransport and AiScript are the demo's helpers - see the Basic chat scenario.
public static IBitAiTransport CallsTool(string toolName, string arguments, string answer, string? declinedAnswer = null) =>
    new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
    {
        // The outcome of the tool this turn ran, if it ran one.
        var outcome = AiScript.ToolOutcomeThisTurn(request);

        await yielder(new BitAiMessageStartedEvent());

        // First round trip: ask for the tool, streaming its arguments, and stop.
        if (outcome is null)
        {
            // Shown until the call appears.
            await yielder(new BitAiStatusEvent("Deciding what to look up"));
            await AiScript.Pause(500, token);

            var callId = BitAiId.NewToolCallId();
            await yielder(new BitAiToolCallStartedEvent(callId, toolName));

            foreach (var slice in AiScript.Slice(arguments, 6))
            {
                await AiScript.Pause(40, token);
                await yielder(new BitAiToolCallDeltaEvent(callId, slice));
            }

            await yielder(new BitAiToolCallCompletedEvent(callId));
            await yielder(new BitAiMessageCompletedEvent(BitAiFinishReason.ToolCalls));
            return;
        }

        // Second round trip: the session ran the tool - or the user declined it, which comes back as a failure - and its
        // result is on the request.
        var reply = outcome.IsSuccess ? answer : declinedAnswer ?? $"I did not run {toolName}, so nothing was changed.";

        await foreach (var evt in AiScript.Write(reply, 4, 12, token)) await yielder(evt);
        await yielder(AiScript.Usage(request, reply));
        await yielder(new BitAiMessageCompletedEvent());
    }));
""";

    private const string example1RequestOptionsCode = """"
// The session's request options go with every request: here the model must call get_weather.
var session = new BitAiChatSession(transport, options: new BitAiChatSessionOptions
{
    RequestOptions = new BitAiRequestOptions { ToolChoice = BitAiToolChoice.Specific("get_weather") },
}, tools: registry);

// The tools themselves can change at run time - a capability only some users get.
registry.Register(reportTool);
registry.Unregister("delete_records");

// In the transport: what the model may call, and how, mapped onto the provider's API.
IReadOnlyList<BitAiToolDefinition> tools = request.Tools;            // Name, Description, ParametersSchema
var choice = request.Options?.ToolChoice ?? BitAiToolChoice.Auto;    // Mode: auto, none, required or specific (ToolName)

// A tool the provider runs itself is reported rather than run: the session never runs a call that has a result.
var callId = BitAiId.NewToolCallId();
yield return new BitAiToolCallStartedEvent(callId, "web_search") { Arguments = AiScript.Json("""{"query":"render modes"}""") };
yield return new BitAiToolProgressEvent(callId, new BitAiToolProgress(0.5, "Reading 11 results"));
yield return new BitAiToolResultEvent(callId, BitAiToolOutcome.FromText("11 results, 3 of them primary studies."));
"""";

    private const string example1ToolClassCode = """
using Microsoft.Extensions.DependencyInjection;

// The same tool as a class of its own, for one that needs services: registered with ai.AddTool<WeatherTool>().
// One instance serves every conversation, so it keeps no state and resolves what it needs per call.
public sealed class WeatherTool : IBitAiTool
{
    public BitAiToolDefinition Definition { get; } = new("get_weather")
    {
        Title = "Weather",
        Description = "Look up the current weather for a city.",
        ParametersSchema = BitAiTool.CreateSchema(AiDemoJsonContext.Default.WeatherArgs),
        Icon = BitIconName.Globe,
        Risk = BitAiToolRisk.ReadOnly,
    };

    public async ValueTask<BitAiToolOutcome> InvokeAsync(BitAiToolInvocation invocation, CancellationToken cancellationToken = default)
    {
        // The model's arguments, read through the generated metadata.
        if (invocation.GetArguments(AiDemoJsonContext.Default.WeatherArgs) is not { } args)
        {
            return BitAiToolOutcome.Failure("No city was given.");
        }

        // No fraction: the card shows a bar that sweeps, with the message.
        invocation.Progress.Report(new BitAiToolProgress(null, $"Asking the weather service about {args.City}"));

        // A scoped service of the app (IWeatherService is the app's own), resolved for this call.
        var weather = invocation.Services!.GetRequiredService<IWeatherService>();
        var result = await weather.GetAsync(args.City, cancellationToken);

        return BitAiToolOutcome.Success(result, AiDemoJsonContext.Default.WeatherResult);
    }
}
""";

    private readonly DemoCodeFile[] example1CodeFiles =
    [
        new("Program.cs", example1ProgramCode),
        new("AiDemoJsonContext.cs", example1JsonContextCode),
        new("AiDemoToolTransports.cs", example1TransportCode),
        new("WeatherTool.cs", example1ToolClassCode),
        new("Request options", example1RequestOptionsCode, "csharp"),
    ];
}
