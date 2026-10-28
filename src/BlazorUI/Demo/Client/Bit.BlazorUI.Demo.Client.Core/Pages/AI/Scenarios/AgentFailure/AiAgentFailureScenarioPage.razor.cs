using Bit.BlazorUI.Demo.Client.Core.Services.AI;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.AgentFailure;

public partial class AiAgentFailureScenarioPage
{
    private readonly BitAiChatSession _session = new(AiDemoAgentTransports.Fails());

    private readonly string[] _suggestions = ["Deploy the current branch"];

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        await _session.DisposeAsync();

        await base.DisposeAsync(disposing);
    }
}
