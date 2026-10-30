using Bit.BlazorUI.Demo.Client.Core.Services.AI;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.Commands;

public partial class AiCommandsScenarioPage
{
    private readonly BitAiChatSession _session = new(AiDemoInteractiveTransports.Commands());
    private IReadOnlyList<BitAiSlashCommand> _commands = [];

    private readonly IReadOnlyList<BitAiMentionItem> _mentions =
    [
        new("file-roadmap", "roadmap.md", "Product roadmap, updated Monday", BitIconName.Page),
        new("file-pricing", "pricing.xlsx", "Pricing model, three tiers", BitIconName.Page),
        new("user-priya", "priya", "Priya Raman, product lead", BitIconName.Contact),
        new("user-tomas", "tomas", "Tomás Silva, engineering", BitIconName.Contact),
        new("source-analytics", "analytics", "Usage analytics, last 30 days", BitIconName.Database),
    ];

    private readonly string[] _suggestions = ["Compare @roadmap.md with @pricing.xlsx", "Ask @priya about the launch date"];

    protected override Task OnInitAsync()
    {
        _commands =
        [
            new("summarize", "Summarize the conversation so far", BitIconName.BulletedList)
            {
                Template = "Summarize the conversation so far in three bullet points.",
            },
            new("explain", "Explain something for a newcomer", BitIconName.Lightbulb)
            {
                Template = "Explain this as if I were new to the codebase: ",
            },
            new("regenerate", "Write the last answer again", BitIconName.Refresh)
            {
                Handler = async () => await _session.RegenerateAsync(),
            },
            new("clear", "Start over with an empty conversation", BitIconName.Delete)
            {
                Handler = () =>
                {
                    _session.Stop();
                    _session.Conversation.Clear();
                    return Task.CompletedTask;
                },
            },
        ];

        return base.OnInitAsync();
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        await _session.DisposeAsync();

        await base.DisposeAsync(disposing);
    }
}
