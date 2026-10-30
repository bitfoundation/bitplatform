namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.Approval;

public partial class AiApprovalScenarioPage
{
    private readonly string example1RazorCode = """
@implements IAsyncDisposable

<div style="height: 560px">
    <BitAiChat Session="session"
               EmptyTitle="Ask for something destructive"
               EmptyBody="delete_records is declared destructive, so the session will not run it unattended."
               Suggestions="suggestions" />
</div>
""";

    private readonly string example1CsharpCode = """"
private BitAiChatSession? session;

private readonly string[] suggestions = ["Delete the archived records"];

[Inject] private IBitAiToolRegistry Tools { get; set; } = default!;
[Inject] private IBitAiToolApprovalPolicy Policy { get; set; } = default!;

protected override void OnInitialized() => session = new BitAiChatSession(
    AiDemoToolTransports.CallsTool(
        "delete_records",
        """{"filter":"status = 'archived' AND updated < '2024-01-01'"}""",
        "Deleted 128 archived records. Nothing else was touched.",
        // What the model says when the user declines: the call comes back to it as a failure.
        declinedAnswer: "Understood - I did not delete anything; the archived records are still there."),
    tools: Tools,
    approvalPolicy: Policy);

public ValueTask DisposeAsync() => session?.DisposeAsync() ?? ValueTask.CompletedTask;
"""";

    private const string example1ProgramCode = """
builder.Services.AddBitBlazorUIAIServices(ai =>
{
    ai.UseTransport(AiDemoTransports.Echo());

    // Destructive: the default approval policy stops every call of it for a human decision.
    ai.AddTool(BitAiTool.Create(
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
        }));
});
""";

    private const string example1TransportCode = """
// AiScriptedTransport and AiScript are the demo's helpers - see the Basic chat scenario.
public static IBitAiTransport CallsTool(string toolName, string arguments, string answer, string? declinedAnswer = null) =>
    new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
    {
        // The outcome of the tool this turn ran, if it ran one.
        var outcome = AiScript.ToolOutcomeThisTurn(request);

        await yielder(new BitAiMessageStartedEvent());

        // First round trip: ask for the tool and stop. The session decides whether it needs approval.
        if (outcome is null)
        {
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

        // Second round trip: a declined call comes back as a failure, and the answer says nothing was done.
        var reply = outcome.IsSuccess ? answer : declinedAnswer ?? $"I did not run {toolName}, so nothing was changed.";

        await foreach (var evt in AiScript.Write(reply, 4, 12, token)) await yielder(evt);
        await yielder(AiScript.Usage(request, reply));
        await yielder(new BitAiMessageCompletedEvent());
    }));
""";

    private const string example1JsonContextCode = """
public sealed record DeleteArgs(string Filter);

public sealed record DeleteResult(string Filter, int Deleted);

// The tool's arguments and result are read and written through generated metadata, so the tool is safe to trim.
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(DeleteArgs))]
[JsonSerializable(typeof(DeleteResult))]
public partial class AiDemoJsonContext : JsonSerializerContext
{
}
""";

    private readonly DemoCodeFile[] example1CodeFiles =
    [
        new("Program.cs", example1ProgramCode),
        new("AiDemoJsonContext.cs", example1JsonContextCode),
        new("AiDemoToolTransports.cs", example1TransportCode),
    ];

    private readonly string example2RazorCode = """
@implements IAsyncDisposable

<div style="height: 560px">
    <BitAiChat Session="session"
               EmptyTitle="Ask about the weather"
               EmptyBody="The transport asks for consent before get_weather runs, whatever the app's policy says."
               Suggestions="suggestions" />
</div>
""";

    private readonly string example2CsharpCode = """"
private BitAiChatSession? session;

private readonly string[] suggestions = ["What is the weather in Lisbon?"];

[Inject] private IBitAiToolRegistry Tools { get; set; } = default!;
[Inject] private IBitAiToolApprovalPolicy Policy { get; set; } = default!;

protected override void OnInitialized() => session = new BitAiChatSession(
    AiDemoToolTransports.CallsToolAskingApproval(
        "get_weather",
        """{"city":"Lisbon"}""",
        // Shown as the question of the prompt.
        reason: "Looking up the weather tells the weather service which city you asked about. Go ahead?",
        answer: "It is 17 degrees and clear in Lisbon, with 54% humidity.",
        declinedAnswer: "Understood - I did not look it up, so the weather service was told nothing."),
    tools: Tools,
    approvalPolicy: Policy);

public ValueTask DisposeAsync() => session?.DisposeAsync() ?? ValueTask.CompletedTask;
"""";

    private const string example2TransportCode = """
// AiScriptedTransport and AiScript are the demo's helpers - see the Basic chat scenario.
public static IBitAiTransport CallsToolAskingApproval(string toolName, string arguments, string reason, string answer, string declinedAnswer) =>
    new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
    {
        var outcome = AiScript.ToolOutcomeThisTurn(request);

        await yielder(new BitAiMessageStartedEvent());

        if (outcome is null)
        {
            await AiScript.Pause(400, token);

            var callId = BitAiId.NewToolCallId();
            await yielder(new BitAiToolCallStartedEvent(callId, toolName) { Arguments = AiScript.Json(arguments) });

            // The provider's own policy wants consent: the call waits for the user, whatever the app's policy says.
            await yielder(new BitAiToolApprovalRequestedEvent(callId) { Reason = reason });

            await yielder(new BitAiMessageCompletedEvent(BitAiFinishReason.ToolCalls));
            return;
        }

        var reply = outcome.IsSuccess ? answer : declinedAnswer;

        await foreach (var evt in AiScript.Write(reply, 4, 12, token)) await yielder(evt);
        await yielder(AiScript.Usage(request, reply));
        await yielder(new BitAiMessageCompletedEvent());
    }));
""";

    private const string example2ProgramCode = """
builder.Services.AddBitBlazorUIAIServices(ai =>
{
    ai.UseTransport(AiDemoTransports.Echo());

    // Read-only: the default approval policy runs it unattended. The transport's request is what stops it here.
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

    private const string example2JsonContextCode = """
public sealed record WeatherArgs(
    [property: Description("The city to look up, for example Oslo.")] string City,
    [property: Description("How many days ahead to forecast.")] int Days = 1);

public sealed record WeatherResult(string City, int Degrees, string Condition, int Humidity);

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(WeatherArgs))]
[JsonSerializable(typeof(WeatherResult))]
public partial class AiDemoJsonContext : JsonSerializerContext
{
}
""";

    private readonly DemoCodeFile[] example2CodeFiles =
    [
        new("Program.cs", example2ProgramCode),
        new("AiDemoJsonContext.cs", example2JsonContextCode),
        new("AiDemoToolTransports.cs", example2TransportCode),
    ];
}
