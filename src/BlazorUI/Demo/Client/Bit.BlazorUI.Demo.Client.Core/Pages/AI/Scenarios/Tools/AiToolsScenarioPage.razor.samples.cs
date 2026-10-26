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
public static IBitAiTransport CallsTool(string toolName, string arguments, string answer, string? declinedAnswer = null) =>
    new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
    {
        // The outcome of the tool this turn ran, if it ran one.
        var outcome = AiScript.ToolOutcomeThisTurn(request);

        await yielder(new BitAiMessageStartedEvent());

        // First round trip: ask for the tool, streaming its arguments, and stop.
        if (outcome is null)
        {
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
        await yielder(new BitAiMessageCompletedEvent());
    }));
""";

    private readonly DemoCodeFile[] example1CodeFiles =
    [
        new("Program.cs", example1ProgramCode),
        new("AiDemoJsonContext.cs", example1JsonContextCode),
        new("AiDemoToolTransports.cs", example1TransportCode),
    ];
}
