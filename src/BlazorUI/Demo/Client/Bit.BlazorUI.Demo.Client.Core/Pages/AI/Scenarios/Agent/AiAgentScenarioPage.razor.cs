using Bit.BlazorUI.Demo.Client.Core.Services.AI;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.Agent;

public partial class AiAgentScenarioPage
{
    private readonly BitAiChatSession _session = new(AiDemoAgentTransports.Research());

    private readonly string[] _suggestions = ["Do the sources agree on the mechanism?"];

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        await _session.DisposeAsync();

        await base.DisposeAsync(disposing);
    }
}
