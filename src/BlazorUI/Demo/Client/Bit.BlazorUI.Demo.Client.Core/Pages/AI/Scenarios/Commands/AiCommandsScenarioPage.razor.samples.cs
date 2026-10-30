namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.Commands;

public partial class AiCommandsScenarioPage
{
    private readonly string example1RazorCode = """
@implements IAsyncDisposable

<div style="height: 560px">
    <BitAiChat Session="session"
               Commands="commands"
               Mentions="mentions"
               Placeholder="Type / for commands, @@ to reference something"
               EmptyTitle="Try a slash or an at sign"
               EmptyBody="Start a message with / to pick a command, or type @@ anywhere to reference a file or a person."
               Suggestions="suggestions" />
</div>
""";

    private readonly string example1CsharpCode = """
private readonly BitAiChatSession session = new(AiDemoInteractiveTransports.Commands());
private IReadOnlyList<BitAiSlashCommand> commands = [];

// The app's own list. The composer inserts the label; the app resolves the id.
private readonly IReadOnlyList<BitAiMentionItem> mentions =
[
    new("file-roadmap", "roadmap.md", "Product roadmap, updated Monday", BitIconName.Page),
    new("file-pricing", "pricing.xlsx", "Pricing model, three tiers", BitIconName.Page),
    new("user-priya", "priya", "Priya Raman, product lead", BitIconName.Contact),
    new("user-tomas", "tomas", "Tomás Silva, engineering", BitIconName.Contact),
    new("source-analytics", "analytics", "Usage analytics, last 30 days", BitIconName.Database),
];

private readonly string[] suggestions = ["Compare @roadmap.md with @pricing.xlsx", "Ask @priya about the launch date"];

protected override void OnInitialized() => commands =
[
    // A Template inserts text into the composer...
    new("summarize", "Summarize the conversation so far", BitIconName.BulletedList)
    {
        Template = "Summarize the conversation so far in three bullet points.",
    },
    new("explain", "Explain something for a newcomer", BitIconName.Lightbulb)
    {
        Template = "Explain this as if I were new to the codebase: ",
    },

    // ...a Handler runs app code, and never reaches the model.
    new("regenerate", "Write the last answer again", BitIconName.Refresh)
    {
        Handler = async () => await session.RegenerateAsync(),
    },
    new("clear", "Start over with an empty conversation", BitIconName.Delete)
    {
        Handler = () =>
        {
            session.Stop();
            session.Conversation.Clear();
            return Task.CompletedTask;
        },
    },
];

public ValueTask DisposeAsync() => session.DisposeAsync();
""";

    private const string example1TransportCode = """"
using System.Text.RegularExpressions;

// AiScriptedTransport and AiScript are the demo's helpers - see the Basic chat scenario.
public static partial class AiDemoInteractiveTransports
{
    // A mention arrives as plain text, @roadmap.md: resolving it is the application's job.
    public static IBitAiTransport Commands() => new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
    {
        await yielder(new BitAiMessageStartedEvent());

        var text = AiScript.LastUserText(request);
        var mentions = MentionRegex().Matches(text)
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var fromYou = request.Messages.Count(m => m.Role.IsUser);
        var fromMe = request.Messages.Count(m => m.Role.IsAssistant);

        // What the /summarize template writes.
        var answer = text.StartsWith("Summarize", StringComparison.OrdinalIgnoreCase)
            ? $"""
              So far:

              - {fromYou} {(fromYou == 1 ? "message" : "messages")} from you, {fromMe} from me.
              - Nothing has been decided yet.
              - The next step is yours.
              """
            : mentions.Length > 0
                ? "You referenced " + string.Join(", ", mentions.Select(m => $"**{m}**")) +
                  ". A real application resolves each one by identifier and puts its content into the prompt; " +
                  "the model never has to guess which file or person you meant."
                : "Type `/` for commands or `@` to reference a file, a person, or a data source.";

        await foreach (var evt in AiScript.Write(answer, 4, 12, token)) await yielder(evt);
        await yielder(AiScript.Usage(request, answer));
        await yielder(new BitAiMessageCompletedEvent());
    }));

    [GeneratedRegex(@"(?<=^|\s)@([\w.\-]+)")]
    private static partial Regex MentionRegex();
}
"""";

    private readonly DemoCodeFile[] example1CodeFiles =
    [
        new("AiDemoInteractiveTransports.cs", example1TransportCode),
    ];
}
