using Bit.BlazorUI.Demo.Client.Core.Services.AI;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.Reasoning;

public partial class AiReasoningScenarioPage
{
    private readonly BitAiChatSession _session = new(AiDemoTransports.Reasoning());

    private readonly string[] _suggestions = ["What does a round trip actually cost?"];

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        await _session.DisposeAsync();

        await base.DisposeAsync(disposing);
    }
}
