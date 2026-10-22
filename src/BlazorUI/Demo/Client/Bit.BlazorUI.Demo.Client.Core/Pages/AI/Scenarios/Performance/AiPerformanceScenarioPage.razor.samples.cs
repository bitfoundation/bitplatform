namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.Performance;

public partial class AiPerformanceScenarioPage
{
    private readonly string example1RazorCode = """
@implements IAsyncDisposable

<div style="height: 560px">
    <BitAiChat Session="session" UiOptions="options" />
</div>
""";

    private readonly string example1CsharpCode = """
private readonly BitAiUiOptions options = BitAiUiOptions.Default with
{
    ShowTimestamps = true,
    VirtualizeThreshold = 60,
};

private BitAiChatSession? session;

[Inject] private BitAiChatSessionFactory SessionFactory { get; set; } = default!;

// A session over a conversation that already has five hundred messages.
protected override void OnInitialized() => session = SessionFactory.Create(AiDemoConversations.Long(500));

public ValueTask DisposeAsync() => session?.DisposeAsync() ?? ValueTask.CompletedTask;
""";

    private const string example1ConversationCode = """"
public static BitAiConversation Long(int messageCount)
{
    var conversation = new BitAiConversation();
    conversation.SetTitle($"{messageCount} message conversation");

    for (var i = 0; i < messageCount; i += 2)
    {
        conversation.AddMessage(BitAiRole.User, $"Question {i / 2 + 1}: what happens at scale here?");

        var assistant = new BitAiMessage(BitAiRole.Assistant);

        // Prose, code, a tool call with its result, and a table with a citation, in turn.
        switch ((i / 2) % 4)
        {
            case 0: assistant.AddPart(new BitAiTextPart("The cost is dominated by rendering, not by parsing.")); break;
            case 1: assistant.AddPart(new BitAiMarkdownPart("```csharp\nscheduler.Request();\n```")); break;
            case 2:
                var call = assistant.AddPart(new BitAiToolCallPart(BitAiId.NewToolCallId(), "search_docs"));
                call.CompleteArguments(AiScript.Json($$"""{"query":"scale {{i}}"}"""));
                call.SetResult(BitAiToolOutcome.Success(AiDemoCatalog.SearchHits($"scale {i}"), AiDemoJsonContext.Default.SearchResult));
                break;
            default:
                assistant.AddPart(new BitAiMarkdownPart("| Messages | Rendered |\n| --- | ---: |\n| 500 | visible only |\n"));
                assistant.AddCitation(new BitAiCitation(BitAiId.New("cit"), source));
                break;
        }

        conversation.AddMessage(assistant);
    }

    return conversation;
}
"""";

    private readonly DemoCodeFile[] example1CodeFiles =
    [
        new("AiDemoConversations.cs", example1ConversationCode),
    ];
}
