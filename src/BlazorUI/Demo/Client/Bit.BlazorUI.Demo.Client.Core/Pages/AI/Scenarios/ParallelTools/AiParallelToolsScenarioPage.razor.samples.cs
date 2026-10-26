namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.ParallelTools;

public partial class AiParallelToolsScenarioPage
{
    private readonly string example1RazorCode = """
@implements IAsyncDisposable

<div style="height: 560px">
    <BitAiChat Session="session"
               EmptyTitle="Compare two cities"
               Suggestions="suggestions" />
</div>
""";

    private readonly string example1CsharpCode = """
private BitAiChatSession? session;

private readonly string[] suggestions = ["Compare the weather in Oslo and Lisbon"];

[Inject] private IBitAiToolRegistry Tools { get; set; } = default!;
[Inject] private BitAiChatSessionFactory SessionFactory { get; set; } = default!;

// A transport of its own, with the registered tools and the session options configured in Program.cs
// (SessionFactory.Defaults), which is where the limit of six rounds comes from.
protected override void OnInitialized() =>
    session = new BitAiChatSession(AiDemoToolTransports.CallsTwoTools(), options: SessionFactory.Defaults, tools: Tools);

public ValueTask DisposeAsync() => session?.DisposeAsync() ?? ValueTask.CompletedTask;
""";

    private const string example1TransportCode = """"
// AiScriptedTransport and AiScript are the demo's helpers - see the Basic chat scenario.
public static IBitAiTransport CallsTwoTools() => new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
{
    await yielder(new BitAiMessageStartedEvent());

    if (AiScript.HasToolResultThisTurn(request) is false)
    {
        // Two calls in one round, each with its arguments complete from the start.
        await yielder(new BitAiToolCallStartedEvent(BitAiId.NewToolCallId(), "get_weather")
        {
            Arguments = AiScript.Json("""{"city":"Oslo"}"""),
        });
        await yielder(new BitAiToolCallStartedEvent(BitAiId.NewToolCallId(), "get_weather")
        {
            Arguments = AiScript.Json("""{"city":"Lisbon"}"""),
        });

        await yielder(new BitAiMessageCompletedEvent(BitAiFinishReason.ToolCalls));
        return;
    }

    const string answer = "Oslo is cold and grey; Lisbon is mild and clear.";
    await foreach (var evt in AiScript.Write(answer, 4, 12, token)) await yielder(evt);
    await yielder(new BitAiMessageCompletedEvent());
}));
"""";

    private const string example1ProgramCode = """
builder.Services.AddBitBlazorUIAIServices(ai =>
{
    ai.UseTransport(AiDemoTransports.Echo());

    // The tool both calls ask for (its types are on the Tool calling scenario).
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

    // How many rounds of tools one turn may take before the session stops asking.
    ai.ConfigureSessions(options => options with { MaxToolRoundtrips = 6 });
});

static WeatherResult Weather(string city) => city.ToLowerInvariant() switch
{
    "oslo" => new WeatherResult("Oslo", -3, "Snow", 81),
    "lisbon" => new WeatherResult("Lisbon", 17, "Clear", 54),
    "auckland" => new WeatherResult("Auckland", 22, "Showers", 72),
    _ => new WeatherResult(city, 14, "Cloudy", 66),
};
""";

    private readonly DemoCodeFile[] example1CodeFiles =
    [
        new("Program.cs", example1ProgramCode),
        new("AiDemoToolTransports.cs", example1TransportCode),
    ];
}
