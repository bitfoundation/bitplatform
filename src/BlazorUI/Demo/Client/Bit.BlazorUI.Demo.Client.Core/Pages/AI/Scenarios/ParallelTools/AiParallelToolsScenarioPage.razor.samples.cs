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

protected override void OnInitialized() =>
    session = new BitAiChatSession(AiDemoToolTransports.CallsTwoTools(), tools: Tools);

public ValueTask DisposeAsync() => session?.DisposeAsync() ?? ValueTask.CompletedTask;
""";

    private const string example1TransportCode = """"
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
    ai.AddTool(weatherTool);

    // How many rounds of tools one turn may take before the session stops asking.
    ai.ConfigureSessions(options => options with { MaxToolRoundtrips = 6 });
});
""";

    private readonly DemoCodeFile[] example1CodeFiles =
    [
        new("Program.cs", example1ProgramCode),
        new("AiDemoToolTransports.cs", example1TransportCode),
    ];
}
